using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol.Payloads;

namespace CaroNet.Client.WinUI.Tests;

public sealed class GameViewModelTests
{
    [Fact]
    public void InitialState_ShowsWaitingTurnMessage()
    {
        var viewModel = new GameViewModel(new FakeGameClientService());

        Assert.False(viewModel.IsMyTurn);
        Assert.Equal("Đang chờ đối thủ...", viewModel.TurnMessage);
    }

    [Fact]
    public void GameStateUpdated_WhenCurrentTurnIsPlayer_ShowsMyTurn()
    {
        var service = new FakeGameClientService();
        var viewModel = new GameViewModel(service);
        viewModel.SetDispatcher(action => action());

        service.RaiseState(new GameViewState(
            "123456",
            "Alice",
            "X",
            "X",
            "Đã vào phòng 123456",
            string.Empty,
            []));

        Assert.True(viewModel.IsMyTurn);
        Assert.Equal("🎯 Lượt của bạn!", viewModel.TurnMessage);
    }

    [Fact]
    public void GameStateUpdated_WhenCurrentTurnIsOpponent_ShowsOpponentTurn()
    {
        var service = new FakeGameClientService();
        var viewModel = new GameViewModel(service);
        viewModel.SetDispatcher(action => action());

        service.RaiseState(new GameViewState(
            "123456",
            "Alice",
            "X",
            "O",
            "Đã vào phòng 123456",
            string.Empty,
            []));

        Assert.False(viewModel.IsMyTurn);
        Assert.Equal("⏳ Đợi đối thủ...", viewModel.TurnMessage);
    }

    private sealed class FakeGameClientService : IGameClientService
    {
        public event EventHandler<ChatReceivedPayload>? ChatReceived;

        public event EventHandler<GameViewState>? GameStateUpdated;

        public GameViewState CurrentState { get; } = new(
            string.Empty,
            "Player",
            "?",
            "X",
            "Chưa kết nối server",
            string.Empty,
            []);

        public Task ConnectAsync(
            ConnectionRequest request,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<GameViewState> CreateRoomAsync(
            CancellationToken cancellationToken) => Task.FromResult(CurrentState);

        public Task<GameViewState> JoinRoomAsync(
            string roomId,
            CancellationToken cancellationToken) => Task.FromResult(CurrentState);

        public Task MakeMoveAsync(
            BoardPosition position,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SendChatAsync(string message) => Task.CompletedTask;

        public void RaiseState(GameViewState state)
        {
            GameStateUpdated?.Invoke(this, state);
        }

        public void RaiseChat(ChatReceivedPayload payload)
        {
            ChatReceived?.Invoke(this, payload);
        }
    }
}
