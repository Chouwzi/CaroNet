using CaroNet.Client.WinUI.ViewModels.Contracts;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CaroNet.Client.WinUI.ViewModels
{
    public class GameBoardViewModel : IGameBoardViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ObservableCollection<string>> Board { get; set; }

        private string _currentPlayer = "X";
        public string CurrentPlayer
        {
            get => _currentPlayer;
            set { _currentPlayer = value; OnPropertyChanged(nameof(CurrentPlayer)); }
        }

        private bool _isMyTurn = true;
        public bool IsMyTurn
        {
            get => _isMyTurn;
            set { _isMyTurn = value; OnPropertyChanged(nameof(IsMyTurn)); }
        }

        private string _gameStatus = "Đang chơi";
        public string GameStatus
        {
            get => _gameStatus;
            set { _gameStatus = value; OnPropertyChanged(nameof(GameStatus)); }
        }

        public GameBoardViewModel()
        {
            Board = new ObservableCollection<ObservableCollection<string>>();
            for (int i = 0; i < 15; i++)
            {
                var row = new ObservableCollection<string>();
                for (int j = 0; j < 15; j++)
                {
                    row.Add("");
                }
                Board.Add(row);
            }
        }

        public void MakeMove(int row, int col)
        {
            if (Board[row][col] != "") return;

            Board[row][col] = CurrentPlayer;
            CurrentPlayer = (CurrentPlayer == "X") ? "O" : "X";
            IsMyTurn = !IsMyTurn;
        }

        public void UpdateBoardFromServer(string[,] boardData)
        {
            if (boardData == null) return;

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    Board[row][col] = boardData[row, col] ?? "";
                }
            }

            GameStatus = "Đã cập nhật bàn cờ từ server";
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}