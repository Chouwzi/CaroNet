using System;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Threading;
using CaroNet.Client.WinUI.Services;
using CaroNet.Shared;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainWindow : Window
{
    private SocketClientConnection? _client;

    public MainWindow()
    {
        InitializeComponent();


        this.Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.Activated -= MainWindow_Activated;

        _client = new SocketClientConnection();

        _client.MessageReceived += (s, e) =>
        {
            Debug.WriteLine("SERVER: " + e.Message.Type);
        };

        _client.ConnectionError += (s, e) =>
        {
            Debug.WriteLine("ERROR: " + e.Message);
        };

        _client.Disconnected += (s, e) =>
        {
            Debug.WriteLine("DISCONNECTED");
        };

        try
        {
            await _client.ConnectAsync("127.0.0.1", 5000, CancellationToken.None);

            Debug.WriteLine("CONNECTED");

            await _client.SendAsync(
                new MessageEnvelope
                {
                    Type = "Hello"
                },
                CancellationToken.None);

            Debug.WriteLine("HELLO SENT");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("CONNECT FAILED: " + ex.Message);
        }

    }
}