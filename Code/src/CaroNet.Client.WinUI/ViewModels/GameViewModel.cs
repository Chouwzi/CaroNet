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
    private string _opponentName = "Đối thủ";
    private string _playerName = "Player";
    private string _playerSymbol = "?";
    private string _roomId = string.Empty;
    private string _serverError = string.Empty;
    private bool _isGameEnded;
    private string _chatInputText = string.Empty;
    private int _myScore;
    private int _opponentScore;
    private BoardPosition? _lastMovePosition;

    public GameViewModel(IGameClientService gameClient)
    {
        _gameClient = gameClient;
        _syncContext = SynchronizationContext.Current;

        _gameClient.GameStateUpdated += GameClient_GameStateUpdated;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BoardCellViewModel> BoardCells { get; } = [];

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public string ChatInputText
    {
        get => _chatInputText;
        set
        {
            SetProperty(ref _chatInputText, value);
            OnPropertyChanged(nameof(IsSendButtonEnabled));
        }
    }

    public bool IsChatInputEnabled => !string.IsNullOrEmpty(RoomId);

    public bool IsSendButtonEnabled =>
        IsChatInputEnabled &&
        !string.IsNullOrWhiteSpace(ChatInputText) &&
        ChatInputText.Length <= 200;

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

    public string OpponentName
    {
        get => _opponentName;
        private set => SetProperty(ref _opponentName, value);
    }

    public string PlayerSymbol
    {
        get => _playerSymbol;
        private set
        {
            if (SetProperty(ref _playerSymbol, value))
            {
                OnPropertyChanged(nameof(OpponentSymbol));
            }
        }
    }

    public string OpponentSymbol => PlayerSymbol switch
    {
        "X" => "O",
        "O" => "X",
        _ => "?"
    };

    public string CurrentTurnSymbol
    {
        get => _currentTurnSymbol;
        private set => SetProperty(ref _currentTurnSymbol, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public string ServerError
    {
        get => _serverError;
        set => SetProperty(ref _serverError, value);
    }

    public int MyScore
    {
        get => _myScore;
        private set
        {
            if (SetProperty(ref _myScore, value))
            {
                OnPropertyChanged(nameof(ScoreText));
            }
        }
    }

    public int OpponentScore
    {
        get => _opponentScore;
        private set
        {
            if (SetProperty(ref _opponentScore, value))
            {
                OnPropertyChanged(nameof(ScoreText));
            }
        }
    }

    public string ScoreText => $"{MyScore} - {OpponentScore}";

    public bool IsGameEnded
    {
        get => _isGameEnded;
        private set => SetProperty(ref _isGameEnded, value);
    }

    public BoardPosition? LastMovePosition
    {
        get => _lastMovePosition;
        private set => SetProperty(ref _lastMovePosition, value);
    }

    public bool IsMyTurn =>
        !string.IsNullOrWhiteSpace(RoomId) &&
        !string.IsNullOrWhiteSpace(PlayerSymbol) &&
        PlayerSymbol != "?" &&
        CurrentTurnSymbol == PlayerSymbol;

    public bool IsOpponentTurn =>
        !string.IsNullOrWhiteSpace(RoomId) &&
        !string.IsNullOrWhiteSpace(OpponentSymbol) &&
        OpponentSymbol != "?" &&
        CurrentTurnSymbol == OpponentSymbol;

    public double MyHeaderOpacity => IsMyTurn ? 1.0 : 0.72;

    public double OpponentHeaderOpacity => IsOpponentTurn ? 1.0 : 0.72;

    public string MyTurnLabel => IsMyTurn ? "ĐẾN LƯỢT" : string.Empty;

    public string OpponentTurnLabel => IsOpponentTurn ? "ĐẾN LƯỢT" : string.Empty;

    public string TurnMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RoomId) ||
                string.IsNullOrWhiteSpace(PlayerSymbol) ||
                PlayerSymbol == "?")
            {
                return "Đang chờ đối thủ...";
            }

            return IsMyTurn
                ? "🎯 Lượt của bạn!"
                : "⏳ Đợi đối thủ...";
        }
    }

    public void SetDispatcher(Action<Action> dispatcher)
    {
        _dispatchToUI = dispatcher;
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
            // Bỏ qua lỗi mạng ngắn hạn khi gửi chat.
        }
    }

    public async Task MakeMoveAsync(int row, int column)
    {
        if (IsGameEnded)
        {
            return;
        }

        try
        {
            await _gameClient.MakeMoveAsync(new BoardPosition(row, column), CancellationToken.None);
        }
        catch (Exception)
        {
            // Bảo vệ UI nếu kết nối mất đúng lúc bấm ô.
        }
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

    private void GameClient_GameStateUpdated(object? sender, GameViewState state)
    {
        SafeExecuteOnUI(() => ApplyState(state));
    }

    private void ApplyState(GameViewState state)
    {
        BoardPosition? detectedLastMove = null;
        var changedMoveCount = 0;

        foreach (var cellState in state.Cells)
        {
            int index = cellState.Row * BoardSize + cellState.Column;
            if (index >= 0 &&
                index < BoardCells.Count &&
                BoardCells[index].Mark != cellState.Mark &&
                !string.IsNullOrEmpty(cellState.Mark))
            {
                detectedLastMove = new BoardPosition(cellState.Row, cellState.Column);
                changedMoveCount++;
            }
        }

        RoomId = state.RoomId;
        PlayerName = state.PlayerName;
        OpponentName = string.IsNullOrWhiteSpace(state.OpponentName) ? "Đối thủ" : state.OpponentName;
        PlayerSymbol = state.PlayerSymbol;
        CurrentTurnSymbol = state.CurrentTurnSymbol;
        ServerError = state.ServerError;
        MyScore = state.MyScore;
        OpponentScore = state.OpponentScore;

        if (state.ConnectionStatus != null &&
            (state.ConnectionStatus.StartsWith("Trận đấu mới") || state.ConnectionStatus.Contains("Đã vào phòng")))
        {
            ConnectionStatus = state.ConnectionStatus;
            IsGameEnded = false;

            foreach (var cell in BoardCells)
            {
                cell.Mark = string.Empty;
                cell.IsWinningCell = false;
                cell.IsLastMove = false;
                cell.IsInteractionEnabled = true;
            }
        }
        else
        {
            if (_connectionStatus == "Đang chờ đối thủ xác nhận...")
            {
                if (!string.IsNullOrEmpty(state.ConnectionStatus) &&
                    (state.ConnectionStatus.StartsWith("Trận đấu mới") || state.ConnectionStatus == "Đối thủ muốn chơi lại!"))
                {
                    ConnectionStatus = state.ConnectionStatus;
                }
            }
            else
            {
                ConnectionStatus = state.ConnectionStatus ?? ConnectionStatus;
            }

            IsGameEnded = state.ConnectionStatus == "Trò chơi kết thúc" || ServerError == "Ván đấu đã kết thúc.";
        }

        foreach (var cellState in state.Cells)
        {
            int index = cellState.Row * BoardSize + cellState.Column;
            if (index >= 0 && index < BoardCells.Count)
            {
                BoardCells[index].Mark = cellState.Mark;
            }
        }

        LastMovePosition = changedMoveCount == 1 ? detectedLastMove : null;
        foreach (var cell in BoardCells)
        {
            cell.IsLastMove =
                LastMovePosition is { } lastMove &&
                cell.Row == lastMove.Row &&
                cell.Column == lastMove.Column;
        }

        if (IsGameEnded || _connectionStatus == "Đang chờ đối thủ xác nhận...")
        {
            foreach (var cell in BoardCells)
            {
                cell.IsInteractionEnabled = false;
            }

            if (_gameClient is SocketGameClientService socketService)
            {
                var targetCells = socketService.WinningCells;
                if (targetCells.Count > 0)
                {
                    foreach (var target in targetCells)
                    {
                        int index = target.Row * BoardSize + target.Col;
                        if (index >= 0 && index < BoardCells.Count)
                        {
                            BoardCells[index].IsWinningCell = true;
                        }
                    }
                }
            }
        }

        OnPropertyChanged(nameof(IsChatInputEnabled));
        OnPropertyChanged(nameof(IsSendButtonEnabled));
        OnPropertyChanged(nameof(IsMyTurn));
        OnPropertyChanged(nameof(IsOpponentTurn));
        OnPropertyChanged(nameof(MyHeaderOpacity));
        OnPropertyChanged(nameof(OpponentHeaderOpacity));
        OnPropertyChanged(nameof(MyTurnLabel));
        OnPropertyChanged(nameof(OpponentTurnLabel));
        OnPropertyChanged(nameof(TurnMessage));
        OnPropertyChanged(nameof(LastMovePosition));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
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

    public sealed class ChatMessageViewModel
    {
        public ChatMessageViewModel(string senderName, string message, DateTime timestamp)
        {
            SenderName = senderName;
            Message = message;
            Timestamp = timestamp;
        }

        public string SenderName { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
    }
}

public sealed class BoardCellViewModel : INotifyPropertyChanged
{
    private string _mark = string.Empty;
    private bool _isWinningCell;
    private bool _isInteractionEnabled = true;
    private bool _isLastMove;

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
            if (_mark == value) return;
            _mark = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mark)));
        }
    }

    public bool IsWinningCell
    {
        get => _isWinningCell;
        set
        {
            if (_isWinningCell == value) return;
            _isWinningCell = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWinningCell)));
        }
    }

    public bool IsInteractionEnabled
    {
        get => _isInteractionEnabled;
        set
        {
            if (_isInteractionEnabled == value) return;
            _isInteractionEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInteractionEnabled)));
        }
    }

    public bool IsLastMove
    {
        get => _isLastMove;
        set
        {
            if (_isLastMove == value) return;
            _isLastMove = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLastMove)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMoveIndicatorOpacity)));
        }
    }

    public double LastMoveIndicatorOpacity => IsLastMove ? 1.0 : 0.0;
}
