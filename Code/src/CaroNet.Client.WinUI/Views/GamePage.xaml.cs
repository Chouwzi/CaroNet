using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

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
        _viewModel = DataContext as GameViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
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

        
        if (TurnBanner != null)
        {
            if (isMyTurn)
            {
                TurnBanner.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 232, 245, 233));
                TurnBanner.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 165, 214, 167));
            }
            else
            {
                TurnBanner.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 245, 245, 245));
                TurnBanner.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 189, 189, 189));
            }
        }

        
        var boardBorder = BoardGrid?.Parent as Border;
        if (boardBorder != null)
        {
            boardBorder.BorderBrush = isMyTurn
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158));
        }

        
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
}
