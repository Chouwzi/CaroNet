using System.Net;
using System.Net.Sockets;
using CaroNet.Shared;
using CaroNet.Shared.Protocol;

namespace CaroNet.Server.Host;

internal class Program
{
    static async Task Main()
    {
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();

        Console.WriteLine("CaroNet Server Host");
        Console.WriteLine("Server is running on port 5000");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected");

            _ = Task.Run(async () =>
            {
                try
                {
                    var stream = client.GetStream();
                    var buffer = new byte[4096];

                    while (true)
                    {
                        var read = await stream.ReadAsync(buffer);
                        if (read == 0) break;

                        var msg = ProtocolCodec.Decode(buffer[..read]);

                        Console.WriteLine($"Received: {msg.Type}");

                        if (msg.Type == "Hello")
                        {
                            var response = new MessageEnvelope
                            {
                                Type = "HelloAccepted",
                                Payload = "Welcome"
                            };

                            var bytes = ProtocolCodec.Encode(response);
                            await stream.WriteAsync(bytes);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client error: {ex.Message}");
                }

                Console.WriteLine("Client disconnected");
            });
        }
    }
}