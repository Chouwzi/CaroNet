using CaroNet.Storage.Statistics;

namespace CaroNet.Shared.Tests;

public class PlayerRecordStoreTests
{
    [Fact]
    public async Task Save_And_Read_Player_Record()
    {
        var store = new InMemoryPlayerRecordStore();

        var record = new PlayerRecord
        {
            PlayerName = "Alice",
            Wins = 10,
            Losses = 2,
            Draws = 1,
            TotalGames = 13
        };

        await store.SaveAsync(record);

        var loaded = await store.GetAsync("Alice");

        Assert.NotNull(loaded);
        Assert.Equal(10, loaded!.Wins);
        Assert.Equal(13, loaded.TotalGames);
    }
}