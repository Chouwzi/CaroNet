using CaroNet.Client.WinUI.Services;

namespace CaroNet.Client.WinUI.Services
{

    public class NetworkMessageHandler
    {
        private readonly GameSessionService _gameSessionService;

        public NetworkMessageHandler(GameSessionService gameSessionService)
        {
            _gameSessionService = gameSessionService;
        }

        
        public void OnGameStateUpdatedReceived(string currentPlayer, bool isMyTurn, string status)
        {
            _gameSessionService.HandleGameStateUpdated(currentPlayer, isMyTurn, status);
        }

        
        public void OnMoveAcceptedReceived(int row, int col, string player)
        {
            _gameSessionService.HandleMoveAccepted(row, col, player);
        }

       
        public void OnGameStartedReceived()
        {
            _gameSessionService.HandleGameStarted();
        }
    }
}