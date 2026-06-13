namespace CaroNet.Shared.Protocol.Payloads;

public sealed class HelloPayload
{
    public string PlayerName { get; init; } = string.Empty;
}