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
        LoadingRing.Visibility = Visibility.Visible;
        LoadingRing.IsActive = true;

        await _viewModel.LoadAsync();

        HistoryList.ItemsSource = _viewModel.Matches;

        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;

        EmptyText.Visibility =
            _viewModel.Matches.Count == 0
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