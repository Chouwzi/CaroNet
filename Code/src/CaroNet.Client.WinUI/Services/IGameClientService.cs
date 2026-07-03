using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol.Payloads;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.Services;

public interface IGameClientService
{

    event EventHandler<ChatReceivedPayload> ChatReceived;
    event EventHandler<DrawOfferReceivedEventArgs>? DrawOfferReceived;
    Task SendChatAsync(string message);
    event EventHandler<GameViewState>? GameStateUpdated;

    GameViewState CurrentState { get; }

    Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken);

    Task<GameViewState> CreateRoomAsync(CancellationToken cancellationToken);

    Task<GameViewState> JoinRoomAsync(string roomId, CancellationToken cancellationToken);

    Task MakeMoveAsync(BoardPosition position, CancellationToken cancellationToken);

    Task SendResignAsync(CancellationToken cancellationToken = default);

    Task SendDrawOfferAsync(CancellationToken cancellationToken = default);

    Task SendDrawResponseAsync(bool accepted, CancellationToken cancellationToken = default);

    Task SendRematchRequestAsync(CancellationToken cancellationToken = default);

    Task LeaveRoomAsync(CancellationToken cancellationToken = default);
}

public sealed record ConnectionRequest(string PlayerName, string Host, int Port);

public sealed record GameViewState(
    string RoomId,
    string PlayerName,
    string PlayerSymbol,
    string CurrentTurnSymbol,
    string ConnectionStatus,
    string ServerError,
    IReadOnlyList<CellViewState> Cells,
    string OpponentName = "Đối thủ",
    int MyScore = 0,
    int OpponentScore = 0,
    bool HasOpponent = false,
    string PlayerId = "");

public sealed record CellViewState(int Row, int Column, string Mark);

public sealed record DrawOfferReceivedEventArgs(string SenderPlayerId, string SenderName);
