using Microsoft.Data.Sqlite;

namespace CaroNet.Storage.Database;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string databasePath)
    {
        _connectionString = SqliteConnectionFactory.CreateConnectionString(databasePath);
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);

        connection.Open();

        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText =
            """
            PRAGMA foreign_keys = ON;
            PRAGMA journal_mode = WAL;
            """;
        pragmaCommand.ExecuteNonQuery();

        CreateMatchesTable(connection);
        CreateMatchMovesTable(connection);
        CreatePlayerRecordsTable(connection);
    }

    private static void CreateMatchesTable(SqliteConnection connection)
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

    private static void CreateMatchMovesTable(SqliteConnection connection)
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
                TimestampUtc TEXT NOT NULL,
                UNIQUE (MatchId, MoveNumber),
                FOREIGN KEY (MatchId) REFERENCES Matches(MatchId) ON DELETE CASCADE
            );
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void CreatePlayerRecordsTable(SqliteConnection connection)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS PlayerRecords
            (
                PlayerName TEXT PRIMARY KEY,
                Wins INTEGER NOT NULL CHECK (Wins >= 0),
                Losses INTEGER NOT NULL CHECK (Losses >= 0),
                Draws INTEGER NOT NULL CHECK (Draws >= 0)
            );
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
