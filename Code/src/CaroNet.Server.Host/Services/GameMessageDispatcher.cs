using System.Text.Json;
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
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime> _lastRequestTimes = new();

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

        // Rate limiting per-session: giới hạn tối đa 10 requests/giây (khoảng cách tối thiểu 100ms) (Issue #53)
        var now = DateTime.UtcNow;
        if (_lastRequestTimes.TryGetValue(session.Id, out var lastTime) && (now - lastTime).TotalMilliseconds < 100)
        {
            await SendErrorAsync(session, "Rate limit exceeded.", cancellationToken);
            return;
        }
        _lastRequestTimes[session.Id] = now;

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

            case MessageType.Chat:
                await HandleChatAsync(session, message, cancellationToken);
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
        _lastRequestTimes.TryRemove(sessionId, out _); // Dọn dẹp bộ giới hạn tốc độ (Issue #53)

        GameRoom? room = _roomManager.HandleDisconnect(sessionId);
        if (room is null) return;

        // Nếu vẫn còn người chơi trong phòng thì người đó thắng
    foreach (var player in room.GetPlayers())
    {
    try
    {
        Console.WriteLine(
        $"[DISCONNECT] Sending GameEnded to {player.Id}");
        await player.SendAsync(
            new MessageEnvelope
            {
                Type = MessageType.GameEnded,
                RoomId = room.RoomId,
                Payload = JsonSerializer.SerializeToElement(new GameEndedPayload
                {
                 WinnerPlayerId = player.Id.ToString(),
                 Reason = "opponent_disconnected",
                 Board = room.BuildBoardPayload()
                 })
            },
            CancellationToken.None);
             Console.WriteLine(
            $"[DISCONNECT] GameEnded sent to {player.Id}");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"[DISCONNECT] Failed to notify player {player.Id}: {ex.Message}");
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

        playerName = CaroNet.Server.Host.Validation.PlayerNameSanitizer.Sanitize(playerName);

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
            await BroadcastGameEndedAsync(room, result.Status, cancellationToken);
            await SaveMatchHistoryAsync(room, result.Status);
        }
    }

    private async Task HandleChatAsync(
    ClientSession session,
    MessageEnvelope message,
    CancellationToken cancellationToken)
    {
        // 1. Kiểm tra xem người chơi đã ở trong Room chưa (Issue: không cho gửi khi chưa vào room)
        GameRoom? room = _roomManager.GetRoomBySession(session.Id);
        if (room is null) return;

        // 2. Kiểm tra dữ liệu gói tin gửi lên
        if (!message.Payload.HasValue) return;

        try
        {
            var chatPayload = message.Payload.Value.Deserialize<ChatPayload>();
            string? processedMessage = chatPayload?.Message?.Trim();

            // Input validation: Reject nếu rỗng hoặc quá 200 ký tự
            if (string.IsNullOrEmpty(processedMessage) || processedMessage.Length > 200)
                return;

            // 3. Lấy tên người gửi đã đăng ký trong hệ thống Server
            string senderName = GetPlayerName(session.Id);

            // 4. Tạo Payload phát sóng chuẩn ChatReceivedPayload
            var broadcastPayload = new ChatReceivedPayload
            {
                SenderName = senderName,
                Message = processedMessage,
                Timestamp = DateTime.Now
            };

            // 5. Đóng gói vào MessageEnvelope phát sóng cho cả room
            var envelope = new MessageEnvelope
            {
                Type = MessageType.ChatReceived,
                RoomId = room.RoomId,
                Payload = JsonSerializer.SerializeToElement(broadcastPayload)
            };

            // 6. Tiến hành Broadcast tới tất cả các Player trong Room (bắt chước logic MakeMove)
            foreach (var player in room.GetPlayers())
            {
                try
                {
                    await player.SendAsync(envelope, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CHAT BROADCAST ERROR] {player.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CHAT ERROR] {session.Id}: {ex.Message}");
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

    private async Task BroadcastGameEndedAsync(
        GameRoom room, GameStatus status, CancellationToken cancellationToken)
    {
        string? winnerId = status switch
        {
            GameStatus.XWon => room.PlayerX?.Id.ToString(),
            GameStatus.OWon => room.PlayerO?.Id.ToString(),
            _ => null
        };

        var envelope = new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            RoomId = room.RoomId,
            Payload = JsonSerializer.SerializeToElement(
             new GameEndedPayload
            {
            WinnerPlayerId = winnerId,
            Reason = null,
             Board = room.BuildBoardPayload()
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
                Console.WriteLine(
                    $"[BROADCAST ERROR] {player.Id}: {ex.Message}");
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
}
