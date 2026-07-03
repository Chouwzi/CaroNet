using System.Text.Json;
using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Tests;

public sealed class SocketGameClientServiceTests
{
    [Fact]
    public async Task CreateRoomAsync_sends_request_and_returns_room_joined_state()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        Task<GameViewState> createRoomTask = service.CreateRoomAsync(
            new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);

        Assert.Equal(MessageType.CreateRoom, connection.SentMessages.Last().Type);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                playerId = "player-x",
                playerSymbol = "X"
            })
        });

        GameViewState state = await createRoomTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("ROOM-01", state.RoomId);
        Assert.Equal("Alice", state.PlayerName);
        Assert.Equal("X", state.PlayerSymbol);
        Assert.Equal("Đã vào phòng ROOM-01", state.ConnectionStatus);
        Assert.False(state.HasOpponent);
    }

    [Fact]
    public async Task GameStarted_MarksOpponentAsPresent()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                playerId = "player-x",
                playerSymbol = "X"
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStarted,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                yourSymbol = "X",
                opponentName = "Bob",
                currentTurnPlayerId = "player-x",
                board = CreateEmptyBoard()
            })
        });

        Assert.True(states.Last().HasOpponent);
        Assert.Equal("Bob", states.Last().OpponentName);
    }

    [Fact]
    public async Task CreateRoomAsync_CompletesWithServerError_WhenRoomRequestIsRejected()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        Task<GameViewState> createRoomTask = service.CreateRoomAsync(
            new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.Error,
            Payload = JsonSerializer.SerializeToElement(new
            {
                message = "Rate limit exceeded."
            })
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => createRoomTask);

        Assert.Equal("Rate limit exceeded.", exception.Message);
        Assert.Equal("Rate limit exceeded.", service.CurrentState.ServerError);
    }

    [Fact]
    public async Task CreateRoomAsync_CompletesImmediately_WhenConnectionFailsWhileWaiting()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        Task<GameViewState> createRoomTask = service.CreateRoomAsync(
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

        connection.RaiseConnectionError(new InvalidOperationException("Socket closed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => createRoomTask);

        Assert.Contains("Socket closed", exception.Message);
        Assert.Contains("Socket closed", service.CurrentState.ServerError);
    }

    [Fact]
    public async Task CreateRoomAsync_CompletesImmediately_WhenDisconnectedWhileWaiting()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        Task<GameViewState> createRoomTask = service.CreateRoomAsync(
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

        await connection.DisconnectAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => createRoomTask);

        Assert.Equal("Mất kết nối server", exception.Message);
        Assert.Equal("Mất kết nối server", service.CurrentState.ConnectionStatus);
    }

    [Fact]
    public async Task GameStateUpdated_updates_board_from_server_state()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var receivedState = new TaskCompletionSource<GameViewState>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        service.GameStateUpdated += (_, state) =>
        {
            if (state.Cells.Any(cell => cell.Row == 7 && cell.Column == 8 && cell.Mark == "X"))
            {
                receivedState.TrySetResult(state);
            }
        };

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                playerId = "player-x",
                playerSymbol = "X"
            })
        });

        string[][] board = CreateEmptyBoard();
        board[7][8] = "X";

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStateUpdated,
            Payload = JsonSerializer.SerializeToElement(new
            {
                currentTurnPlayerId = "player-o",
                board
            })
        });

        GameViewState state = await receivedState.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("X", state.Cells.Single(cell => cell.Row == 7 && cell.Column == 8).Mark);
        Assert.Equal("O", state.CurrentTurnSymbol);
        Assert.Empty(state.ServerError);
    }

    [Fact]
    public async Task RematchAccepted_ClearsEndedStateErrorAndWinningCells()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStarted,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                yourSymbol = "X",
                opponentName = "Bob",
                currentTurnPlayerId = "player-x",
                board = CreateEmptyBoard()
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            Payload = JsonSerializer.SerializeToElement(new
            {
                winnerPlayerId = "player-x",
                message = "Ván đấu đã kết thúc.",
                board = CreateEmptyBoard(),
                winningCells = new[]
                {
                    new { row = 0, column = 0 }
                }
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RematchAccepted,
            PlayerId = "player-o",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                yourSymbol = "O",
                opponentName = "Bob",
                currentTurnPlayerId = "player-o",
                board = CreateEmptyBoard()
            })
        });

        GameViewState rematchState = states.Last();

        Assert.Equal("Trận đấu mới đã bắt đầu!", rematchState.ConnectionStatus);
        Assert.Empty(rematchState.ServerError);
        Assert.True(rematchState.HasOpponent);
        Assert.Empty(service.WinningCells);
    }

    [Fact]
    public async Task GameStarted_PublishesOpponentName_AndGameEndedUpdatesScore()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStarted,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                yourSymbol = "X",
                opponentName = "Bob",
                currentTurnPlayerId = "player-x",
                board = CreateEmptyBoard()
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            Payload = JsonSerializer.SerializeToElement(new
            {
                winnerPlayerId = "player-x",
                board = CreateEmptyBoard()
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            Payload = JsonSerializer.SerializeToElement(new
            {
                winnerPlayerId = "player-o",
                board = CreateEmptyBoard()
            })
        });

        GameViewState state = states.Last();

        Assert.Equal("Bob", state.OpponentName);
        Assert.Equal(1, state.MyScore);
        Assert.Equal(1, state.OpponentScore);
    }

    [Fact]
    public async Task MakeMoveAsync_sends_request_without_mutating_board_locally()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var updateCount = 0;

        service.GameStateUpdated += (_, _) => updateCount++;

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                playerId = "player-x",
                playerSymbol = "X"
            })
        });
        updateCount = 0;

        await service.MakeMoveAsync(
            new BoardPosition(3, 4),
            CancellationToken.None);

        MessageEnvelope sentMove = connection.SentMessages.Last();

        Assert.Equal(MessageType.MakeMove, sentMove.Type);
        Assert.Equal("ROOM-01", sentMove.RoomId);
        Assert.Equal("player-x", sentMove.PlayerId);
        Assert.Equal(0, updateCount);

        JsonElement payload = sentMove.Payload!.Value;
        Assert.Equal(3, payload.GetProperty("row").GetInt32());
        Assert.Equal(4, payload.GetProperty("column").GetInt32());
    }

    [Fact]
    public async Task MatchActionMethods_SendExpectedMessageTypes()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStarted,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                yourSymbol = "X",
                opponentName = "Bob",
                currentTurnPlayerId = "player-x",
                board = CreateEmptyBoard()
            })
        });

        await service.SendResignAsync(CancellationToken.None);
        await service.SendDrawOfferAsync(CancellationToken.None);
        await service.SendDrawResponseAsync(true, CancellationToken.None);

        var lastThree = connection.SentMessages.TakeLast(3).ToArray();

        Assert.Equal(MessageType.Resign, lastThree[0].Type);
        Assert.Equal(MessageType.DrawOffer, lastThree[1].Type);
        Assert.Equal(MessageType.DrawResponse, lastThree[2].Type);
        Assert.True(lastThree[2].Payload!.Value.GetProperty("accepted").GetBoolean());
    }

    [Fact]
    public async Task LeaveRoomAsync_SendsLeaveRoomAndClearsLocalRoomState()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.RoomJoined,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                roomId = "ROOM-01",
                playerId = "player-x",
                playerSymbol = "X"
            })
        });

        await service.LeaveRoomAsync(CancellationToken.None);

        MessageEnvelope leaveRoom = connection.SentMessages.Last();
        Assert.Equal(MessageType.LeaveRoom, leaveRoom.Type);
        Assert.Equal("ROOM-01", leaveRoom.RoomId);
        Assert.Empty(service.CurrentState.RoomId);
        Assert.Equal("?", service.CurrentState.PlayerSymbol);
        Assert.False(service.CurrentState.HasOpponent);
        Assert.Equal("Đã rời phòng.", service.CurrentState.ConnectionStatus);
    }

    [Fact]
    public async Task MoveRejected_publishes_error_without_changing_existing_board()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        await service.ConnectAsync(
            new ConnectionRequest("Alice", "127.0.0.1", 5000),
            CancellationToken.None);

        string[][] board = CreateEmptyBoard();
        board[2][2] = "X";

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameStateUpdated,
            Payload = JsonSerializer.SerializeToElement(new
            {
                currentTurnPlayerId = "player-o",
                board
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.MoveRejected,
            Payload = JsonSerializer.SerializeToElement(new
            {
                message = "Chưa tới lượt của bạn."
            })
        });

        GameViewState rejectedState = states.Last();

        Assert.Equal("X", rejectedState.Cells.Single(cell => cell.Row == 2 && cell.Column == 2).Mark);
        Assert.Equal("Chưa tới lượt của bạn.", rejectedState.ServerError);
    }

    [Fact]
    public void GameEnded_WithOpponentDisconnectedReason_PublishesWinMessage()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();
        string[][] board = CreateEmptyBoard();
        board[4][5] = "O";

        service.GameStateUpdated += (_, state) => states.Add(state);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            Payload = JsonSerializer.SerializeToElement(new
            {
                reason = "opponent_disconnected",
                winnerPlayerId = "player-x",
                board
            })
        });

        GameViewState endedState = states.Last();

        Assert.Equal("Đối thủ đã ngắt kết nối. Bạn thắng!", endedState.ServerError);
        Assert.Equal("O", endedState.Cells.Single(cell => cell.Row == 4 && cell.Column == 5).Mark);
    }

    [Fact]
    public void GameEnded_WithTimeoutReason_PublishesTimeoutMessage()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.HelloAccepted,
            PlayerId = "player-x",
            Payload = JsonSerializer.SerializeToElement(new
            {
                playerId = "player-x",
                playerName = "Alice"
            })
        });

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.GameEnded,
            Payload = JsonSerializer.SerializeToElement(new
            {
                reason = "timeout",
                winnerPlayerId = "player-o",
                board = CreateEmptyBoard()
            })
        });

        Assert.Equal("Bạn hết thời gian. Bạn thua.", states.Last().ServerError);
    }

    [Fact]
    public async Task DisconnectAsync_PublishesDisconnectedStatus()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var states = new List<GameViewState>();

        service.GameStateUpdated += (_, state) => states.Add(state);

        await connection.DisconnectAsync();

        Assert.Equal("Mất kết nối server", states.Last().ConnectionStatus);
    }

    [Fact]
    public void ChatReceived_PublishesSenderAndMessage()
    {
        var connection = new FakeClientConnection();
        var service = new SocketGameClientService(connection);
        var receivedMessages = new List<CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload>();

        service.ChatReceived += (_, payload) => receivedMessages.Add(payload);

        connection.RaiseMessage(new MessageEnvelope
        {
            Type = MessageType.ChatReceived,
            Payload = JsonSerializer.SerializeToElement(new
            {
                senderName = "Hệ thống",
                message = "Đối thủ muốn chơi lại!",
                timestamp = DateTime.UtcNow
            })
        });

        var message = Assert.Single(receivedMessages);
        Assert.Equal("Hệ thống", message.SenderName);
        Assert.Equal("Đối thủ muốn chơi lại!", message.Message);
    }

    private static string[][] CreateEmptyBoard()
    {
        return Enumerable.Range(0, GameViewModel.BoardSize)
            .Select(_ => Enumerable.Repeat(string.Empty, GameViewModel.BoardSize).ToArray())
            .ToArray();
    }

    private sealed class FakeClientConnection : IClientConnection
    {
        public List<MessageEnvelope> SentMessages { get; } = [];

        public bool IsConnected { get; private set; }

        public event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;

        public event EventHandler<Exception>? ConnectionError
        {
            add => _connectionError += value;
            remove => _connectionError -= value;
        }

        private event EventHandler<Exception>? _connectionError;

        public event EventHandler? Disconnected;

        public Task ConnectAsync(
            string host,
            int port,
            CancellationToken cancellationToken)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task SendAsync(
            MessageEnvelope message,
            CancellationToken cancellationToken)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public void RaiseMessage(MessageEnvelope message)
        {
            MessageReceived?.Invoke(
                this,
                new ClientMessageReceivedEventArgs(message));
        }

        public void RaiseConnectionError(Exception exception)
        {
            _connectionError?.Invoke(this, exception);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
