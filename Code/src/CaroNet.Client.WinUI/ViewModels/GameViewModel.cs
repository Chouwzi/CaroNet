using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class GameViewModel : INotifyPropertyChanged
{
    public const int BoardSize = 15;

    private string _currentMark = "X";
    private string _currentTurnName = "Username A";

    public GameViewModel()
    {
        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                BoardCells.Add(new BoardCellViewModel(row, column));
            }
        }

        ChatMessages.Add(new ChatMessageViewModel(PlayerBName, "Bạn đi trước đi.", false));
        ChatMessages.Add(new ChatMessageViewModel(PlayerAName, "Ok, mình đánh giữa bàn nhé.", true));
        ChatMessages.Add(new ChatMessageViewModel(PlayerBName, "Cẩn thận, mình đang chặn rồi đó.", false));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PlayerAName { get; } = "Username A";

    public string PlayerBName { get; } = "Username B";

    public int PlayerAScore { get; } = 0;

    public int PlayerBScore { get; } = 1;

    public string PlayerATimerText { get; } = "00:30";

    public string PlayerBTimerText { get; } = "00:30";

    public int TotalGames { get; } = 160;

    public string WinLoseRatio { get; } = "1 / 3";

    public ObservableCollection<BoardCellViewModel> BoardCells { get; } = [];

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public string CurrentTurnName
    {
        get => _currentTurnName;
        private set => SetProperty(ref _currentTurnName, value);
    }

    public void PlaceMark(int row, int column)
    {
        var cell = BoardCells[row * BoardSize + column];
        if (!string.IsNullOrEmpty(cell.Mark))
        {
            return;
        }

        cell.Mark = _currentMark;
        _currentMark = _currentMark == "X" ? "O" : "X";
        CurrentTurnName = _currentMark == "X" ? PlayerAName : PlayerBName;
    }

    public void AddMyChatMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ChatMessages.Add(new ChatMessageViewModel(PlayerAName, text.Trim(), true));
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class BoardCellViewModel : INotifyPropertyChanged
{
    private string _mark = string.Empty;

    public BoardCellViewModel(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Row { get; }

    public int Column { get; }

    public string Mark
    {
        get => _mark;
        set
        {
            if (_mark == value)
            {
                return;
            }

            _mark = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mark)));
        }
    }
}

public sealed record ChatMessageViewModel(string SenderName, string Text, bool IsMine);
