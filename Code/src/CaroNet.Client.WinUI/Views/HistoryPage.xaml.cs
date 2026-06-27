using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class HistoryPage : Page
{
    private readonly HistoryViewModel _viewModel = new();

    public HistoryPage()
    {
        InitializeComponent();

        Loaded += HistoryPage_Loaded;
    }

    private async void HistoryPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        // Hiện loading
        LoadingRing.Visibility = Visibility.Visible;
        LoadingRing.IsActive = true;

        // Load dữ liệu
        await _viewModel.LoadAsync();

        // Gán dữ liệu cho ListView
        HistoryList.ItemsSource = _viewModel.Matches;

        // Tắt loading
        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;

        // Hiện thông báo nếu không có dữ liệu
        EmptyText.Visibility = _viewModel.Matches.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}