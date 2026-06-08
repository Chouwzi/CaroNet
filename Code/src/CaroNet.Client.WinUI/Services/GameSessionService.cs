using CaroNet.Client.WinUI.ViewModels.Contracts;

namespace CaroNet.Client.WinUI.Services
{
    public class GameSessionService
    {
        private readonly IGameBoardViewModel _gameVM;

        public GameSessionService(IGameBoardViewModel gameVM)
        {
            _gameVM = gameVM;
        }

        public void HandleGameStateUpdated(string currentPlayer, bool isMyTurn, string status)
        {
            _gameVM.CurrentPlayer = currentPlayer;
            _gameVM.IsMyTurn = isMyTurn;
            _gameVM.GameStatus = status;
        }

        public void HandleMoveAccepted(int row, int col, string player)
        {
            _gameVM.Board[row][col] = player;
            _gameVM.CurrentPlayer = (player == "X") ? "O" : "X";
            _gameVM.IsMyTurn = !_gameVM.IsMyTurn;
        }

        public void HandleGameStarted()
        {
            _gameVM.GameStatus = "Trận đấu bắt đầu!";
            _gameVM.IsMyTurn = true;
        }

        
        public void HandleFullBoardUpdate(string[,] boardData, string currentPlayer, bool isMyTurn, string status)
        {
            _gameVM.UpdateBoardFromServer(boardData);
            _gameVM.CurrentPlayer = currentPlayer;
            _gameVM.IsMyTurn = isMyTurn;
            _gameVM.GameStatus = status;
        }
    }
}