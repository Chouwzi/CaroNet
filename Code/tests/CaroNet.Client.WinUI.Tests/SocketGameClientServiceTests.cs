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
            add { }
            remove { }
        }

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

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
