namespace CaroNet.Shared.Protocol.Payloads;

public sealed class GameEndedPayload
{
    public string? WinnerPlayerId { get; init; }

    public string? Reason { get; init; }

    public string[][] Board { get; init; } = [];
}