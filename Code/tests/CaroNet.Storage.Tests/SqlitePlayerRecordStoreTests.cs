using CaroNet.Storage.Database;
using CaroNet.Storage.Statistics;

namespace CaroNet.Storage.Tests;

public sealed class SqlitePlayerRecordStoreTests
{
    [Fact]
    public async Task Save_And_Read_Back_Player_Record()
    {
        var databasePath =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.db");

        var initializer =
            new DatabaseInitializer(databasePath);

        initializer.Initialize();

        var store =
            new SqlitePlayerRecordStore(databasePath);

        var record =
            new PlayerRecord(
                "Alice",
                10,
                2,
                1);

        await store.SaveAsync(record);

        var loaded =
            await store.GetAsync("Alice");

        Assert.NotNull(loaded);

        Assert.Equal(
            record.PlayerName,
            loaded!.PlayerName);

        Assert.Equal(
            record.Wins,
            loaded.Wins);

        Assert.Equal(
            record.Losses,
            loaded.Losses);

        Assert.Equal(
            record.Draws,
            loaded.Draws);
    }
}