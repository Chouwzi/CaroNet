using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Services;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class MainMenuViewModel : INotifyPropertyChanged
{
    private readonly IGameClientService _gameClient;
    private string _connectionStatus = "Chưa kết nối";
    private string _host = "127.0.0.1";
    private string _playerName = string.Empty;
    private string _port = "5000";
    private string _roomId = "ROOM-001";

    public MainMenuViewModel(IGameClientService gameClient)
    {
        _gameClient = gameClient;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string RoomId
    {
        get => _roomId;
        set => SetProperty(ref _roomId, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public async Task ConnectAsync()
    {
        if (!int.TryParse(Port, out var port))
        {
            ConnectionStatus = "Port server không hợp lệ.";
            return;
        }

        await _gameClient.ConnectAsync(new ConnectionRequest(PlayerName, Host, port), CancellationToken.None);
        ConnectionStatus = $"Đã connect tới {Host}:{port}";
    }

    public async Task CreateRoomAsync()
    {
        await _gameClient.CreateRoomAsync(CancellationToken.None);
    }

    public async Task JoinRoomAsync()
    {
        await _gameClient.JoinRoomAsync(RoomId, CancellationToken.None);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
