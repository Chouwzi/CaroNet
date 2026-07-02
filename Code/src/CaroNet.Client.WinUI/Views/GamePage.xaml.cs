using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
        InitializeComponent();

        _viewModel = new GameViewModel(AppServices.GameClient);

        _viewModel.SetDispatcher(action => DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            () => action()));

        DataContext = _viewModel;
        BuildBoard();

        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(GameViewModel.ConnectionStatus) || args.PropertyName == nameof(GameViewModel.ServerError))
            {
                if ((_viewModel.ConnectionStatus == "Trò chơi kết thúc"
                    || _viewModel.ServerError == "Ván đấu đã kết thúc.")
                    && !_gameEndDialogShowing)
                {
                    _gameEndDialogShowing = true;

                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        HighlightWinningCellsOnUI();
                        await ShowGameEndedDialogAsync();
                        _gameEndDialogShowing = false;
                    });
                }
                else if (_viewModel.ConnectionStatus.StartsWith("Trận đấu mới"))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        BuildBoard(); 
                    });
                }
            }
        };
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

            button.SetBinding(ContentControl.ContentProperty, new Binding
            {
                Path = new PropertyPath(nameof(BoardCellViewModel.Mark)),
                Mode = BindingMode.OneWay,
            });

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
        if (sender is Button { DataContext: BoardCellViewModel cell })
        {
            await _viewModel.MakeMoveAsync(cell.Row, cell.Column);
        }
    }

    private void HighlightWinningCellsOnUI()
    {
        if (AppServices.GameClient is not SocketGameClientService socketService) return;

        var targetCells = socketService.WinningCells;
        if (targetCells == null || targetCells.Count == 0) return;

        foreach (var child in BoardGrid.Children)
        {
            if (child is Button button)
            {
                int r = Grid.GetRow(button);
                int c = Grid.GetColumn(button);

                if (targetCells.Any(cell => cell.Row == r && cell.Col == c))
                {
                    button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Yellow);
                    button.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                }
            }
        }
    }

    private async Task ShowGameEndedDialogAsync()
    {
        string titleText = "Hòa!";
        var iconColor = Microsoft.UI.Colors.DarkGray;
        string statusIcon = "•";

        if (AppServices.GameClient is SocketGameClientService socketService &&
            socketService.WinningCells != null &&
            socketService.WinningCells.Count > 0)
        {
            var firstWinCell = socketService.WinningCells.First();
            var matchingCell = _viewModel.BoardCells.FirstOrDefault(c => c.Row == firstWinCell.Row && c.Column == firstWinCell.Col);

            if (matchingCell != null && !string.IsNullOrEmpty(matchingCell.Mark))
            {
                string winningMark = matchingCell.Mark;
                string myMark = _viewModel.PlayerSymbol;

                if (winningMark == myMark)
                {
                    titleText = "Bạn thắng!";
                    statusIcon = "✓";
                    iconColor = Microsoft.UI.Colors.Green; 
                }
                else
                {
                    titleText = "Bạn thua!";
                    statusIcon = "✗";
                    iconColor = Microsoft.UI.Colors.Red; 
                }
            }
        }

        var titleStackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };

        var iconTextBlock = new TextBlock
        {
            Text = statusIcon,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor),
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBlock = new TextBlock
        {
            Text = titleText,
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        titleStackPanel.Children.Add(iconTextBlock);
        titleStackPanel.Children.Add(textBlock);

        string contentText = "Ván đấu đã khép lại thành công.";

        if (!string.IsNullOrEmpty(_viewModel.ServerError))
        {
            contentText = _viewModel.ServerError;
        }
        
        else if (_viewModel.ConnectionStatus == "Đối thủ muốn chơi lại!")
        {
            contentText = "Đối thủ muốn chơi lại! Bấm nút Chơi lại bên dưới để bắt đầu ngay.";
        }
        ContentDialog gameEndedDialog = new ContentDialog
        {
            Title = titleStackPanel,
            Content = contentText,
            PrimaryButtonText = "Về menu",
            SecondaryButtonText = "Chơi lại",
            XamlRoot = this.Content.XamlRoot
        };

        ContentDialogResult result = await gameEndedDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(MainMenuPage)); 
            }
        }
        else if (result == ContentDialogResult.Secondary)
        {
            if (AppServices.GameClient is SocketGameClientService clientService)
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
}