namespace CaroNet.Shared.Protocol.Payloads;

public sealed class CreateRoomPayload
{
    public string RoomName { get; init; } = string.Empty;
}