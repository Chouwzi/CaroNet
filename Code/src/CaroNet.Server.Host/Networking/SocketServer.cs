using System.Net;
using System.Net.Sockets;
using CaroNet.Server.Host.Services;

namespace CaroNet.Server.Host.Networking;

public sealed class SocketServer
{
    private const int ListenPort = 5000;

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

        try
        {
            _listener.Bind(
                new IPEndPoint(
                    IPAddress.Any,
                    ListenPort));

            _listener.Listen(100);

            Console.WriteLine(
                $"[SERVER] Listening on port {ListenPort}");

            while (!cancellationToken.IsCancellationRequested)
            {
                Socket socket;

                try
                {
                    socket =
                        await _listener.AcceptAsync(
                            cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var session =
                    new ClientSession(
                        socket,
                        _dispatcher);

                _registry.Add(session);

                Console.WriteLine(
                    $"[SERVER] Client connected. Online={_registry.Count}");

                // Bug fix: do NOT pass cancellationToken to Task.Run as scheduler CT.
                // Passing it there would cancel the *scheduling* of the task, so if the
                // token fires before the task is scheduled, the finally block that removes
                // the session from the registry would never execute (session leak).
                // The lambda already receives the token via the closure and forwards it
                // to session.RunAsync, which handles graceful shutdown internally.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await session.RunAsync(cancellationToken);
                    }
                    finally
                    {
                        _registry.Remove(session.Id);

                        // Notify dispatcher so room cleanup happens
                        if (_dispatcher is GameMessageDispatcher gameDispatcher)
                        {
                            await gameDispatcher.HandleDisconnectAsync(session.Id);
                        }

                        Console.WriteLine(
                            $"[SERVER] Client removed. Online={_registry.Count}");
                    }
                });
            }
        }
        finally
        {
            // Bug fix: dispose listener so the OS releases port ListenPort immediately.
            // Without this, the port stays bound until GC finalizes the socket.
            _listener.Dispose();

            Console.WriteLine(
                "[SERVER] Listener socket closed.");
        }
    }
}