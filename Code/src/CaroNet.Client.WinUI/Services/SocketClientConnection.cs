using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Services
{
    public sealed class SocketClientConnection : IClientConnection
    {
        private Socket? _socket;
        private CancellationTokenSource? _cts;

        public bool IsConnected => _socket?.Connected ?? false;

        public event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<string>? Disconnected;

        public async Task ConnectAsync(string host, int port)
        {
            if (IsConnected)
                throw new InvalidOperationException("Already connected");

            _socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            await _socket.ConnectAsync(host, port);

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }

        public async Task SendAsync(MessageEnvelope message)
        {
            if (!IsConnected || _socket == null)
                throw new InvalidOperationException("Not connected");

            byte[] frame = ProtocolCodec.Encode(message);
            await _socket.SendAsync(frame, SocketFlags.None);
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] header = await ReceiveExactAsync(4, token);
                    if (header.Length == 0)
                        break;

                    int length =
                        (header[0] << 24) |
                        (header[1] << 16) |
                        (header[2] << 8) |
                        header[3];

                    byte[] payload = await ReceiveExactAsync(length, token);
                    if (payload.Length == 0)
                        break;

                    var message = ProtocolCodec.Decode(payload);

                    MessageReceived?.Invoke(
                        this,
                        new ClientMessageReceivedEventArgs(message)
                    );
                }
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, ex.Message);
            }
            finally
            {
                await DisconnectAsync();
            }
        }

        private async Task<byte[]> ReceiveExactAsync(int size, CancellationToken token)
        {
            if (_socket == null)
                return Array.Empty<byte>();

            byte[] buffer = new byte[size];
            int received = 0;

            while (received < size)
            {
                int bytes = await _socket.ReceiveAsync(
                    buffer.AsMemory(received, size - received),
                    SocketFlags.None,
                    token);

                if (bytes == 0)
                    return Array.Empty<byte>();

                received += bytes;
            }

            return buffer;
        }

        public Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
            }
            catch { }

            _socket = null;
            _cts = null;

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(DisconnectAsync());
        }
    }
}