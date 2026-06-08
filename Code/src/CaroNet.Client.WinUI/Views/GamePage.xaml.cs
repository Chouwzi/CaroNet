using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{
    private readonly GameViewModel _viewModel = new(AppServices.GameClient);

    public GamePage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        BuildBoard();
    }

    private void BuildBoard()
    {
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Children.Clear();

        for (var index = 0; index < GameViewModel.BoardSize; index++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition());
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        foreach (var cell in _viewModel.BoardCells)
        {
            var button = new Button
            {
                DataContext = cell,
                Style = (Style)Resources["BoardCellButtonStyle"],
            };

            button.SetBinding(ContentProperty, new Binding { Path = new PropertyPath(nameof(BoardCellViewModel.Mark)) });
            button.Click += BoardCellButton_Click;

            Grid.SetRow(button, cell.Row);
            Grid.SetColumn(button, cell.Column);
            BoardGrid.Children.Add(button);
        }
    }

    private async void BoardCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BoardCellViewModel cell })
        {
            await _viewModel.MakeMoveAsync(cell.Row, cell.Column);
        }
    }
}
