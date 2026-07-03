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
    Task SendChatAsync(string message);
    event EventHandler<GameViewState>? GameStateUpdated;

    GameViewState CurrentState { get; }

    Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken);

    Task<GameViewState> CreateRoomAsync(CancellationToken cancellationToken);

    Task<GameViewState> JoinRoomAsync(string roomId, CancellationToken cancellationToken);

    Task MakeMoveAsync(BoardPosition position, CancellationToken cancellationToken);
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
    int OpponentScore = 0);

public sealed record CellViewState(int Row, int Column, string Mark);
