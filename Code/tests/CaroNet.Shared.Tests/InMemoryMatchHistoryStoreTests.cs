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
            StartedAtUtc = DateTime.UtcNow,
            EndedAtUtc = DateTime.UtcNow
        };

        await store.SaveMatchAsync(match);

        var loaded = await store.GetMatchAsync(matchId);

        Assert.NotNull(loaded);
    }
}