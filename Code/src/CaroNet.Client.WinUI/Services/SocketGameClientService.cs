using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Services;

public sealed class SocketGameClientService : IGameClientService, IAsyncDisposable
{
    private const int BoardSize = 15;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly IClientConnection _connection;
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _roomRequestLock = new(1, 1);
    private TaskCompletionSource<GameViewState>? _roomJoinedCompletion;
    private string _connectionStatus = "Chưa kết nối server";
    private string _currentTurnSymbol = "X";
    private string _playerId = string.Empty;
    private string _playerName = "Player";
    private string _playerSymbol = "?";
    private string _roomId = string.Empty;
    private string _serverError = string.Empty;
    private string[,] _board = InitEmptyBoard();

    // Định nghĩa sự kiện nhận Chat từ Socket mạng thật gửi về
    public event EventHandler<CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload>? ChatReceived;

    // Hàm gửi tin nhắn qua Socket lên Server mạng thật
    public async Task SendChatAsync(string message)
    {
        var payload = new CaroNet.Shared.Protocol.Payloads.ChatPayload
        {
            Message = message
        };

        var envelope = new MessageEnvelope
        {
            Type = MessageType.Chat,
            RoomId = EmptyToNull(_roomId),
            PlayerId = EmptyToNull(_playerId),
            // Đồng bộ cách đóng gói dạng JsonElement giống như hàm MakeMoveAsync
            Payload = JsonSerializer.SerializeToElement(payload)
        };

        // Truyền kèm CancellationToken do hàm SendAsync của connection yêu cầu
        await _connection.SendAsync(envelope, CancellationToken.None);
    }

    public SocketGameClientService(IClientConnection connection)
    {
        _connection = connection;
        _connection.MessageReceived += Connection_MessageReceived;
        _connection.ConnectionError += Connection_ConnectionError;
        _connection.Disconnected += Connection_Disconnected;
    }

    private static string[,] InitEmptyBoard()
    {
        var board = new string[BoardSize, BoardSize];
        for (int r = 0; r < BoardSize; r++)
            for (int c = 0; c < BoardSize; c++)
                board[r, c] = string.Empty;
        return board;
    }

    public event EventHandler<GameViewState>? GameStateUpdated;

    public GameViewState CurrentState => BuildState();

    public async Task ConnectAsync(
        ConnectionRequest request,
        CancellationToken cancellationToken)
    {
        _playerName = string.IsNullOrWhiteSpace(request.PlayerName)
            ? "Player"
            : request.PlayerName.Trim();

        await _connection.ConnectAsync(
            request.Host,
            request.Port,
            cancellationToken);

        _connectionStatus = $"Đã kết nối server {request.Host}:{request.Port}";
        _serverError = string.Empty;

        await _connection.SendAsync(
            new MessageEnvelope
            {
                Type = MessageType.Hello,
                Payload = JsonSerializer.SerializeToElement(new
                {
                    playerName = _playerName
                })
            },
            cancellationToken);

        PublishState();
    }

    public async Task<GameViewState> CreateRoomAsync(CancellationToken cancellationToken)
    {
        // Thử chiếm chốt chặn ngay lập tức (0ms). Nếu thất bại = đang có request chạy ngầm.
        if (!await _roomRequestLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Hệ thống đang xử lý yêu cầu vào phòng trước đó. Vui lòng đợi.");
        }

        try
        {
            TaskCompletionSource<GameViewState> completion = PrepareRoomJoinWaiter();

            await _connection.SendAsync(
                new MessageEnvelope
                {
                    Type = MessageType.CreateRoom,
                    PlayerId = EmptyToNull(_playerId),
                    Payload = JsonSerializer.SerializeToElement(new { })
                },
                cancellationToken);

            return await WaitForRoomJoinedAsync(completion, cancellationToken);
        }
        finally
        {
            // Luôn luôn mở khóa chốt chặn khi kết thúc (kể cả thành công, thất bại hay timeout)
            _roomRequestLock.Release();
        }
    }

    public async Task<GameViewState> JoinRoomAsync(
        string roomId,
        CancellationToken cancellationToken)
    {
        // Thử chiếm chốt chặn ngay lập tức
        if (!await _roomRequestLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Hệ thống đang xử lý yêu cầu vào phòng trước đó. Vui lòng đợi.");
        }

        try
        {
            TaskCompletionSource<GameViewState> completion = PrepareRoomJoinWaiter();
            string trimmedRoomId = roomId.Trim();

            await _connection.SendAsync(
                new MessageEnvelope
                {
                    Type = MessageType.JoinRoom,
                    RoomId = trimmedRoomId,
                    PlayerId = EmptyToNull(_playerId),
                    Payload = JsonSerializer.SerializeToElement(new
                    {
                        roomId = trimmedRoomId
                    })
                },
                cancellationToken);

            return await WaitForRoomJoinedAsync(completion, cancellationToken);
        }
        finally
        {
            // Luôn luôn mở khóa chốt chặn khi kết thúc
            _roomRequestLock.Release();
        }
    }

    public async Task MakeMoveAsync(
        BoardPosition position,
        CancellationToken cancellationToken)
    {
        await _connection.SendAsync(
            new MessageEnvelope
            {
                Type = MessageType.MakeMove,
                RoomId = EmptyToNull(_roomId),
                PlayerId = EmptyToNull(_playerId),
                Payload = JsonSerializer.SerializeToElement(new
                {
                    row = position.Row,
                    column = position.Column,
                    playerId = _playerId
                })
            },
            cancellationToken);
    }

    private TaskCompletionSource<GameViewState> PrepareRoomJoinWaiter()
    {
        var completion = new TaskCompletionSource<GameViewState>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_stateLock)
        {
            _roomJoinedCompletion = completion;
            _serverError = string.Empty;
        }

        return completion;
    }

    private static async Task<GameViewState> WaitForRoomJoinedAsync(
        TaskCompletionSource<GameViewState> completion,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            return await completion.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Server chưa phản hồi yêu cầu phòng.");
        }
    }

    private void Connection_MessageReceived(
        object? sender,
        ClientMessageReceivedEventArgs args)
    {
        try
        {
            switch (args.Message.Type)
            {
                case MessageType.HelloAccepted:
                    ApplyHelloAccepted(args.Message);
                    break;
                case MessageType.RoomJoined:
                case MessageType.GameStarted:
                    ApplyRoomJoined(args.Message);
                    break;
                case MessageType.GameStateUpdated:
                    ApplyGameStateUpdated(args.Message);
                    break;
                case MessageType.MoveRejected:
                case MessageType.Error:
                    ApplyServerError(args.Message);
                    break;
                case MessageType.GameEnded:
                    ApplyGameEnded(args.Message);
                    break;
                case MessageType.ChatReceived:
                    ApplyChatReceived(args.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            UpdateError($"Không đọc được message từ server: {ex.Message}");
        }
    }

    private void ApplyHelloAccepted(MessageEnvelope message)
    {
        lock (_stateLock)
        {
            _playerId = FirstNonEmpty(
                GetString(message.Payload, "playerId"),
                message.PlayerId,
                _playerId);
            _connectionStatus = "Server đã chấp nhận kết nối.";
            _serverError = string.Empty;
        }

        PublishState();
    }

    private void ApplyRoomJoined(MessageEnvelope message)
    {
        lock (_stateLock)
        {
            _roomId = FirstNonEmpty(
                GetString(message.Payload, "roomId"),
                message.RoomId,
                _roomId);
            _playerId = FirstNonEmpty(
                GetString(message.Payload, "playerId"),
                message.PlayerId,
                _playerId);
            _playerSymbol = FirstNonEmpty(
                GetString(message.Payload, "playerSymbol"),
                GetString(message.Payload, "yourSymbol"),
                GetString(message.Payload, "symbol"),
                _playerSymbol);
            _connectionStatus = string.IsNullOrWhiteSpace(_roomId)
                ? "Đã vào phòng"
                : $"Đã vào phòng {_roomId}";
            _serverError = string.Empty;

            // GameStarted gửi kèm board và lượt đi
            if (TryReadBoard(message.Payload, out string[,]? board))
            {
                _board = board!;
            }
            _currentTurnSymbol = ResolveCurrentTurnSymbol(message.Payload);
        }

        GameViewState state = PublishState();
        _roomJoinedCompletion?.TrySetResult(state);
    }

    private void ApplyGameStateUpdated(MessageEnvelope message)
    {
        lock (_stateLock)
        {
            // Board server gửi là nguồn dữ liệu chính; client chỉ render lại snapshot này.
            if (TryReadBoard(message.Payload, out string[,]? board))
            {
                _board = board!;
            }

            _currentTurnSymbol = ResolveCurrentTurnSymbol(message.Payload);
            _serverError = string.Empty;
        }

        PublishState();
    }

    private void ApplyServerError(MessageEnvelope message)
    {
        lock (_stateLock)
        {
            _serverError = FirstNonEmpty(
                GetString(message.Payload, "message"),
                GetString(message.Payload, "error"),
                GetString(message.Payload, "reason"),
                "Server từ chối yêu cầu.");
        }

        PublishState();
    }

    private void ApplyGameEnded(MessageEnvelope message)
    {
        lock (_stateLock)
        {
            if (TryReadBoard(message.Payload, out string[,]? board))
            {
                _board = board!;
            }

            string reason = GetString(message.Payload, "reason");



if (reason == "opponent_disconnected")
{
    _serverError = "Đối thủ đã ngắt kết nối. Bạn thắng!";
}
else
{
    _serverError = FirstNonEmpty(
        GetString(message.Payload, "message"),
        "Ván đấu đã kết thúc.");
}
        }

        PublishState();
    }

    private void Connection_ConnectionError(object? sender, Exception exception)
    {
        UpdateError($"Lỗi kết nối: {exception.Message}");
    }

    private void Connection_Disconnected(object? sender, EventArgs args)
    {
        lock (_stateLock)
        {
            _connectionStatus = "Mất kết nối server";
        }

        PublishState();
    }

    private void UpdateConnectionStatus(string status)
    {
        lock (_stateLock)
        {
            _connectionStatus = status;
            _serverError = string.Empty;
        }

        PublishState();
    }

    private void UpdateError(string error)
    {
        lock (_stateLock)
        {
            _serverError = error;
        }

        PublishState();
    }

    private GameViewState PublishState()
    {
        GameViewState state = BuildState();
        GameStateUpdated?.Invoke(this, state);
        return state;
    }

    private GameViewState BuildState()
    {
        lock (_stateLock)
        {
            return new GameViewState(
                _roomId,
                _playerName,
                _playerSymbol,
                _currentTurnSymbol,
                _connectionStatus,
                _serverError,
                BuildCells());
        }
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

    private string ResolveCurrentTurnSymbol(JsonElement? payload)
    {
        string explicitSymbol = GetString(payload, "currentTurnSymbol");
        if (!string.IsNullOrWhiteSpace(explicitSymbol))
        {
            return explicitSymbol;
        }

        string currentTurnPlayerId = GetString(payload, "currentTurnPlayerId");
        if (string.IsNullOrWhiteSpace(currentTurnPlayerId))
        {
            return _currentTurnSymbol;
        }

        return currentTurnPlayerId == _playerId
            ? _playerSymbol
            : GetOpponentSymbol(_playerSymbol);
    }

    private static bool TryReadBoard(
        JsonElement? payload,
        out string[,]? board)
    {
        board = null;

        if (!TryGetProperty(payload, "board", out JsonElement boardElement) ||
            boardElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsedBoard = new string[BoardSize, BoardSize];
        var rowIndex = 0;

        foreach (JsonElement rowElement in boardElement.EnumerateArray().Take(BoardSize))
        {
            if (rowElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var columnIndex = 0;
            foreach (JsonElement cellElement in rowElement.EnumerateArray().Take(BoardSize))
            {
                parsedBoard[rowIndex, columnIndex] = cellElement.ValueKind == JsonValueKind.String
                    ? cellElement.GetString() ?? string.Empty
                    : string.Empty;
                columnIndex++;
            }

            rowIndex++;
        }

        board = parsedBoard;
        return true;
    }

    private static string GetString(JsonElement? payload, string propertyName)
    {
        if (!TryGetProperty(payload, propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static bool TryGetProperty(
        JsonElement? payload,
        string propertyName,
        out JsonElement property)
    {
        property = default;

        if (payload is not { ValueKind: JsonValueKind.Object } objectPayload)
        {
            return false;
        }

        // Server có thể serialize bằng camelCase hoặc PascalCase tùy layer.
        foreach (JsonProperty jsonProperty in objectPayload.EnumerateObject())
        {
            if (string.Equals(
                    jsonProperty.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                property = jsonProperty.Value;
                return true;
            }
        }

        return false;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string GetOpponentSymbol(string playerSymbol)
    {
        return playerSymbol == "X" ? "O" : "X";
    }

    private static string? EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public async ValueTask DisposeAsync()
    {
        // === THÊM DÒNG NÀY VÀO ĐÂY ===
        _roomRequestLock.Dispose();

        await _connection.DisposeAsync();
    }
    private void ApplyChatReceived(MessageEnvelope message)
    {
        if (message.Payload.HasValue)
        {
            try
            {
                var chatReceivedPayload = message.Payload.Value.Deserialize<CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload>();
                if (chatReceivedPayload != null)
                {
                    // Kích hoạt Event để GameViewModel bên ngoài nghe thấy và render lên giao diện
                    ChatReceived?.Invoke(this, chatReceivedPayload);
                }
            }
            catch (Exception ex)
            {
                UpdateError($"Không thể giải mã tin nhắn chat: {ex.Message}");
            }
        }
    }
}
