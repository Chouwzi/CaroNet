using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.Validation;
using Windows.Storage;

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

    // Khai báo cho ServerHost
    private string _serverHost = "127.0.0.1";
    public string ServerHost
    {
        get => _serverHost;
        set => SetProperty(ref _serverHost, value);
    }

    // Khai báo cho ServerPort
    private int _serverPort = 5000;
    public int ServerPort
    {
        get => _serverPort;
        set => SetProperty(ref _serverPort, value);
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
        // 1. Kiểm tra tên hợp lệ
        string? playerNameError = PlayerNameValidator.Validate(PlayerName);
        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }

        // 2. LƯU THÔNG TIN NGAY KHI BẤM (Dù kết nối lỗi vẫn lưu để lần sau không phải nhập lại)
        try
        {
            // Thử lưu bằng LocalSettings đúng yêu cầu Issue
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["PlayerName"] = PlayerName;
            localSettings.Values["ServerHost"] = ServerHost;
            localSettings.Values["ServerPort"] = ServerPort;
        }
        catch (InvalidOperationException)
        {
            // Fallback: Nếu chạy Unpackaged thì lưu tạm vào file text ở AppData để cứu app
            try
            {
                string appDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroNet");
                System.IO.Directory.CreateDirectory(appDataFolder);
                string filePath = System.IO.Path.Combine(appDataFolder, "settings.txt");
                System.IO.File.WriteAllLines(filePath, new string[] { PlayerName, ServerHost, ServerPort.ToString() });
            }
            catch { }
        }

        // 3. Tiến hành kết nối mạng
        try
        {
            await _gameClient.ConnectAsync(
                new ConnectionRequest(PlayerName, ServerHost, ServerPort), CancellationToken.None);

            ConnectionStatus = $"Đã connect tới server {ServerHost}:{ServerPort}";
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
