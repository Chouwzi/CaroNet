namespace CaroNet.Client.WinUI.Services;

public static class AppServices
{
    public static IGameClientService GameClient { get; } =
        new SocketGameClientService(new SocketClientConnection());
}
