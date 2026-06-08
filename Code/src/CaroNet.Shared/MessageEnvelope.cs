using CaroNet.Shared;
namespace CaroNet.Shared;

public sealed class MessageEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
}