using Microsoft.Data.Sqlite;

namespace CaroNet.Storage.Matches;

public sealed class SqliteMatchHistoryStore
    : IMatchHistoryStore
{
    private readonly string _connectionString;

    public SqliteMatchHistoryStore(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task SaveMatchAsync(
        MatchRecord match,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var insertMatch = connection.CreateCommand();

        insertMatch.CommandText =
            """
            INSERT OR REPLACE INTO Matches
            (
                MatchId,
                RoomId,
                PlayerXName,
                PlayerOName,
                WinnerName,
                StartedAtUtc,
                EndedAtUtc
            )
            VALUES
            (
                $matchId,
                $roomId,
                $playerX,
                $playerO,
                $winner,
                $started,
                $ended
            );
            """;

        insertMatch.Parameters.AddWithValue(
            "$matchId",
            match.MatchId.ToString());

        insertMatch.Parameters.AddWithValue(
            "$roomId",
            match.RoomId);

        insertMatch.Parameters.AddWithValue(
            "$playerX",
            match.PlayerXName);

        insertMatch.Parameters.AddWithValue(
            "$playerO",
            match.PlayerOName);

        insertMatch.Parameters.AddWithValue(
            "$winner",
            (object?)match.WinnerName ?? DBNull.Value);

        insertMatch.Parameters.AddWithValue(
            "$started",
            match.StartedAtUtc.ToString("O"));

        insertMatch.Parameters.AddWithValue(
            "$ended",
            match.EndedAtUtc?.ToString("O") ?? (object)DBNull.Value);

        await insertMatch.ExecuteNonQueryAsync(cancellationToken);

        foreach (var move in match.Moves)
        {
            var insertMove = connection.CreateCommand();

            insertMove.CommandText =
                """
                INSERT INTO MatchMoves
                (
                    MatchId,
                    MoveNumber,
                    PlayerName,
                    Row,
                    Column,
                    TimestampUtc
                )
                VALUES
                (
                    $matchId,
                    $moveNumber,
                    $player,
                    $row,
                    $column,
                    $timestamp
                );
                """;

            insertMove.Parameters.AddWithValue(
                "$matchId",
                match.MatchId.ToString());

            insertMove.Parameters.AddWithValue(
                "$moveNumber",
                move.MoveNumber);

            insertMove.Parameters.AddWithValue(
                "$player",
                move.PlayerName);

            insertMove.Parameters.AddWithValue(
                "$row",
                move.Row);

            insertMove.Parameters.AddWithValue(
                "$column",
                move.Column);

            insertMove.Parameters.AddWithValue(
                "$timestamp",
                move.TimestampUtc.ToString("O"));

            await insertMove.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<MatchRecord?> GetMatchAsync(
    Guid matchId,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var matchCommand = connection.CreateCommand();

        matchCommand.CommandText =
            """
        SELECT
            MatchId,
            RoomId,
            PlayerXName,
            PlayerOName,
            WinnerName,
            StartedAtUtc,
            EndedAtUtc
        FROM Matches
        WHERE MatchId = $matchId;
        """;

        matchCommand.Parameters.AddWithValue(
            "$matchId",
            matchId.ToString());

        await using var reader =
            await matchCommand.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var roomId = reader.GetString(1);
        var playerX = reader.GetString(2);
        var playerO = reader.GetString(3);

        string? winner =
            reader.IsDBNull(4)
                ? null
                : reader.GetString(4);

        var startedAtUtc =
            DateTime.Parse(reader.GetString(5));

        DateTime? endedAtUtc =
            reader.IsDBNull(6)
                ? null
                : DateTime.Parse(reader.GetString(6));

        var moves = new List<MatchMoveRecord>();

        var moveCommand = connection.CreateCommand();

        moveCommand.CommandText =
            """
        SELECT
            MoveNumber,
            PlayerName,
            Row,
            Column,
            TimestampUtc
        FROM MatchMoves
        WHERE MatchId = $matchId
        ORDER BY MoveNumber;
        """;

        moveCommand.Parameters.AddWithValue(
            "$matchId",
            matchId.ToString());

        await using var moveReader =
            await moveCommand.ExecuteReaderAsync(cancellationToken);

        while (await moveReader.ReadAsync(cancellationToken))
        {
            moves.Add(
                new MatchMoveRecord(
                    moveReader.GetInt32(0),
                    moveReader.GetString(1),
                    moveReader.GetInt32(2),
                    moveReader.GetInt32(3),
                    DateTime.Parse(moveReader.GetString(4))));
        }

        return new MatchRecord(
            matchId,
            roomId,
            playerX,
            playerO,
            winner,
            startedAtUtc,
            endedAtUtc,
            moves);
    }

    public async Task<IReadOnlyList<MatchRecord>> GetAllMatchesAsync(
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
            """
        SELECT MatchId
        FROM Matches
        ORDER BY EndedAtUtc DESC;
        """;

        var matchIds = new List<Guid>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            matchIds.Add(
                Guid.Parse(reader.GetString(0)));
        }

        var matches = new List<MatchRecord>();

        foreach (var matchId in matchIds)
        {
            var match = await GetMatchAsync(
                matchId,
                cancellationToken);

            if (match is not null)
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    public async Task<IReadOnlyList<MatchRecord>> GetMatchesByPlayerAsync(
    string playerName,
    CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText =
            """
        SELECT MatchId
        FROM Matches
        WHERE PlayerXName = $playerName
           OR PlayerOName = $playerName
        ORDER BY EndedAtUtc DESC;
        """;

        command.Parameters.AddWithValue(
            "$playerName",
            playerName);

        var matchIds = new List<Guid>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            matchIds.Add(
                Guid.Parse(reader.GetString(0)));
        }

        var matches = new List<MatchRecord>();

        foreach (var matchId in matchIds)
        {
            var match = await GetMatchAsync(
                matchId,
                cancellationToken);

            if (match is not null)
            {
                matches.Add(match);
            }
        }

        return matches;
    }
}