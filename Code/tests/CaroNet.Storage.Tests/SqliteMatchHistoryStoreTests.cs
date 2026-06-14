using CaroNet.Storage.Database;
using CaroNet.Storage.Matches;

namespace CaroNet.Storage.Tests;

public sealed class SqliteMatchHistoryStoreTests
{
    [Fact]
    public async Task Save_And_Read_Back_Match()
    {
        var databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db");

        var initializer =
            new DatabaseInitializer(databasePath);

        initializer.Initialize();

        var store =
            new SqliteMatchHistoryStore(databasePath);

        var matchId = Guid.NewGuid();

        var match =
            new MatchRecord(
                matchId,
                "ROOM-1",
                "Alice",
                "Bob",
                "Alice",
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow,
                new List<MatchMoveRecord>
                {
                    new(
                        1,
                        "Alice",
                        7,
                        7,
                        DateTime.UtcNow),

                    new(
                        2,
                        "Bob",
                        7,
                        8,
                        DateTime.UtcNow)
                });

        await store.SaveMatchAsync(match);

        var loaded =
            await store.GetMatchAsync(matchId);

        Assert.NotNull(loaded);

        Assert.Equal(
            match.RoomId,
            loaded!.RoomId);

        Assert.Equal(
            match.PlayerXName,
            loaded.PlayerXName);

        Assert.Equal(
            match.PlayerOName,
            loaded.PlayerOName);

        Assert.Equal(
            2,
            loaded.Moves.Count);
    }
}