namespace CaroNet.Shared.Protocol;

public enum MessageType
{
    // Client -> Server
    Hello = 1,
    CreateRoom = 2,
    JoinRoom = 3,
    Ready = 4,
    MakeMove = 5,
    Chat = 6,
    Heartbeat = 7,
    Reconnect = 8,

    // Server -> Client
    HelloAccepted = 50,
    RoomListUpdated = 51,
    RoomJoined = 52,
    GameStarted = 53,
    MoveAccepted = 54,
    MoveRejected = 55,
    GameStateUpdated = 56,
    GameEnded = 57,
    ChatReceived = 58,

    Error = 100
}
