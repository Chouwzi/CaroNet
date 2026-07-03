using CaroNet.Server.Host.Networking;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace CaroNet.Server.Host.GameRooms;

// Phòng chơi chứa 2 người và trạng thái ván.
public sealed class GameRoom
{
    private readonly object _lock = new();
    private readonly List<(string PlayerName, int Row, int Col, DateTime Timestamp)> _moveHistory = [];
    private readonly HashSet<Guid> _rematchRequests = [];
    private System.Timers.Timer? _rematchTimer;
    private PlayerSymbol _lastStartingPlayer = PlayerSymbol.X;

    public string RoomId { get; } = Random.Shared.Next(100000, 999999).ToString();

    public CaroGameState GameState { get; } = new(15);

    public DateTime StartedAtUtc { get; private set; }

    public ClientSession? PlayerX { get; private set; }

    public ClientSession? PlayerO { get; private set; }

    public string? PlayerXName { get; private set; }

    public string? PlayerOName { get; private set; }

    public bool IsFull => PlayerX is not null && PlayerO is not null;

    public bool IsEmpty => PlayerX is null && PlayerO is null;

    // Thêm người chơi, trả về ký hiệu (X/O) hoặc null nếu phòng đầy.
    public PlayerSymbol? TryAddPlayer(ClientSession session, string playerName)
    {
        lock (_lock)
        {
            if (PlayerX is null)
            {
                PlayerX = session;
                PlayerXName = playerName;
                return PlayerSymbol.X;
            }

            if (PlayerO is null)
            {
                PlayerO = session;
                PlayerOName = playerName;
                return PlayerSymbol.O;
            }

            return null;
        }
    }

    public void RemovePlayer(Guid sessionId)
    {
        lock (_lock)
        {
            if (PlayerX?.Id == sessionId)
            {
                PlayerX = null;
                PlayerXName = null;
            }
            else if (PlayerO?.Id == sessionId)
            {
                PlayerO = null;
                PlayerOName = null;
            }
        }
    }

    public PlayerSymbol? GetPlayerSymbol(Guid sessionId)
    {
        lock (_lock)
        {
            if (PlayerX?.Id == sessionId) return PlayerSymbol.X;
            if (PlayerO?.Id == sessionId) return PlayerSymbol.O;
            return null;
        }
    }

    public MoveResult TryMakeMove(Guid sessionId, int row, int column)
    {
        lock (_lock)
        {
            PlayerSymbol? symbol = GetPlayerSymbolUnsafe(sessionId);
            if (symbol is null)
                return new MoveResult(false, GameState.Status, MoveRejectReason.WrongTurn);

            string playerName = symbol == PlayerSymbol.X ? PlayerXName! : PlayerOName!;
            var result = GameState.MakeMove(new BoardPosition(row, column), symbol.Value);

            if (result.IsSuccess)
            {
                _moveHistory.Add((playerName, row, column, DateTime.UtcNow));

                // Đánh dấu thời gian bắt đầu ở nước đầu tiên
                if (_moveHistory.Count == 1)
                    StartedAtUtc = _moveHistory[0].Timestamp;
            }

            return result;
        }
    }

    // Lấy danh sách nước đi để lưu lịch sử.
    public IReadOnlyList<(string PlayerName, int Row, int Col, DateTime Timestamp)> GetMoveHistory()
    {
        lock (_lock)
        {
            return _moveHistory.ToList();
        }
    }

    public IReadOnlyList<ClientSession> GetPlayers()
    {
        lock (_lock)
        {
            var list = new List<ClientSession>(2);
            if (PlayerX is not null) list.Add(PlayerX);
            if (PlayerO is not null) list.Add(PlayerO);
            return list;
        }
    }

    // Chuyển board thành string[][] để gửi cho client.
    public string[][] BuildBoardPayload()
    {
        lock (_lock)
        {
            int size = GameState.Size;
            var board = new string[size][];
            for (int r = 0; r < size; r++)
            {
                board[r] = new string[size];
                for (int c = 0; c < size; c++)
                {
                    board[r][c] = GameState[r, c] switch
                    {
                        CellState.X => "X",
                        CellState.O => "O",
                        _ => ""
                    };
                }
            }
            return board;
        }
    }

    private PlayerSymbol? GetPlayerSymbolUnsafe(Guid sessionId)
    {
        if (PlayerX?.Id == sessionId) return PlayerSymbol.X;
        if (PlayerO?.Id == sessionId) return PlayerSymbol.O;
        return null;
    }

    public (bool Success, bool BothAccepted, IReadOnlyList<ClientSession> ActivePlayers) HandleRematchRequest(Guid sessionId)
    {
        lock (_lock)
        {
            if (GameState.Status == GameStatus.Playing) return (false, false, Array.Empty<ClientSession>());
            if (GetPlayerSymbolUnsafe(sessionId) is null || _rematchRequests.Contains(sessionId)) return (false, false, Array.Empty<ClientSession>());

            _rematchRequests.Add(sessionId);
            var players = GetPlayers();

            if (_rematchRequests.Count == 1)
            {
                StartRematchTimeout();
                return (true, false, players);
            }

            if (_rematchRequests.Count == players.Count)
            {
                StopRematchTimeout();
                ExecuteRematchReset();
                return (true, true, players);
            }

            return (true, false, players);
        }
    }

    private void StartRematchTimeout()
    {
        _rematchTimer = new System.Timers.Timer(15000);
        _rematchTimer.Elapsed += OnRematchTimeout;
        _rematchTimer.AutoReset = false;
        _rematchTimer.Start();
    }

    public void StopRematchTimeout()
    {
        if (_rematchTimer is not null)
        {
            _rematchTimer.Stop();
            _rematchTimer.Dispose();
            _rematchTimer = null;
        }
    }

    private void OnRematchTimeout(object? sender, ElapsedEventArgs e)
    {
        IReadOnlyList<ClientSession> players;
        lock (_lock)
        {
            _rematchRequests.Clear();
            StopRematchTimeout();
            players = GetPlayers();
        }

        Task.Run(async () =>
        {
            foreach (var player in players)
            {
                try
                {
                    await player.SendAsync(new MessageEnvelope
                    {
                        Type = MessageType.Error,
                        RoomId = RoomId,
                        Payload = System.Text.Json.JsonSerializer.SerializeToElement(new { message = "Hết thời gian chờ đối thủ đồng ý chơi lại (15s)." })
                    }, System.Threading.CancellationToken.None);
                }
                catch
                {
                }
            }
        });
    }

    private void ExecuteRematchReset()
    {
        _rematchRequests.Clear();
        _moveHistory.Clear();

        // 1. Đảo lượt đi đầu tiên của ván tiếp theo
        _lastStartingPlayer = (_lastStartingPlayer == PlayerSymbol.X) ? PlayerSymbol.O : PlayerSymbol.X;

        // 2. ĐỔI PHE THỰC TẾ: Đảo chỗ PlayerX và PlayerO trong phòng để đồng bộ với ván mới
        var tempPlayer = PlayerX;
        var tempName = PlayerXName;

        PlayerX = PlayerO;
        PlayerXName = PlayerOName;

        PlayerO = tempPlayer;
        PlayerOName = tempName;

        // 3. Gọi hàm reset trạng thái bàn cờ về trống và đặt lượt đi đầu tiên theo quân cờ mới
        GameState.ResetForRematch(_lastStartingPlayer);
        StartedAtUtc = DateTime.UtcNow;
    }
}