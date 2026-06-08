namespace CaroNet.Storage.Matches;

public sealed class MatchMoveRecord
{
    public int MoveNumber { get; init; }

    public string Player { get; init; } = string.Empty;

    public int Row { get; init; }

    public int Column { get; init; }

    public DateTime TimestampUtc { get; init; }
}