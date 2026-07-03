using System.Collections.Concurrent;
using CaroNet.Server.Host.Networking;

namespace CaroNet.Server.Host.GameRooms;

// Quản lý danh sách phòng. Giới hạn MaxRooms để tránh tràn tài nguyên.
public sealed class RoomManager
{
    private const int MaxRooms = 100;

    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly ConcurrentDictionary<Guid, string> _sessionRoomMap = new();

    public int RoomCount => _rooms.Count;

    public GameRoom? CreateRoom(ClientSession session, string playerName)
    {
        if (IsSessionInRoom(session.Id))
        {
            return null;
        }

        if (_rooms.Count >= MaxRooms)
            return null;

        var room = new GameRoom();

        if (!_rooms.TryAdd(room.RoomId, room))
            return null;

        room.TryAddPlayer(session, playerName);
        _sessionRoomMap[session.Id] = room.RoomId;

        Console.WriteLine(
            $"[ROOM] Created {room.RoomId} by {playerName}");

        return room;
    }

    public (GameRoom? room, Shared.Game.PlayerSymbol? symbol) JoinRoom(
        ClientSession session, string roomId, string playerName)
    {
        if (IsSessionInRoom(session.Id))
        {
            return (null, null);
        }

        if (!_rooms.TryGetValue(roomId, out GameRoom? room))
            return (null, null);

        var symbol = room.TryAddPlayer(session, playerName);
        if (symbol is null)
            return (null, null);

        _sessionRoomMap[session.Id] = roomId;

        Console.WriteLine(
            $"[ROOM] {playerName} joined {roomId} as {symbol}");

        return (room, symbol);
    }

    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out GameRoom? room);
        return room;
    }

    public GameRoom? GetRoomBySession(Guid sessionId)
    {
        if (_sessionRoomMap.TryGetValue(sessionId, out string? roomId))
            return GetRoom(roomId);
        return null;
    }

    public bool IsSessionInRoom(Guid sessionId)
    {
        return _sessionRoomMap.ContainsKey(sessionId);
    }

    // Xử lý ngắt kết nối: xóa khỏi phòng, dọn phòng trống.
    public GameRoom? HandleDisconnect(Guid sessionId)
    {
        if (!_sessionRoomMap.TryRemove(sessionId, out string? roomId))
            return null;

        if (!_rooms.TryGetValue(roomId, out GameRoom? room))
            return null;

        room.RemovePlayer(sessionId);

        if (room.IsEmpty)
        {
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine($"[ROOM] {roomId} removed (empty)");
        }

        return room;
    }
}
