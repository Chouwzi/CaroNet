using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;

namespace CaroNet.Client.WinUI.Tests;

public sealed class MainMenuViewModelTests
{
    [Fact]
    public async Task ConnectAsync_returns_false_and_keeps_error_status_when_service_fails()
    {
        var service = new FailingGameClientService(
            new InvalidOperationException("Không thể kết nối server."));
        var viewModel = new MainMenuViewModel(service)
        {
            PlayerName = "Annie"
        };

        bool connected = await viewModel.ConnectAsync();

        Assert.False(connected);
        Assert.Contains("Không thể kết nối server", viewModel.ConnectionStatus);
    }

    private sealed class FailingGameClientService(Exception exception) : IGameClientService
    {
        public event EventHandler<GameViewState>? GameStateUpdated
        {
            add { }
            remove { }
        }

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
            CancellationToken cancellationToken)
        {
            throw exception;
        }

        public Task<GameViewState> CreateRoomAsync(CancellationToken cancellationToken)
        {
            throw exception;
        }

        public Task<GameViewState> JoinRoomAsync(
            string roomId,
            CancellationToken cancellationToken)
        {
            throw exception;
        }

        public Task MakeMoveAsync(
            BoardPosition position,
            CancellationToken cancellationToken)
        {
            throw exception;
        }
    }
}
