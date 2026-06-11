namespace CaroNet.Storage.Statistics;

public sealed class PlayerRecord
{
    public string PlayerName { get; init; } = string.Empty;

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Draws { get; set; }

    public int TotalGames { get; set; }
}