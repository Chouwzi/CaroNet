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
    HelloAccepted = 20,
    RoomListUpdated = 21,
    RoomJoined = 22,
    GameStarted = 23,
    MoveAccepted = 24,
    MoveRejected = 25,
    GameStateUpdated = 26,
    GameEnded = 27,
    ChatReceived = 28,

    Error = 100
}