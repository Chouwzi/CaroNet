namespace CaroNet.Storage.Matches;

public sealed class SqliteMatchHistoryStore
    : IMatchHistoryStore
{
    public Task SaveMatchAsync(
        MatchRecord match,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<MatchRecord?> GetMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<MatchRecord>> GetAllMatchesAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}