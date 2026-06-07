using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{
    private readonly GameViewModel _viewModel = new();

    public GamePage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        BuildBoard();
        BuildChatMessages();
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

    private void BoardCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BoardCellViewModel cell })
        {
            _viewModel.PlaceMark(cell.Row, cell.Column);
        }
    }

    private void BuildChatMessages()
    {
        ChatMessagesPanel.Children.Clear();

        foreach (var message in _viewModel.ChatMessages)
        {
            AddChatBubble(message);
        }
    }

    private void AddChatBubble(ChatMessageViewModel message)
    {
        var bubble = new Border
        {
            MaxWidth = 260,
            Padding = new Thickness(14, 10, 14, 10),
            CornerRadius = new CornerRadius(16),
            HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Background = message.IsMine
                ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                : (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = message.IsMine
                ? null
                : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = message.IsMine ? new Thickness(0) : new Thickness(1),
        };

        var content = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        };

        content.Children.Add(new TextBlock
        {
            Text = message.SenderName,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = message.IsMine
                ? (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"]
                : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = message.IsMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        });

        content.Children.Add(new TextBlock
        {
            Text = message.Text,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = message.IsMine
                ? (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"]
                : (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
        });

        bubble.Child = content;
        ChatMessagesPanel.Children.Add(bubble);
    }

    private void SendChatButton_Click(object sender, RoutedEventArgs e)
    {
        var beforeCount = _viewModel.ChatMessages.Count;
        _viewModel.AddMyChatMessage(ChatMessageTextBox.Text);
        if (_viewModel.ChatMessages.Count == beforeCount)
        {
            return;
        }

        AddChatBubble(_viewModel.ChatMessages[^1]);
        ChatMessageTextBox.Text = string.Empty;
        ChatScrollViewer.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
    }
}
