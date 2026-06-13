using System;
using System.Threading.Tasks;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Services
{
    public interface IClientConnection : IAsyncDisposable
    {
        bool IsConnected { get; }

        event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
        event EventHandler<string>? Disconnected;

        Task ConnectAsync(string host, int port);
        Task SendAsync(MessageEnvelope message);
        Task DisconnectAsync();
    }
}