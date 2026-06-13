using System.Net;
using System.Net.Sockets;
using CaroNet.Server.Host.Services;

namespace CaroNet.Server.Host.Networking;

public sealed class SocketServer
{
    private readonly ClientSessionRegistry _registry;
    private readonly IMessageDispatcher _dispatcher;

    private Socket? _listener;

    public SocketServer(
        ClientSessionRegistry registry,
        IMessageDispatcher dispatcher)
    {
        _registry = registry;
        _dispatcher = dispatcher;
    }

    public async Task RunAsync(
        CancellationToken cancellationToken)
    {
        _listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        _listener.Bind(
            new IPEndPoint(
                IPAddress.Any,
                5000));

        _listener.Listen(100);

        Console.WriteLine(
            "[SERVER] Listening on port 5000");

        while (!cancellationToken.IsCancellationRequested)
        {
            Socket socket =
                await _listener.AcceptAsync(
                    cancellationToken);

            var session =
                new ClientSession(
                   socket,
                      _dispatcher);

            _registry.Add(session);

            Console.WriteLine(
                $"[SERVER] Client connected. Online={_registry.Count}");

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await session.RunAsync(
                            cancellationToken);
                    }
                    finally
                    {
                        _registry.Remove(
                            session.Id);

                        Console.WriteLine(
                            $"[SERVER] Client removed. Online={_registry.Count}");
                    }
                },
                cancellationToken);
        }
    }
}