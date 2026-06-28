using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Client.WinUI.Services;
using CaroNet.Shared.Game;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class GameViewModel : INotifyPropertyChanged
{
    public const int BoardSize = 15;

    private readonly IGameClientService _gameClient;
    private readonly SynchronizationContext? _syncContext;
    private Action<Action>? _dispatchToUI;

    private string _connectionStatus = "Chưa kết nối server";
    private string _currentTurnSymbol = "X";
    private string _playerName = "Player";
    private string _playerSymbol = "?";
    private string _roomId = string.Empty;
    private string _serverError = string.Empty;

    public GameViewModel(IGameClientService gameClient)
    {
        _gameClient = gameClient;
        _syncContext = SynchronizationContext.Current;
        _gameClient.GameStateUpdated += GameClient_GameStateUpdated;

        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                BoardCells.Add(new BoardCellViewModel(row, column));
            }
        }

        ApplyState(_gameClient.CurrentState);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BoardCellViewModel> BoardCells { get; } = [];

    public void SetDispatcher(Action<Action> dispatcher)
    {
        _dispatchToUI = dispatcher;
    }

    public string RoomId
    {
        get => _roomId;
        private set => SetProperty(ref _roomId, value);
    }

    public string PlayerName
    {
        get => _playerName;
        private set => SetProperty(ref _playerName, value);
    }

    public string PlayerSymbol
    {
        get => _playerSymbol;
        private set => SetProperty(ref _playerSymbol, value);
    }

    public string CurrentTurnSymbol
    {
        get => _currentTurnSymbol;
        private set => SetProperty(ref _currentTurnSymbol, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public string ServerError
    {
        get => _serverError;
        private set => SetProperty(ref _serverError, value);
    }

    public bool IsMyTurn => CurrentTurnSymbol == PlayerSymbol && !string.IsNullOrEmpty(PlayerSymbol);

    public string TurnMessage
    {
        get
        {
            if (string.IsNullOrEmpty(PlayerSymbol) || string.IsNullOrEmpty(RoomId))
                return "Đang chờ đối thủ...";

            return IsMyTurn ? "🎯 Lượt của bạn!" : "⏳ Đợi đối thủ...";
        }
    }

    public async Task MakeMoveAsync(int row, int column)
    {
        await _gameClient.MakeMoveAsync(new BoardPosition(row, column), CancellationToken.None);
    }

    private void GameClient_GameStateUpdated(object? sender, GameViewState state)
    {
        if (_dispatchToUI is not null)
        {
            _dispatchToUI(() => ApplyState(state));
        }
        else if (_syncContext is not null)
        {
            _syncContext.Post(_ => ApplyState(state), null);
        }
        else
        {
            ApplyState(state);
        }
    }

    private void ApplyState(GameViewState state)
    {
        RoomId = state.RoomId;
        PlayerName = state.PlayerName;
        PlayerSymbol = state.PlayerSymbol;
        CurrentTurnSymbol = state.CurrentTurnSymbol;
        ConnectionStatus = state.ConnectionStatus;
        ServerError = state.ServerError;

        foreach (var cellState in state.Cells)
        {
            BoardCells[cellState.Row * BoardSize + cellState.Column].Mark = cellState.Mark;
        }

        OnPropertyChanged(nameof(IsMyTurn));
        OnPropertyChanged(nameof(TurnMessage));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class BoardCellViewModel : INotifyPropertyChanged
{
    private string _mark = string.Empty;

    public BoardCellViewModel(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Row { get; }
    public int Column { get; }

    public string Mark
    {
        get => _mark;
        set
        {
            if (_mark == value)
            {
                return;
            }

            _mark = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mark)));
        }
    }
}
