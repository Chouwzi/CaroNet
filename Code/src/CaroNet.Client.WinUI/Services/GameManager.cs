using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;

namespace CaroNet.Client.WinUI.Services
{

    public class GameManager
    {
        public GameBoardViewModel GameBoardVM { get; private set; }
        public GameSessionService GameSessionService { get; private set; }
        public NetworkMessageHandler MessageHandler { get; private set; }

        public GameManager()
        {
            
            GameBoardVM = new GameBoardViewModel();
            GameSessionService = new GameSessionService(GameBoardVM);
            MessageHandler = new NetworkMessageHandler(GameSessionService);
        }

        
        public void SimulateReceiveGameState()
        {
            
            MessageHandler.OnGameStateUpdatedReceived(
                currentPlayer: "X",
                isMyTurn: true,
                status: "Đến lượt bạn"
            );
        }

        public void SimulateReceiveMove(int row, int col, string player)
        {
            MessageHandler.OnMoveAcceptedReceived(row, col, player);
        }
    }
}