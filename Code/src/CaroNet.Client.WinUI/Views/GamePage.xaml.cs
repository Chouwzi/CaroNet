using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media; // Thêm namespace này ở đầu file GamePage.xaml.cs

namespace CaroNet.Client.WinUI.Views;

public sealed partial class GamePage : Page
{

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

    public GamePage()
    {
        this.InitializeComponent();

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
            button.Click += BoardCellButton_Click;

            Grid.SetRow(button, cell.Row);
            Grid.SetColumn(button, cell.Column);
            BoardGrid.Children.Add(button);
        }
    }

    // Sự kiện người chơi click vào một ô trên bàn cờ để đánh X / O
    private async void BoardCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BoardCellViewModel cell })
        {
            await _viewModel.MakeMoveAsync(cell.Row, cell.Column);
        }
    }
}