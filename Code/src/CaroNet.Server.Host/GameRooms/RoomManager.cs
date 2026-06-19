using System.Collections.Concurrent;
using CaroNet.Server.Host.Networking;

namespace CaroNet.Server.Host.GameRooms;

/// <summary>
/// Manages all active game rooms. Thread-safe via ConcurrentDictionary.
/// Limits max rooms to prevent resource exhaustion (DoS protection).
/// </summary>
public sealed class RoomManager
{
    /// <summary>Max rooms allowed at any time (DoS guard).</summary>
    private const int MaxRooms = 100;

    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    /// <summary>Maps session ID → room ID for fast lookup on disconnect.</summary>
    private readonly ConcurrentDictionary<Guid, string> _sessionRoomMap = new();

    public int RoomCount => _rooms.Count;

    /// <summary>
    /// Creates a new room and adds the creator as Player X.
    /// Returns null if max rooms exceeded.
    /// </summary>
    public GameRoom? CreateRoom(ClientSession session, string playerName)
    {
        if (_rooms.Count >= MaxRooms)
            return null;

        var room = new GameRoom();

        if (!_rooms.TryAdd(room.RoomId, room))
            return null;

        room.TryAddPlayer(session, playerName);
        _sessionRoomMap[session.Id] = room.RoomId;

        Console.WriteLine(
            $"[ROOM] Created room {room.RoomId} by {playerName} ({session.Id})");

        return room;
    }

    /// <summary>
    /// Joins an existing room as Player O.
    /// Returns (room, assignedSymbol) or (null, null) on failure.
    /// </summary>
    public (GameRoom? room, Shared.Game.PlayerSymbol? symbol) JoinRoom(
        ClientSession session, string roomId, string playerName)
    {
        if (!_rooms.TryGetValue(roomId, out GameRoom? room))
            return (null, null);

        var symbol = room.TryAddPlayer(session, playerName);
        if (symbol is null)
            return (null, null);

        _sessionRoomMap[session.Id] = roomId;

        Console.WriteLine(
            $"[ROOM] {playerName} ({session.Id}) joined room {roomId} as {symbol}");

        return (room, symbol);
    }

    /// <summary>
    /// Gets a room by ID.
    /// </summary>
    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out GameRoom? room);
        return room;
    }

    /// <summary>
    /// Gets the room a session is currently in.
    /// </summary>
    public GameRoom? GetRoomBySession(Guid sessionId)
    {
        if (_sessionRoomMap.TryGetValue(sessionId, out string? roomId))
            return GetRoom(roomId);
        return null;
    }

    /// <summary>
    /// Handles a player disconnecting: removes from room, cleans up empty rooms.
    /// </summary>
    public GameRoom? HandleDisconnect(Guid sessionId)
    {
        if (!_sessionRoomMap.TryRemove(sessionId, out string? roomId))
            return null;

        if (!_rooms.TryGetValue(roomId, out GameRoom? room))
            return null;

        room.RemovePlayer(sessionId);

        Console.WriteLine(
            $"[ROOM] Session {sessionId} left room {roomId}");

        // Clean up empty rooms
        if (room.IsEmpty)
        {
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine(
                $"[ROOM] Room {roomId} removed (empty)");
        }

        return room;
    }
}
