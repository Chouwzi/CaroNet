using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Server.Host.GameRooms;
using CaroNet.Server.Host.Networking;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol;
using CaroNet.Shared.Protocol.Payloads;
using CaroNet.Storage.Matches;

namespace CaroNet.Server.Host.Services;

// Xử lý message từ client: Hello, CreateRoom, JoinRoom, MakeMove.
public sealed class GameMessageDispatcher : IMessageDispatcher
{
    private readonly RoomManager _roomManager;
    private readonly ClientSessionRegistry _registry;
    private readonly IMatchHistoryStore? _matchHistoryStore;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> _playerNames = new();

    public GameMessageDispatcher(
        RoomManager roomManager,
        ClientSessionRegistry registry,
        IMatchHistoryStore? matchHistoryStore = null)
    {
        _roomManager = roomManager;
        _registry = registry;
        _matchHistoryStore = matchHistoryStore;
    }

    public async Task DispatchAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"[DISPATCH] Client={session.Id} Type={message.Type}");

        switch (message.Type)
        {
            case MessageType.Hello:
                await HandleHelloAsync(session, message, cancellationToken);
                break;

            case MessageType.CreateRoom:
                await HandleCreateRoomAsync(session, message, cancellationToken);
                break;

            case MessageType.JoinRoom:
                await HandleJoinRoomAsync(session, message, cancellationToken);
                break;

            case MessageType.MakeMove:
                await HandleMakeMoveAsync(session, message, cancellationToken);
                break;
            case MessageType.Rematch:
                await HandleRematchAsync(session, message, cancellationToken);
                break;

            default:
                Console.WriteLine(
                    $"[DISPATCH] Unhandled message type: {message.Type}");
                break;
        }
    }

    // Xử lý khi client ngắt kết nối.
    public async Task HandleDisconnectAsync(Guid sessionId)
    {
        _playerNames.TryRemove(sessionId, out _);

        GameRoom? room = _roomManager.HandleDisconnect(sessionId);
        if (room is null) return;
        room.StopRematchTimeout();
        // Báo đối thủ biết
        foreach (var player in room.GetPlayers())
        {
            try
            {
                await player.SendAsync(new MessageEnvelope
                {
                    Type = MessageType.Error,
                    Payload = JsonSerializer.SerializeToElement(new
                    {
                        message = "Đối thủ đã ngắt kết nối."
                    })
                }, CancellationToken.None);
            }
            catch
            {

            }
        }
    }

    private async Task HandleHelloAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        string playerName = "Player";

        if (message.Payload.HasValue)
        {
            try
            {
                var hello = message.Payload.Value.Deserialize<HelloPayload>();
                if (!string.IsNullOrWhiteSpace(hello?.PlayerName))
                    playerName = hello.PlayerName.Trim();
            }
            catch { /* use default name */ }
        }

        // Giới hạn tên tránh spam
        if (playerName.Length > 30)
            playerName = playerName[..30];

        _playerNames[session.Id] = playerName;

        await session.SendAsync(new MessageEnvelope
        {
            Type = MessageType.HelloAccepted,
            PlayerId = session.Id.ToString(),
            Payload = JsonSerializer.SerializeToElement(new
            {
                playerName,
                playerId = session.Id.ToString()
            })
        }, cancellationToken);
    }

    private async Task HandleCreateRoomAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        string playerName = GetPlayerName(session.Id);

        GameRoom? room = _roomManager.CreateRoom(session, playerName);

        if (room is null)
        {
            await SendErrorAsync(session, "Không thể tạo phòng. Server đã đầy.", cancellationToken);
            return;
        }

        await session.SendAsync(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            RoomId = room.RoomId,
            PlayerId = session.Id.ToString(),
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = room.RoomId,
                symbol = "X",
                playerName
            })
        }, cancellationToken);
    }

    private async Task HandleJoinRoomAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        string? roomId = message.RoomId;

        if (string.IsNullOrWhiteSpace(roomId) && message.Payload.HasValue)
        {
            try
            {
                var joinPayload = message.Payload.Value.Deserialize<JoinRoomPayload>();
                roomId = joinPayload?.RoomId;
            }
            catch { /* invalid payload */ }
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            await SendErrorAsync(session, "Mã phòng không hợp lệ.", cancellationToken);
            return;
        }

        string playerName = GetPlayerName(session.Id);

        var (room, symbol) = _roomManager.JoinRoom(session, roomId, playerName);

        if (room is null || symbol is null)
        {
            await SendErrorAsync(session, "Phòng không tồn tại hoặc đã đầy.", cancellationToken);
            return;
        }

        await session.SendAsync(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            RoomId = room.RoomId,
            PlayerId = session.Id.ToString(),
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = room.RoomId,
                symbol = symbol.Value.ToString(),
                playerName
            })
        }, cancellationToken);

        // Đủ 2 người, bắt đầu ván
        if (room.IsFull)
        {
            await BroadcastGameStartedAsync(room, cancellationToken);
        }
    }

    private async Task HandleMakeMoveAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        GameRoom? room = _roomManager.GetRoomBySession(session.Id);
        if (room is null)
        {
            await SendErrorAsync(session, "Bạn chưa vào phòng nào.", cancellationToken);
            return;
        }

        if (!room.IsFull)
        {
            await SendErrorAsync(session, "Chờ đối thủ vào phòng.", cancellationToken);
            return;
        }

        int row = 0, col = 0;

        if (message.Payload.HasValue)
        {
            try
            {
                var movePayload = message.Payload.Value.Deserialize<MakeMovePayload>();
                if (movePayload is not null)
                {
                    row = movePayload.Row;
                    col = movePayload.Column;
                }
            }
            catch
            {
                await SendErrorAsync(session, "Dữ liệu nước đi không hợp lệ.", cancellationToken);
                return;
            }
        }

        MoveResult result = room.TryMakeMove(session.Id, row, col);

        if (!result.IsSuccess)
        {
            string reason = result.Reason switch
            {
                MoveRejectReason.CellOccupied => "Ô này đã được đánh.",
                MoveRejectReason.WrongTurn => "Chưa đến lượt của bạn.",
                MoveRejectReason.OutOfBounds => "Tọa độ ngoài bàn cờ.",
                MoveRejectReason.GameEnded => "Trò chơi đã kết thúc.",
                _ => "Nước đi không hợp lệ."
            };

            await session.SendAsync(new MessageEnvelope
            {
                Type = MessageType.MoveRejected,
                RoomId = room.RoomId,
                Payload = JsonSerializer.SerializeToElement(new { reason })
            }, cancellationToken);

            return;
        }

        await BroadcastGameStateAsync(room, cancellationToken);

        if (result.Status != GameStatus.Playing)
        {
            // TRUYỀN THÊM row VÀ col VÀO ĐỂ KHÔNG BỊ LỖI THUỘC TÍNH
            await BroadcastGameEndedAsync(room, result.Status, row, col, cancellationToken);
            await SaveMatchHistoryAsync(room, result.Status);
        }
    }

    private async Task BroadcastGameStartedAsync(
        GameRoom room, CancellationToken cancellationToken)
    {
        var gameState = BuildGameStatePayload(room);

        foreach (var player in room.GetPlayers())
        {
            try
            {
                var symbol = room.GetPlayerSymbol(player.Id);
                await player.SendAsync(new MessageEnvelope
                {
                    Type = MessageType.GameStarted,
                    RoomId = room.RoomId,
                    PlayerId = player.Id.ToString(),
                    Payload = JsonSerializer.SerializeToElement(new
                    {
                        roomId = room.RoomId,
                        yourSymbol = symbol?.ToString() ?? "",
                        board = gameState.Board,
                        currentTurnPlayerId = gameState.CurrentTurnPlayerId
                    })
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[BROADCAST ERROR] {player.Id}: {ex.Message}");
            }
        }
    }

    private async Task BroadcastGameStateAsync(
        GameRoom room, CancellationToken cancellationToken)
    {
        var payload = BuildGameStatePayload(room);
        var envelope = new MessageEnvelope
        {
            Type = MessageType.GameStateUpdated,
            RoomId = room.RoomId,
            Payload = JsonSerializer.SerializeToElement(payload)
        };

        foreach (var player in room.GetPlayers())
        {
            try
            {
                await player.SendAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[BROADCAST ERROR] {player.Id}: {ex.Message}");
            }
        }
    }

    // THAY ĐỔI: Hàm nhận thêm tham số int lastRow, int lastCol để quét ô thắng trực tiếp
    private async Task BroadcastGameEndedAsync(
         GameRoom room, GameStatus status, int lastRow, int lastCol, CancellationToken cancellationToken)
    {
        string? winnerId = status switch
        {
            GameStatus.XWon => room.PlayerX?.Id.ToString(),
            GameStatus.OWon => room.PlayerO?.Id.ToString(),
            _ => null
        };

        // SỬA TẠI ĐÂY: Tính toán và chuyển đổi danh sách tọa độ sang JSON Payload an toàn
        object? winningCellsPayload = null;

        if (status == GameStatus.XWon || status == GameStatus.OWon)
        {
            // Lấy danh sách (int Row, int Col) từ Engine
            var rawCells = CaroRuleEngine.GetWinningCells(room.GameState, lastRow, lastCol);

            // Chuyển đổi trực tiếp thành cấu trúc thuộc tính viết thường (row, col) để Client đọc được luôn
            winningCellsPayload = rawCells.Select(c => new { row = c.Row, col = c.Col }).ToList();
        }

        var envelope = new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            RoomId = room.RoomId,
            Payload = JsonSerializer.SerializeToElement(new
            {
                status = status.ToString(),
                winnerId,
                board = room.BuildBoardPayload(),
                winningCells = winningCellsPayload // Gửi danh sách đã format chuẩn xuống Client
            })
        };

        foreach (var player in room.GetPlayers())
        {
            try
            {
                await player.SendAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BROADCAST ERROR] {player.Id}: {ex.Message}");
            }
        }
    }
    private GameStatePayload BuildGameStatePayload(GameRoom room)
    {
        string currentTurnId = room.GameState.CurrentPlayer == PlayerSymbol.X
            ? room.PlayerX?.Id.ToString() ?? ""
            : room.PlayerO?.Id.ToString() ?? "";

        return new GameStatePayload
        {
            CurrentTurnPlayerId = currentTurnId,
            Board = room.BuildBoardPayload(),
            IsGameOver = room.GameState.Status != GameStatus.Playing,
            WinnerPlayerId = room.GameState.Status switch
            {
                GameStatus.XWon => room.PlayerX?.Id.ToString(),
                GameStatus.OWon => room.PlayerO?.Id.ToString(),
                _ => null
            }
        };
    }

    private async Task SendErrorAsync(
        ClientSession session, string errorMessage,
        CancellationToken cancellationToken)
    {
        await session.SendAsync(new MessageEnvelope
        {
            Type = MessageType.Error,
            Payload = JsonSerializer.SerializeToElement(new
            {
                message = errorMessage
            })
        }, cancellationToken);
    }

    private string GetPlayerName(Guid sessionId)
    {
        return _playerNames.TryGetValue(sessionId, out string? name)
            ? name
            : "Player";
    }

    private async Task SaveMatchHistoryAsync(GameRoom room, GameStatus status)
    {
        if (_matchHistoryStore is null) return;

        try
        {
            string? winnerName = status switch
            {
                GameStatus.XWon => room.PlayerXName,
                GameStatus.OWon => room.PlayerOName,
                _ => null
            };

            var moves = room.GetMoveHistory();
            var now = DateTime.UtcNow;

            var matchMoves = moves.Select((m, i) =>
                new MatchMoveRecord(i + 1, m.PlayerName, m.Row, m.Col, m.Timestamp))
                .ToList();

            var record = new MatchRecord(
                Guid.NewGuid(),
                room.RoomId,
                room.PlayerXName ?? "Player X",
                room.PlayerOName ?? "Player O",
                winnerName,
                room.StartedAtUtc,
                now,
                matchMoves);

            await _matchHistoryStore.SaveMatchAsync(record);
            Console.WriteLine($"[HISTORY] Saved match {record.MatchId} in room {room.RoomId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HISTORY ERROR] {ex.Message}");
        }
    }

    private async Task HandleRematchAsync(
        ClientSession session,
        MessageEnvelope message,
        CancellationToken cancellationToken)
    {
        GameRoom? room = _roomManager.GetRoomBySession(session.Id);
        if (room is null)
        {
            await SendErrorAsync(session, "Bạn không ở trong phòng chơi nào.", cancellationToken);
            return;
        }

        var (success, bothAccepted, players) = room.HandleRematchRequest(session.Id);
        if (!success) return;

        if (bothAccepted)
        {
            var gameState = BuildGameStatePayload(room);

            foreach (var player in players)
            {
                try
                {
                    var symbol = room.GetPlayerSymbol(player.Id);
                    await player.SendAsync(new MessageEnvelope
                    {
                        Type = MessageType.RematchAcepted,
                        RoomId = room.RoomId,
                        PlayerId = player.Id.ToString(),
                        Payload = JsonSerializer.SerializeToElement(new
                        {
                            roomId = room.RoomId,
                            yourSymbol = symbol?.ToString() ?? "",
                            board = gameState.Board,
                            currentTurnPlayerId = gameState.CurrentTurnPlayerId
                        })
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[REMATCH BROADCAST ERROR] {player.Id}: {ex.Message}");
                }
            }
        }
        else
        {
            var opponent = players.FirstOrDefault(p => p.Id != session.Id);
            if (opponent is not null)
            {
                await opponent.SendAsync(new MessageEnvelope
                {
                    Type = MessageType.ChatReceived,
                    RoomId = room.RoomId,
                    Payload = JsonSerializer.SerializeToElement(new
                    {
                        sender = "Hệ thống",
                        message = "Đối thủ muốn chơi lại! Bấm Chơi lại để bắt đầu trận mới."
                    })
                }, cancellationToken);
            }
        }
    }
}