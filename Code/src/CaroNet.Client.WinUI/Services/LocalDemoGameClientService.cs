using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Shared.Game;

namespace CaroNet.Client.WinUI.Services;

public sealed class LocalDemoGameClientService : IGameClientService
{
    private const int BoardSize = 15;

    private readonly string[,] _board = new string[BoardSize, BoardSize];
    private string _connectionStatus = "Chưa kết nối server";
    private string _currentTurnSymbol = "X";
    private string _playerName = "Player";
    private string _playerSymbol = "X";
    private string _roomId = string.Empty;
    private GameViewState? _currentState;

    // Đăng ký sự kiện Chat cho class demo
    public event EventHandler<CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload>? ChatReceived;

    // Xử lý gửi chat ảo khi chạy Demo không có mạng
    public async Task SendChatAsync(string message)
    {
        // Giả lập độ trễ mạng 500ms rồi tự động phản hồi lại tin nhắn
        await Task.Delay(500);

        ChatReceived?.Invoke(this, new CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload
        {
            SenderName = "Hệ thống (Demo)",
            Message = $"Bạn vừa nói: {message}",
            Timestamp = DateTime.Now
        });
    }

    public event EventHandler<GameViewState>? GameStateUpdated;

    public GameViewState CurrentState => _currentState ?? BuildState(string.Empty);

    public Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        _playerName = string.IsNullOrWhiteSpace(request.PlayerName) ? "Player" : request.PlayerName.Trim();
        _connectionStatus = $"Đã kết nối demo tới {request.Host}:{request.Port}";
        PublishState(string.Empty);
        return Task.CompletedTask;
    }

    public Task<GameViewState> CreateRoomAsync(CancellationToken cancellationToken)
    {
        _roomId = "ROOM-001";
        _playerSymbol = "X";
        _connectionStatus = $"Đã tạo phòng {_roomId}";
        var state = PublishState(string.Empty);
        return Task.FromResult(state);
    }

    public Task<GameViewState> JoinRoomAsync(string roomId, CancellationToken cancellationToken)
    {
        _roomId = string.IsNullOrWhiteSpace(roomId) ? "ROOM-001" : roomId.Trim();
        _playerSymbol = "O";
        _connectionStatus = $"Đã vào phòng {_roomId}";
        var state = PublishState(string.Empty);
        return Task.FromResult(state);
    }

    public Task MakeMoveAsync(BoardPosition position, CancellationToken cancellationToken)
    {
        if (position.Row < 0 || position.Row >= BoardSize || position.Column < 0 || position.Column >= BoardSize)
        {
            PublishState("MoveRejected: nước đi ngoài bàn cờ.");
            return Task.CompletedTask;
        }

        if (_playerSymbol != _currentTurnSymbol)
        {
            PublishState("MoveRejected: chưa tới lượt của bạn.");
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(_board[position.Row, position.Column]))
        {
            PublishState("MoveRejected: ô này đã được đánh.");
            return Task.CompletedTask;
        }

        _board[position.Row, position.Column] = _playerSymbol;
        _currentTurnSymbol = _currentTurnSymbol == "X" ? "O" : "X";
        PublishState(string.Empty);
        return Task.CompletedTask;
    }

    private GameViewState PublishState(string serverError)
    {
        var state = BuildState(serverError);
        _currentState = state;
        GameStateUpdated?.Invoke(this, state);
        return state;
    }

    private GameViewState BuildState(string serverError)
    {
        var state = new GameViewState(
            _roomId,
            _playerName,
            _playerSymbol,
            _currentTurnSymbol,
            _connectionStatus,
            serverError,
            BuildCells());

        return state;
    }

    private IReadOnlyList<CellViewState> BuildCells()
    {
        var cells = new List<CellViewState>(BoardSize * BoardSize);
        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                cells.Add(new CellViewState(row, column, _board[row, column]));
            }
        }

        return cells;
    }
}
