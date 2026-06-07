using System.Text.Json;

namespace CaroNet.Shared.Protocol;

public sealed class MessageEnvelope
{
    public MessageType Type { get; init; }

    public string? RequestId { get; init; }

    public string? RoomId { get; init; }

    public string? PlayerId { get; init; }

    public JsonElement Payload { get; init; }
}