namespace CaroNet.Storage;

using CaroNet.Storage;
public sealed class MatchRecord
{
    public Guid MatchId { get; init; }

    public string RoomId { get; init; } = string.Empty;

    public string PlayerX { get; init; } = string.Empty;

    public string PlayerO { get; init; } = string.Empty;

    public string? Winner { get; init; }

    public DateTime StartedAtUtc { get; init; }

    public DateTime EndedAtUtc { get; init; }

    public IReadOnlyList<MatchMoveRecord> Moves { get; init; }
        = Array.Empty<MatchMoveRecord>();
}