using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.Validation;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class MainMenuViewModel : INotifyPropertyChanged
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 5000;

    private readonly IGameClientService _gameClient;
    private string _connectionStatus = "Chưa kết nối";
    private string _playerName = string.Empty;
    private string _roomId = string.Empty;
    private bool _isConnected;

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

    public async Task<bool> ConnectAsync()
    {
        string? playerNameError = PlayerNameValidator.Validate(PlayerName);
        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }
        try
        {
            await _gameClient.ConnectAsync(
                new ConnectionRequest(PlayerName, DefaultHost, DefaultPort), CancellationToken.None);
            ConnectionStatus = $"Đã connect tới server mặc định {DefaultHost}:{DefaultPort}";
            _isConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            return false;
        }
    }

    // Tự connect nếu chưa có kết nối.
    private async Task<bool> EnsureConnectedAsync()
    {
        if (_isConnected) return true;
        return await ConnectAsync();
    }

    public async Task<bool> CreateRoomAsync()
    {
        string? playerNameError = PlayerNameValidator.Validate(PlayerName);

        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }

        try
        {
            if (!await EnsureConnectedAsync()) return false;

            GameViewState state = await _gameClient.CreateRoomAsync(CancellationToken.None);
            ConnectionStatus = state.ConnectionStatus;
            return !string.IsNullOrWhiteSpace(state.RoomId);
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            return false;
        }
    }

    public async Task<bool> JoinRoomAsync()
    {
        string? playerNameError = PlayerNameValidator.Validate(PlayerName);
        string? roomIdError = RoomIdValidator.Validate(RoomId);

        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }

        if (roomIdError != null)
        {
            ConnectionStatus = roomIdError;
            return false;
        }

        try
        {
            if (!await EnsureConnectedAsync()) return false;

            GameViewState state = await _gameClient.JoinRoomAsync(RoomId, CancellationToken.None);
            ConnectionStatus = state.ConnectionStatus;
            return !string.IsNullOrWhiteSpace(state.RoomId);
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            return false;
        }
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
