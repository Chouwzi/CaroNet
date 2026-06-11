using CaroNet.Storage.Matches;

namespace CaroNet.Shared.Tests;

public class InMemoryMatchHistoryStoreTests
{
    [Fact]
    public async Task Save_And_Read_Back_Match()
    {
        var store = new InMemoryMatchHistoryStore();

        var matchId = Guid.NewGuid();

        var match = new MatchRecord
        {
            MatchId = matchId,
            RoomId = "ROOM-1",
            PlayerX = "Alice",
            PlayerO = "Bob",
            Winner = "Alice",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            EndedAtUtc = DateTime.UtcNow,
            Moves = new List<MatchMoveRecord>
            {
                new()
                {
                    MoveNumber = 1,
                    Player = "Alice",
                    Row = 7,
                    Column = 7,
                    TimestampUtc = DateTime.UtcNow
                },
                new()
                {
                    MoveNumber = 2,
                    Player = "Bob",
                    Row = 7,
                    Column = 8,
                    TimestampUtc = DateTime.UtcNow
                }
            }
        };

        await store.SaveMatchAsync(match);

        var loaded = await store.GetMatchAsync(matchId);

        Assert.NotNull(loaded);

        Assert.Equal("ROOM-1", loaded!.RoomId);
        Assert.Equal("Alice", loaded.PlayerX);
        Assert.Equal("Bob", loaded.PlayerO);
        Assert.Equal("Alice", loaded.Winner);

        Assert.Equal(2, loaded.Moves.Count);

        Assert.Equal(7, loaded.Moves[0].Row);
        Assert.Equal(8, loaded.Moves[1].Column);
    }
}