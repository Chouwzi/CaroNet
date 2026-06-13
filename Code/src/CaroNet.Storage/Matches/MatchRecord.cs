namespace CaroNet.Storage.Matches;

public sealed record MatchRecord(
    Guid MatchId,
    string RoomId,
    string PlayerXName,
    string PlayerOName,
    string? WinnerName,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    IReadOnlyList<MatchMoveRecord> Moves)
{
    public bool IsCompleted => EndedAtUtc.HasValue;
}
