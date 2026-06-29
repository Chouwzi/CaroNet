using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

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
        try
        {
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;

            await _viewModel.LoadAsync();

            HistoryList.ItemsSource = _viewModel.Matches;

            EmptyText.Visibility =
                _viewModel.Matches.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Lỗi",
                Content = $"Không thể tải lịch sử trận đấu.\n\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
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