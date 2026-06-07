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
            PlayerX = "Bao",
            PlayerO = "Chuong",
            Winner = "Bao",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            EndedAtUtc = DateTime.UtcNow,
            Moves = new List<MatchMoveRecord>
            {
                new()
                {
                    MoveNumber = 1,
                    Player = "Bao",
                    Row = 7,
                    Column = 7,
                    TimestampUtc = DateTime.UtcNow
                },
                new()
                {
                    MoveNumber = 2,
                    Player = "Chuong",
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
        Assert.Equal("Bao", loaded.PlayerX);
        Assert.Equal("Chuong", loaded.PlayerO);
        Assert.Equal("Bao", loaded.Winner);
        Assert.Equal(2, loaded.Moves.Count);
    }
}