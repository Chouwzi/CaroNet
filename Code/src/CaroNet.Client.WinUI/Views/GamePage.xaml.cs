using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{
    private readonly GameViewModel _viewModel;
    private bool _gameEndDialogShowing;

    public GamePage()
    {
        this.InitializeComponent();

        _viewModel = new GameViewModel(AppServices.GameClient);
        this.DataContext = _viewModel;

        _viewModel.SetDispatcher(action => DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            () => action()));

        _viewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        EmptyStateTextBlock.Visibility = Visibility.Visible;

        BuildBoard();
        UpdateTurnUI();
    }

    private async void ViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameViewModel.IsMyTurn) ||
            e.PropertyName == nameof(GameViewModel.TurnMessage) ||
            e.PropertyName == nameof(GameViewModel.IsGameEnded) ||
            e.PropertyName == nameof(GameViewModel.ConnectionStatus))
        {
            UpdateTurnUI();
        }

        if (e.PropertyName == nameof(GameViewModel.ConnectionStatus) &&
            _viewModel.ConnectionStatus.StartsWith("Trận đấu mới", StringComparison.Ordinal))
        {
            BuildBoard();
            UpdateTurnUI();
            return;
        }

        if (e.PropertyName == nameof(GameViewModel.ServerError) &&
            _viewModel.ServerError == "Đối thủ đã ngắt kết nối. Bạn thắng!")
        {
            await ShowOpponentDisconnectedDialogAsync();
            return;
        }

        if ((e.PropertyName == nameof(GameViewModel.IsGameEnded) ||
             e.PropertyName == nameof(GameViewModel.ConnectionStatus) ||
             e.PropertyName == nameof(GameViewModel.ServerError)) &&
            _viewModel.IsGameEnded &&
            !_gameEndDialogShowing)
        {
            _gameEndDialogShowing = true;
            HighlightWinningCellsOnUI();
            await ShowGameEndedDialogAsync();
            _gameEndDialogShowing = false;
        }
    }

    private void ChatMessages_CollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            EmptyStateTextBlock.Visibility = _viewModel.ChatMessages.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                ChatListView.Items.Count > 0)
            {
                var lastItem = ChatListView.Items[^1];
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        ChatListView.ScrollIntoView(lastItem);
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"Auto-scroll failed: {ex.Message}");
                    }
                });
            }
        });
    }

    private async void SendChatButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SendChatAsync();
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
            var cellContent = new Grid
            {
                DataContext = cell,
                IsHitTestVisible = false
            };

            var markText = new TextBlock
            {
                FontSize = 22,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            markText.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath(nameof(BoardCellViewModel.Mark)),
                Mode = BindingMode.OneWay,
            });

            var lastMoveDot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 3, 0)
            };

            lastMoveDot.SetBinding(UIElement.OpacityProperty, new Binding
            {
                Path = new PropertyPath(nameof(BoardCellViewModel.LastMoveIndicatorOpacity)),
                Mode = BindingMode.OneWay,
            });

            cellContent.Children.Add(markText);
            cellContent.Children.Add(lastMoveDot);

            var button = new Button
            {
                DataContext = cell,
                Content = cellContent,
                Style = (Style)Resources["BoardCellButtonStyle"],
            };

            button.SetBinding(Control.IsEnabledProperty, new Binding
            {
                Path = new PropertyPath(nameof(BoardCellViewModel.IsInteractionEnabled)),
                Mode = BindingMode.OneWay,
            });

            button.Click += BoardCellButton_Click;

            Grid.SetRow(button, cell.Row);
            Grid.SetColumn(button, cell.Column);
            BoardGrid.Children.Add(button);
        }
    }

    private async void BoardCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsMyTurn || _viewModel.IsGameEnded)
        {
            return;
        }

        if (sender is Button { DataContext: BoardCellViewModel cell })
        {
            await _viewModel.MakeMoveAsync(cell.Row, cell.Column);
        }
    }

    private void UpdateTurnUI()
    {
        bool canPlay = _viewModel.IsMyTurn && !_viewModel.IsGameEnded;

        if (TurnBanner != null)
        {
            if (canPlay)
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
            boardBorder.BorderBrush = canPlay
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158));
        }

        if (BoardGrid is null)
        {
            return;
        }

        foreach (var child in BoardGrid.Children)
        {
            if (child is not Button button)
            {
                continue;
            }

            button.IsHitTestVisible = canPlay;
            button.Opacity = canPlay ? 1.0 : 0.65;
        }
    }

    private void HighlightWinningCellsOnUI()
    {
        if (AppServices.GameClient is not SocketGameClientService socketService ||
            socketService.WinningCells.Count == 0)
        {
            return;
        }

        foreach (var child in BoardGrid.Children)
        {
            if (child is not Button button)
            {
                continue;
            }

            int row = Grid.GetRow(button);
            int column = Grid.GetColumn(button);
            if (socketService.WinningCells.Any(cell => cell.Row == row && cell.Col == column))
            {
                button.Background = new SolidColorBrush(Colors.Yellow);
                button.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            }
        }
    }

    private async Task ShowOpponentDisconnectedDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Kết thúc trận đấu",
            Content = "Đối thủ đã ngắt kết nối. Bạn thắng!",
            CloseButtonText = "Về menu",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
        Frame.Navigate(typeof(MainMenuPage));
    }

    private async Task ShowGameEndedDialogAsync()
    {
        string titleText = "Hòa!";
        var iconColor = Colors.DarkGray;
        string statusIcon = "•";

        if (AppServices.GameClient is SocketGameClientService socketService &&
            socketService.WinningCells.Count > 0)
        {
            var firstWinCell = socketService.WinningCells.First();
            var matchingCell = _viewModel.BoardCells.FirstOrDefault(
                cell => cell.Row == firstWinCell.Row && cell.Column == firstWinCell.Col);

            if (matchingCell is not null && !string.IsNullOrEmpty(matchingCell.Mark))
            {
                if (matchingCell.Mark == _viewModel.PlayerSymbol)
                {
                    titleText = "Bạn thắng!";
                    statusIcon = "✓";
                    iconColor = Colors.Green;
                }
                else
                {
                    titleText = "Bạn thua!";
                    statusIcon = "✗";
                    iconColor = Colors.Red;
                }
            }
        }

        var titleStackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };

        titleStackPanel.Children.Add(new TextBlock
        {
            Text = statusIcon,
            Foreground = new SolidColorBrush(iconColor),
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });

        titleStackPanel.Children.Add(new TextBlock
        {
            Text = titleText,
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        var dialog = new ContentDialog
        {
            Title = titleStackPanel,
            Content = string.IsNullOrEmpty(_viewModel.ServerError)
                ? "Ván đấu đã khép lại thành công."
                : _viewModel.ServerError,
            PrimaryButtonText = "Về menu",
            SecondaryButtonText = "Chơi lại",
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            Frame.Navigate(typeof(MainMenuPage));
        }
        else if (result == ContentDialogResult.Secondary &&
            AppServices.GameClient is SocketGameClientService clientService)
        {
            _viewModel.ConnectionStatus = "Đang chờ đối thủ xác nhận...";

            try
            {
                await clientService.SendRematchRequestAsync();
            }
            catch (Exception ex)
            {
                _viewModel.ServerError = $"Không thể gửi yêu cầu: {ex.Message}";
            }
        }
    }
}
