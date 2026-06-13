namespace CaroNet.Shared.Protocol.Payloads;

public sealed class GameStatePayload
{
    public string CurrentTurnPlayerId { get; init; } =
        string.Empty;

    public string[][] Board { get; init; } = [];

    public bool IsGameOver { get; init; }

    public string? WinnerPlayerId { get; init; }
}
