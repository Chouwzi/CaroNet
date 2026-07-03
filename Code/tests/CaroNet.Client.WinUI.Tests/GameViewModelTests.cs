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

    [Fact]
    public void GameStateUpdated_WithOneNewMark_UpdatesLastMoveIndicator()
    {
        var service = new FakeGameClientService();
        var viewModel = new GameViewModel(service);
        viewModel.SetDispatcher(action => action());
        var board = CreateEmptyCells();
        board[6 * GameViewModel.BoardSize + 7] = new CellViewState(6, 7, "O");

        service.RaiseState(new GameViewState(
            "123456",
            "Alice",
            "X",
            "X",
            "Đã vào phòng 123456",
            string.Empty,
            board));

        Assert.Equal(new BoardPosition(6, 7), viewModel.LastMovePosition);
        Assert.True(viewModel.BoardCells.Single(cell => cell.Row == 6 && cell.Column == 7).IsLastMove);
        Assert.Equal(1.0, viewModel.BoardCells.Single(cell => cell.Row == 6 && cell.Column == 7).LastMoveIndicatorOpacity);
        Assert.False(viewModel.BoardCells.Single(cell => cell.Row == 0 && cell.Column == 0).IsLastMove);
    }

    [Fact]
    public void GameStateUpdated_WhenNewGameStarts_ClearsLastMoveIndicator()
    {
        var service = new FakeGameClientService();
        var viewModel = new GameViewModel(service);
        viewModel.SetDispatcher(action => action());
        var board = CreateEmptyCells();
        board[6 * GameViewModel.BoardSize + 7] = new CellViewState(6, 7, "O");

        service.RaiseState(new GameViewState(
            "123456",
            "Alice",
            "X",
            "X",
            "Đã vào phòng 123456",
            string.Empty,
            board));

        service.RaiseState(new GameViewState(
            "123456",
            "Alice",
            "X",
            "X",
            "Trận đấu mới đã bắt đầu!",
            string.Empty,
            CreateEmptyCells()));

        Assert.Null(viewModel.LastMovePosition);
        Assert.All(viewModel.BoardCells, cell => Assert.False(cell.IsLastMove));
    }

    private static List<CellViewState> CreateEmptyCells()
    {
        var cells = new List<CellViewState>(GameViewModel.BoardSize * GameViewModel.BoardSize);

        for (var row = 0; row < GameViewModel.BoardSize; row++)
        {
            for (var column = 0; column < GameViewModel.BoardSize; column++)
            {
                cells.Add(new CellViewState(row, column, string.Empty));
            }
        }

        return cells;
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
