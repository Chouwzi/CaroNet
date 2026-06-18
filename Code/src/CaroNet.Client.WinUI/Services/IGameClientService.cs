using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Shared.Game;

namespace CaroNet.Client.WinUI.Services;

public interface IGameClientService
{
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
    IReadOnlyList<CellViewState> Cells);

public sealed record CellViewState(int Row, int Column, string Mark);
