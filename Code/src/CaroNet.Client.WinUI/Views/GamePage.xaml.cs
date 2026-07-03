using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{
    private GameViewModel? _viewModel;

    public GamePage()
    {
        this.InitializeComponent();
        this.Loaded += GamePage_Loaded;
    }

    private void GamePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            BuildBoard();
            UpdateTurnUI();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameViewModel.IsMyTurn) ||
            e.PropertyName == nameof(GameViewModel.TurnMessage))
        {
            UpdateTurnUI();
        }
    }

    private void UpdateTurnUI()
    {
        if (_viewModel == null) return;

        bool isMyTurn = _viewModel.IsMyTurn;

        // Đổi màu Banner
        if (TurnBanner != null)
        {
            TurnBanner.Background = isMyTurn
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 232, 245, 233))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 245, 245, 245));

            TurnBanner.BorderBrush = isMyTurn
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 165, 214, 167))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 189, 189, 189));
        }

        // Đổi màu viền bảng
        var boardBorder = BoardGrid?.Parent as Border;
        if (boardBorder != null)
        {
            boardBorder.BorderBrush = isMyTurn
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158));
        }

        // Bật/Tắt ô cờ
        if (BoardGrid != null)
        {
            foreach (var child in BoardGrid.Children)
            {
                if (child is Button button)
                {
                    button.IsEnabled = isMyTurn;
                    button.IsHitTestVisible = isMyTurn;
                    button.Opacity = isMyTurn ? 1.0 : 0.65;
                }
            }
        }
    }

    private void BuildBoard()
    {
        if (_viewModel == null) return;

        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();

        for (int i = 0; i < GameViewModel.BoardSize; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int row = 0; row < GameViewModel.BoardSize; row++)
        {
            for (int col = 0; col < GameViewModel.BoardSize; col++)
            {
                var button = new Button
                {
                    Content = "",
                    FontSize = 22,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(1),
                    Tag = new BoardPosition(row, col)
                };

                button.Click += BoardButton_Click;

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                BoardGrid.Children.Add(button);
            }
        }
    }

    private async void BoardButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BoardPosition position && _viewModel != null)
        {
            await _viewModel.MakeMoveAsync(position.Row, position.Column);
        }
    }
}