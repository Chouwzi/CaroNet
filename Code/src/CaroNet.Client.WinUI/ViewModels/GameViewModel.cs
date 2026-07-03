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
    private bool _isGameEnded;
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
            // Kháng lỗi đường truyền mạng
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
        set => SetProperty(ref _connectionStatus, value);
    }

    public string ServerError
    {
        get => _serverError;
        set => SetProperty(ref _serverError, value);
    }

    public bool IsGameEnded
    {
        get => _isGameEnded;
        private set => SetProperty(ref _isGameEnded, value);
    }

    public bool IsMyTurn =>
        !string.IsNullOrWhiteSpace(RoomId) &&
        !string.IsNullOrWhiteSpace(PlayerSymbol) &&
        PlayerSymbol != "?" &&
        CurrentTurnSymbol == PlayerSymbol;

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
            // Bảo vệ ứng dụng nếu mất kết nối khi bấm ô cờ
        }
    }

    // Hàm phụ trợ dùng chung để ép bất kỳ tiến trình nào về luồng UI một cách an toàn
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
        RoomId = state.RoomId;
        PlayerName = state.PlayerName;
        PlayerSymbol = state.PlayerSymbol;
        CurrentTurnSymbol = state.CurrentTurnSymbol;
        ServerError = state.ServerError;

        if (state.ConnectionStatus != null && (state.ConnectionStatus.StartsWith("Trận đấu mới") || state.ConnectionStatus.Contains("Đã vào phòng")))
        {
            ConnectionStatus = state.ConnectionStatus;
            IsGameEnded = false;

            foreach (var cell in BoardCells)
            {
                cell.Mark = string.Empty;
                cell.IsWinningCell = false;
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

            IsGameEnded = (state.ConnectionStatus == "Trò chơi kết thúc" || ServerError == "Ván đấu đã kết thúc.");
        }

        foreach (var cellState in state.Cells)
        {
            int index = cellState.Row * BoardSize + cellState.Column;
            if (index >= 0 && index < BoardCells.Count)
            {
                BoardCells[index].Mark = cellState.Mark;
            }
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
                if (targetCells != null && targetCells.Count > 0)
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

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChatInputEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSendButtonEnabled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMyTurn)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TurnMessage)));
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void GameClient_ChatReceived(object? sender, CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload payload)
    {
        // Khóa chặt việc nạp tin nhắn chat luôn luôn phải chạy trên luồng giao diện chính
        SafeExecuteOnUI(() => AddChatMessageToUI(payload));
    }

    private void AddChatMessageToUI(CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload payload)
    {
        ChatMessages.Add(new ChatMessageViewModel(payload.SenderName, payload.Message, payload.Timestamp));
    }
}

public sealed class BoardCellViewModel : INotifyPropertyChanged
{
    private string _mark = string.Empty;
    private bool _isWinningCell;
    private bool _isInteractionEnabled = true;

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
}
