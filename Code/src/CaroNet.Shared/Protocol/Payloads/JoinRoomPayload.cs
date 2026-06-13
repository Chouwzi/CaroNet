namespace CaroNet.Shared.Protocol.Payloads;

public sealed class JoinRoomPayload
{
    public string RoomId { get; init; } = string.Empty;
}