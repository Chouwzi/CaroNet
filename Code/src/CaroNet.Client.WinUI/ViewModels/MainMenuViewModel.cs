using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Services;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class MainMenuViewModel : INotifyPropertyChanged
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;

    private readonly IGameClientService _gameClient;
    private string _connectionStatus = "Chưa kết nối";
    private string _playerName = string.Empty;
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
        await _gameClient.ConnectAsync(new ConnectionRequest(PlayerName, DefaultHost, DefaultPort), CancellationToken.None);
        ConnectionStatus = $"Đã connect tới server mặc định {DefaultHost}:{DefaultPort}";
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
