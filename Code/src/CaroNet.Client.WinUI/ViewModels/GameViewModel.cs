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
    private string _chatInputText = string.Empty;

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public string ChatInputText
    {
        get => _chatInputText;
        set
        {
            SetProperty(ref _chatInputText, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSendButtonEnabled)));
        }
    }

    public bool IsChatInputEnabled => !string.IsNullOrEmpty(RoomId);
    public bool IsSendButtonEnabled => IsChatInputEnabled && !string.IsNullOrWhiteSpace(ChatInputText) && ChatInputText.Length <= 200;

    public GameViewModel(IGameClientService gameClient)
    {
        _gameClient = gameClient;
        _syncContext = SynchronizationContext.Current;

        _gameClient.GameStateUpdated += OnGameStateUpdatedFromServer;
        _gameClient.ChatReceived += GameClient_ChatReceived;

        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                BoardCells.Add(new BoardCellViewModel(row, column));
            }
        }

        ApplyState(_gameClient.CurrentState);
    }

    public async Task SendChatAsync()
    {
        string cleanMessage = ChatInputText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(cleanMessage) || cleanMessage.Length > 200 || string.IsNullOrEmpty(RoomId))
        {
            return;
        }

        try
        {
            await _gameClient.SendChatAsync(cleanMessage);
            ChatInputText = string.Empty;
        }
        catch (Exception)
        {
        }
    }

    public sealed class ChatMessageViewModel
    {
        public string SenderName { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public ChatMessageViewModel(string senderName, string message, DateTime timestamp)
        {
            SenderName = senderName;
            Message = message;
            Timestamp = timestamp;
        }
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
        try
        {
            await _gameClient.MakeMoveAsync(new BoardPosition(row, column), CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }

    private GameViewState MapToGameViewState(GameViewState incomingState)
    {
        var state = new GameViewState
        {
            RoomId = incomingState.RoomId,
            PlayerName = incomingState.PlayerName,
            PlayerSymbol = incomingState.PlayerSymbol,
            CurrentTurnSymbol = incomingState.CurrentTurnSymbol,
            ConnectionStatus = incomingState.ConnectionStatus,
            ServerError = incomingState.ServerError,
            OpponentName = incomingState.OpponentName,
            MyScore = incomingState.MyScore,
            OpponentScore = incomingState.OpponentScore
        };

        state.Cells = new List<CellState>();
        foreach (var cell in incomingState.Cells)
        {
            state.Cells.Add(new CellState
            {
                Row = cell.Row,
                Column = cell.Column,
                Mark = cell.Mark
            });
        }

        return state;
    }

    private void OnGameStateUpdatedFromServer(object? sender, GameViewState state)
    {
        var mappedState = MapToGameViewState(state);
        SafeExecuteOnUI(() => ApplyState(mappedState));
    }

    private void SafeExecuteOnUI(Action action)
    {
        if (_dispatchToUI is not null)
        {
            _dispatchToUI(action);
        }
        else if (_syncContext is not null)
        {
            _syncContext.Post(_ => action(), null);
        }
        else
        {
            action();
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
            int index = cellState.Row * BoardSize + cellState.Column;
            if (index >= 0 && index < BoardCells.Count)
            {
                BoardCells[index].Mark = cellState.Mark;
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChatInputEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSendButtonEnabled)));
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

    private void GameClient_ChatReceived(object? sender, CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload payload)
    {
        SafeExecuteOnUI(() => AddChatMessageToUI(payload));
    }

    private void AddChatMessageToUI(CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload payload)
    {
        ChatMessages.Add(new ChatMessageViewModel(payload.SenderName, payload.Message, payload.Timestamp));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}