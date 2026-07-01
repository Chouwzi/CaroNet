using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Models;
using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.Validation;
using System.Linq;
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

    // ================= BEST RECORD =================

    public ObservableCollection<BestRecordItem> BestRecords { get; }
        = new();

    public async Task LoadBestRecordsAsync()
    {
        BestRecords.Clear();

        try
        {
            var matches =
                await AppServices.MatchHistoryStore.GetAllMatchesAsync();

            var ranking = matches
                .Where(match => !string.IsNullOrWhiteSpace(match.WinnerName))
                .GroupBy(match => match.WinnerName!)
                .Select(group => new BestRecordItem
                {
                    PlayerName = group.Key,
                    Wins = group.Count()
                })
                .OrderByDescending(item => item.Wins)
                .ThenBy(item => item.PlayerName)
                .Take(10)
                .ToList();

            int rank = 1;

            foreach (var item in ranking)
            {
                item.Rank = rank++;
                BestRecords.Add(item);
            }
        }
        catch
        {
            
        }
    }

    

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    
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
        string? playerNameError = PlayerNameValidator.Validate(PlayerName);

        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }

        // Lưu thông tin người chơi
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values["PlayerName"] = PlayerName;
            localSettings.Values["ServerHost"] = ServerHost;
            localSettings.Values["ServerPort"] = ServerPort;
        }
        catch (InvalidOperationException)
        {
            try
            {
                string appDataFolder =
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        "CaroNet");

                System.IO.Directory.CreateDirectory(appDataFolder);

                string filePath =
                    System.IO.Path.Combine(
                        appDataFolder,
                        "settings.txt");

                System.IO.File.WriteAllLines(
                    filePath,
                    new[]
                    {
                    PlayerName,
                    ServerHost,
                    ServerPort.ToString()
                    });
            }
            catch
            {
            }
        }

        try
        {
            await _gameClient.ConnectAsync(
                new ConnectionRequest(
                    PlayerName,
                    ServerHost,
                    ServerPort),
                CancellationToken.None);

            ConnectionStatus =
                $"Đã connect tới server {ServerHost}:{ServerPort}";

            _isConnected = true;

            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            return false;
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (_isConnected)
        {
            return true;
        }

        return await ConnectAsync();
    }

    public async Task<bool> CreateRoomAsync()
    {
        string? playerNameError =
            PlayerNameValidator.Validate(PlayerName);

        if (playerNameError != null)
        {
            ConnectionStatus = playerNameError;
            return false;
        }

        try
        {
            if (!await EnsureConnectedAsync())
            {
                return false;
            }

            GameViewState state =
                await _gameClient.CreateRoomAsync(
                    CancellationToken.None);

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
        string? playerNameError =
            PlayerNameValidator.Validate(PlayerName);

        string? roomIdError =
            RoomIdValidator.Validate(RoomId);

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
            if (!await EnsureConnectedAsync())
            {
                return false;
            }

            GameViewState state =
                await _gameClient.JoinRoomAsync(
                    RoomId,
                    CancellationToken.None);

            ConnectionStatus = state.ConnectionStatus;

            return !string.IsNullOrWhiteSpace(state.RoomId);
        }
        catch (Exception ex)
        {
            ConnectionStatus = ex.Message;
            return false;
        }
    }

    private void SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}