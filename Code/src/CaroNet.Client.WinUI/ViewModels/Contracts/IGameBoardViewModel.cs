using System.Collections.ObjectModel;

namespace CaroNet.Client.WinUI.ViewModels.Contracts
{
    public interface IGameBoardViewModel
    {
        ObservableCollection<ObservableCollection<string>> Board { get; }

        string CurrentPlayer { get; set; }
        bool IsMyTurn { get; set; }
        string GameStatus { get; set; }

        void MakeMove(int row, int col);

        
        void UpdateBoardFromServer(string[,] boardData);
    }
}