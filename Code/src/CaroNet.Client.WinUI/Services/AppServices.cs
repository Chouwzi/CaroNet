using CaroNet.Storage.Matches;
using CaroNet.Storage.Statistics;
using System;
using System.IO;

namespace CaroNet.Client.WinUI.Services;

public static class AppServices
{
    public static IGameClientService GameClient { get; } =
        new SocketGameClientService(new SocketClientConnection());

    private static string FindDatabasePath()
    {
        var current = AppContext.BaseDirectory;

        while (current != null)
        {
            var dbPath = Path.Combine(
                current,
                "src",
                "CaroNet.Server.Host",
                "caronet.db");

            if (File.Exists(dbPath))
            {
                return dbPath;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        // Fallback để app không crash nếu chưa tìm thấy database
        return "caronet.db";
    }

    public static IMatchHistoryStore MatchHistoryStore { get; } =
        new SqliteMatchHistoryStore(FindDatabasePath());
}