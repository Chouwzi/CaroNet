using Microsoft.Data.Sqlite;

namespace CaroNet.Storage.Database;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);

        connection.Open();

        CreateMatchesTable(connection);
        CreateMatchMovesTable(connection);
        CreatePlayerRecordsTable(connection);
    }

    private static void CreateMatchesTable(
        SqliteConnection connection)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS Matches
            (
                MatchId TEXT PRIMARY KEY,
                RoomId TEXT NOT NULL,
                PlayerXName TEXT NOT NULL,
                PlayerOName TEXT NOT NULL,
                WinnerName TEXT,
                StartedAtUtc TEXT NOT NULL,
                EndedAtUtc TEXT NOT NULL
            );
            """;

        using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.ExecuteNonQuery();
    }

    private static void CreateMatchMovesTable(
        SqliteConnection connection)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS MatchMoves
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchId TEXT NOT NULL,
                MoveNumber INTEGER NOT NULL,
                PlayerName TEXT NOT NULL,
                Row INTEGER NOT NULL,
                Column INTEGER NOT NULL,
                TimestampUtc TEXT NOT NULL
            );
            """;

        using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.ExecuteNonQuery();
    }

    private static void CreatePlayerRecordsTable(
        SqliteConnection connection)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS PlayerRecords
            (
                PlayerName TEXT PRIMARY KEY,
                Wins INTEGER NOT NULL,
                Losses INTEGER NOT NULL,
                Draws INTEGER NOT NULL
            );
            """;

        using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.ExecuteNonQuery();
    }
}