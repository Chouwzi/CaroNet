using CaroNet.Storage.Matches;
using CaroNet.Storage.Statistics;

namespace CaroNet.Client.WinUI.Services;

public static class AppServices
{
    public static IGameClientService GameClient { get; } =
        new SocketGameClientService(new SocketClientConnection());

    public static IMatchHistoryStore MatchHistoryStore { get; } =
        new SqliteMatchHistoryStore(
            @"C:\Users\Dell\source\repos\Chouwzi\CaroNet\Code\src\CaroNet.Server.Host\caronet.db");

    
}