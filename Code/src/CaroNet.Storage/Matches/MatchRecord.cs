namespace CaroNet.Storage.Matches;

public sealed class MatchRecord
{
    public Guid MatchId { get; init; }

    public string RoomId { get; init; } = string.Empty;

    public string PlayerX { get; init; } = string.Empty;

    public string PlayerO { get; init; } = string.Empty;

    public string? Winner { get; init; }

    public DateTime StartedAtUtc { get; init; }

    public DateTime EndedAtUtc { get; init; }

    public List<MatchMoveRecord> Moves { get; init; } = new();
}