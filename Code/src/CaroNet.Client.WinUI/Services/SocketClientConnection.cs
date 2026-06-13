using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Shared;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Services
{
    public sealed class SocketClientConnection : IClientConnection
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private Task? _receiveLoopTask;
        private bool _isDisconnected;

        public bool IsConnected => _client?.Connected == true;

        public event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<Exception>? ConnectionError;
        public event EventHandler? Disconnected;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, cancellationToken);

            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            _isDisconnected = false;

            _receiveLoopTask = ReceiveLoopAsync(_cts.Token);
        }

        public async Task SendAsync(MessageEnvelope message, CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected");

            var bytes = ProtocolCodec.Encode(message);
            await _stream.WriteAsync(bytes, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            if (_isDisconnected)
                return;

            _isDisconnected = true;

            try
            {
                _cts?.Cancel();
                if (_receiveLoopTask != null)
                    await _receiveLoopTask;
            }
            catch { }
            finally
            {
                _stream?.Close();
                _client?.Close();

                _stream = null;
                _client = null;
                _cts = null;

                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_stream == null)
                        break;

                    var message = await ProtocolCodec.DecodeAsync(_stream, cancellationToken);

                    MessageReceived?.Invoke(
                        this,
                        new ClientMessageReceivedEventArgs(message));
                }
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke(this, ex);
            }
            finally
            {
                if (!_isDisconnected)
                {
                    _isDisconnected = true;
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}