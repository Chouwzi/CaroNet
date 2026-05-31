namespace CaroNet.Shared.Protocol;

public enum MessageType
{
    Hello = 1,
    CreateRoom = 2,
    JoinRoom = 3,
    Ready = 4,
    MakeMove = 5,
    Chat = 6,
    Heartbeat = 7,
    Reconnect = 8,
    Error = 100
}
