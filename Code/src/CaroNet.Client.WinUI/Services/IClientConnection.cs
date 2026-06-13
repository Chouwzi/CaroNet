
﻿using CaroNet.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.Services;

public interface IClientConnection : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(string host, int port, CancellationToken cancellationToken);

    Task DisconnectAsync();

    Task SendAsync(
        MessageEnvelope message,
        CancellationToken cancellationToken);

    event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
    event EventHandler<Exception>? ConnectionError;
    event EventHandler? Disconnected;

﻿using System;
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