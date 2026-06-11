namespace CaroNet.Storage.Matches;

public interface IMatchHistoryStore
{
    Task SaveMatchAsync(
        MatchRecord match,
        CancellationToken cancellationToken = default);

    Task<MatchRecord?> GetMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchRecord>> GetAllMatchesAsync(
        CancellationToken cancellationToken = default);
}