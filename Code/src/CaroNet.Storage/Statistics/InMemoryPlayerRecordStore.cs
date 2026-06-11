using System.Collections.Concurrent;

namespace CaroNet.Storage.Statistics;

public sealed class InMemoryPlayerRecordStore
    : IPlayerRecordStore
{
    private readonly ConcurrentDictionary<string, PlayerRecord>
        _records = new();

    public Task SaveAsync(
        PlayerRecord record,
        CancellationToken cancellationToken = default)
    {
        _records[record.PlayerName] = record;

        return Task.CompletedTask;
    }

    public Task<PlayerRecord?> GetAsync(
        string playerName,
        CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(playerName, out var record);

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<PlayerRecord>> GetTopPlayersAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlayerRecord> result =
            _records.Values
                .OrderByDescending(x => x.Wins)
                .Take(count)
                .ToList();

        return Task.FromResult(result);
    }
}