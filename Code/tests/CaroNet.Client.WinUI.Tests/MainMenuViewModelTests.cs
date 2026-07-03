using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit; // Đảm bảo đã có để nhận diện thuộc tính [Fact]

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

#pragma warning disable CS0067
    private sealed class FailingGameClientService(Exception exception) : IGameClientService
    {
        // Vô hiệu hóa cảnh báo CS0067 dành riêng cho các sự kiện giả lập của Test double
        public event EventHandler<CaroNet.Shared.Protocol.Payloads.ChatReceivedPayload>? ChatReceived;

        public event EventHandler<DrawOfferReceivedEventArgs>? DrawOfferReceived;

        public event EventHandler<GameViewState>? GameStateUpdated;
#pragma warning restore CS0067

        public Task SendChatAsync(string message) => Task.CompletedTask;

        public Task SendResignAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendDrawOfferAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendDrawResponseAsync(bool accepted, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendRematchRequestAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LeaveRoomAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

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
