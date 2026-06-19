using CaroNet.Server.Host.GameRooms;
using CaroNet.Server.Host.Networking;
using CaroNet.Server.Host.Services;

var cancellationTokenSource =
    new CancellationTokenSource();

Console.CancelKeyPress += (
    sender,
    eventArgs) =>
{
    Console.WriteLine(
        "[SERVER] Shutdown requested...");

    eventArgs.Cancel = true;

    cancellationTokenSource.Cancel();
};

var registry =
    new ClientSessionRegistry();

var roomManager =
    new RoomManager();

var dispatcher =
    new GameMessageDispatcher(
        roomManager,
        registry);

var server =
    new SocketServer(
        registry,
        dispatcher);

try
{
    await server.RunAsync(
        cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine(
        "[SERVER] Stopped.");
}