using CaroNet.Server.Host.Networking;
using CaroNet.Shared.Game;

namespace CaroNet.Server.Host.GameRooms;

// Phòng chơi chứa 2 người và trạng thái ván.
public sealed class GameRoom
{
    private readonly object _lock = new();
    private readonly List<(string PlayerName, int Row, int Col, DateTime Timestamp)> _moveHistory = [];

    public string RoomId { get; } = Guid.NewGuid().ToString("N")[..8];

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
}
