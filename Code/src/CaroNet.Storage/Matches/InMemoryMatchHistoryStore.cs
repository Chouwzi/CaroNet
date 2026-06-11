using System.Collections.Concurrent;

namespace CaroNet.Storage.Matches;

public sealed class InMemoryMatchHistoryStore
    : IMatchHistoryStore
{
    private readonly ConcurrentDictionary<Guid, MatchRecord> _matches
        = new();

    public Task SaveMatchAsync(
        MatchRecord match,
        CancellationToken cancellationToken = default)
    {
        _matches[match.MatchId] = match;

        return Task.CompletedTask;
    }

    public Task<MatchRecord?> GetMatchAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        _matches.TryGetValue(matchId, out var match);

        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<MatchRecord>> GetAllMatchesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MatchRecord> result =
            _matches.Values.ToList();

        return Task.FromResult(result);
    }
}