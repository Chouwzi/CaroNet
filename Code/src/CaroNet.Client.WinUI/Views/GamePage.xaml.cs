using CaroNet.Client.WinUI.ViewModels;
using CaroNet.Shared.Game;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
<<<<<<< HEAD
using Microsoft.UI.Xaml.Data;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media; // Thêm namespace này ở đầu file GamePage.xaml.cs
=======
using Microsoft.UI.Xaml.Media;
using Windows.UI;
>>>>>>> feature/43-turn-indicator

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{
<<<<<<< HEAD

    private T? FindVisualChild<T>(DependencyObject element) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child is T t) return t;

            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }
    private readonly GameViewModel _viewModel;
=======
    private GameViewModel? _viewModel;
>>>>>>> feature/43-turn-indicator

    public GamePage()
    {
        this.InitializeComponent();
<<<<<<< HEAD

        // 1. Khởi tạo ViewModel kết nối qua dịch vụ Game mạng (AppServices.GameClient) của bạn
        _viewModel = new GameViewModel(AppServices.GameClient);
        this.DataContext = _viewModel;

        // 2. Thiết lập DispatcherQueue chuẩn WinUI 3 để tránh crash khi nhận dữ liệu từ Thread chạy ngầm
        _viewModel.SetDispatcher(action => DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
            () => action()));

        // 3. Đăng ký sự kiện theo dõi tin nhắn chat để cập nhật UI
        _viewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;

        // Trạng thái mờ hiển thị ban đầu khi chưa có ai nhắn tin
        EmptyStateTextBlock.Visibility = Visibility.Visible;

        // 4. Vẽ bàn cờ Caro lên giao diện Grid
        BuildBoard();
    }

    // Xử lý logic hiển thị/cuộn danh sách Chat
    // ✅ ĐOẠN CODE MỚI ĐÃ ĐƯỢC BẢO VỆ, KHÔNG LO BỊ CRASH
    private void ChatMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Bọc TOÀN BỘ logic xử lý sự kiện vào UI Thread để tránh COMException hoàn toàn
        DispatcherQueue.TryEnqueue(() =>
        {
            // 1. Cập nhật giao diện trạng thái trống (Dòng này trước đây chạy ở luồng ngầm gây crash)
            EmptyStateTextBlock.Visibility = _viewModel.ChatMessages.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            // 2. Tự động cuộn xuống cuối khi có tin nhắn mới được thêm vào
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (ChatListView.Items.Count > 0)
                {
                    // Lấy ra item cuối cùng ngay tại thời điểm này
                    var lastItem = ChatListView.Items[^1];

                    // Đẩy lệnh cuộn vào DispatcherQueue để đợi UI ổn định layout
                    this.DispatcherQueue.TryEnqueue(() =>
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
            }
        });
    }

    // Sự kiện nút gửi tin nhắn Chat
    private async void SendChatButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SendChatAsync();
    }

    // Logic sinh động sinh các ô nút bấm (Button) cho bàn cờ Caro của bạn
=======
        this.Loaded += GamePage_Loaded;
    }

    private void GamePage_Loaded(object sender, RoutedEventArgs e)
    {
        // Khởi tạo ViewModel và tạo bàn cờ (giống develop)
        if (DataContext is GameViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            BuildBoard();           // Tạo bàn cờ 15x15
            UpdateTurnUI();         // Cập nhật màu theo lượt lần đầu
        }
    }

    /// <summary>
    /// Tạo bàn cờ 15x15 (giữ nguyên từ develop)
    /// </summary>
>>>>>>> feature/43-turn-indicator
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

<<<<<<< HEAD
    // Sự kiện người chơi click vào một ô trên bàn cờ để đánh X / O
    private async void BoardCellButton_Click(object sender, RoutedEventArgs e)
=======
    private async void BoardButton_Click(object sender, RoutedEventArgs e)
>>>>>>> feature/43-turn-indicator
    {
        if (sender is Button button && button.Tag is BoardPosition position && _viewModel != null)
        {
            await _viewModel.MakeMoveAsync(position.Row, position.Column);
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

        // === Đổi màu Banner ===
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

        // === Đổi màu viền bảng cờ ===
        var boardBorder = BoardGrid?.Parent as Border;
        if (boardBorder != null)
        {
            boardBorder.BorderBrush = isMyTurn
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158));
        }

        // === Bật/Tắt ô cờ ===
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