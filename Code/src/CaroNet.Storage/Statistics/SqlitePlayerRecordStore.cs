namespace CaroNet.Storage.Statistics;

public sealed class SqlitePlayerRecordStore
    : IPlayerRecordStore
{
    public Task SaveAsync(
        PlayerRecord record,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PlayerRecord?> GetAsync(
        string playerName,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<PlayerRecord>> GetTopPlayersAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}