using Microsoft.Data.Sqlite;

namespace CaroNet.Storage.Statistics;

public sealed class SqlitePlayerRecordStore
    : IPlayerRecordStore
{
    private readonly string _connectionString;

    public SqlitePlayerRecordStore(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task SaveAsync(
        PlayerRecord record,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT OR REPLACE INTO PlayerRecords
            (
                PlayerName,
                Wins,
                Losses,
                Draws
            )
            VALUES
            (
                $playerName,
                $wins,
                $losses,
                $draws
            );
            """;

        command.Parameters.AddWithValue(
            "$playerName",
            record.PlayerName);

        command.Parameters.AddWithValue(
            "$wins",
            record.Wins);

        command.Parameters.AddWithValue(
            "$losses",
            record.Losses);

        command.Parameters.AddWithValue(
            "$draws",
            record.Draws);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PlayerRecord?> GetAsync(
    string playerName,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
            """
        SELECT
            PlayerName,
            Wins,
            Losses,
            Draws
        FROM PlayerRecords
        WHERE PlayerName = $playerName;
        """;

        command.Parameters.AddWithValue(
            "$playerName",
            playerName);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PlayerRecord(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3));
    }

    public async Task<IReadOnlyList<PlayerRecord>> GetTopPlayersAsync(
    int limit,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
            """
        SELECT
            PlayerName,
            Wins,
            Losses,
            Draws
        FROM PlayerRecords
        ORDER BY Wins DESC
        LIMIT $limit;
        """;

        command.Parameters.AddWithValue(
            "$limit",
            limit);

        var records = new List<PlayerRecord>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(
                new PlayerRecord(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)));
        }

        return records;
    }
}