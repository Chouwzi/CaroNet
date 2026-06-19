using CaroNet.Server.Host.Networking;
using CaroNet.Shared.Game;

namespace CaroNet.Server.Host.GameRooms;

/// <summary>
/// Represents a single game room holding two players and game state.
/// All public methods are guarded by a lock for thread-safety.
/// </summary>
public sealed class GameRoom
{
    private readonly object _lock = new();

    public string RoomId { get; } = Guid.NewGuid().ToString("N")[..8];

    public CaroGameState GameState { get; } = new(15);

    public ClientSession? PlayerX { get; private set; }

    public ClientSession? PlayerO { get; private set; }

    public string? PlayerXName { get; private set; }

    public string? PlayerOName { get; private set; }

    public bool IsFull => PlayerX is not null && PlayerO is not null;

    public bool IsEmpty => PlayerX is null && PlayerO is null;

    /// <summary>
    /// Adds a player to the room. Returns the assigned symbol or null if room is full.
    /// </summary>
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

            return null; // Room is full
        }
    }

    /// <summary>
    /// Removes a player from the room when they disconnect.
    /// </summary>
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

    /// <summary>
    /// Gets the symbol assigned to a session, or null if not in this room.
    /// </summary>
    public PlayerSymbol? GetPlayerSymbol(Guid sessionId)
    {
        lock (_lock)
        {
            if (PlayerX?.Id == sessionId) return PlayerSymbol.X;
            if (PlayerO?.Id == sessionId) return PlayerSymbol.O;
            return null;
        }
    }

    /// <summary>
    /// Attempts to make a move. Thread-safe via lock.
    /// </summary>
    public MoveResult TryMakeMove(Guid sessionId, int row, int column)
    {
        lock (_lock)
        {
            PlayerSymbol? symbol = GetPlayerSymbolUnsafe(sessionId);
            if (symbol is null)
                return new MoveResult(false, GameState.Status, MoveRejectReason.WrongTurn);

            return GameState.MakeMove(new BoardPosition(row, column), symbol.Value);
        }
    }

    /// <summary>
    /// Gets both player sessions (for broadcasting).
    /// </summary>
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

    /// <summary>
    /// Builds the board as string[][] for GameStatePayload.
    /// </summary>
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

    // Internal helper without lock (caller must hold lock)
    private PlayerSymbol? GetPlayerSymbolUnsafe(Guid sessionId)
    {
        if (PlayerX?.Id == sessionId) return PlayerSymbol.X;
        if (PlayerO?.Id == sessionId) return PlayerSymbol.O;
        return null;
    }
}
