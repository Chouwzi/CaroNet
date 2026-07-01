using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    private readonly MainMenuViewModel _viewModel =
        new(AppServices.GameClient);

    public MainMenuPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += MainMenuPage_Loaded;
    }

    private async void MainMenuPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            await _viewModel.LoadBestRecordsAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Lỗi",
                Content = $"Không thể tải bảng xếp hạng.\n\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        string? playerName = null;
        string? serverHost = null;
        int? serverPort = null;

        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

            if (localSettings.ContainsKey("PlayerName"))
                playerName = localSettings["PlayerName"] as string;

            if (localSettings.ContainsKey("ServerHost"))
                serverHost = localSettings["ServerHost"] as string;

            if (localSettings.ContainsKey("ServerPort") &&
                int.TryParse(localSettings["ServerPort"]?.ToString(), out int p1))
            {
                serverPort = p1;
            }
        }
        catch (InvalidOperationException)
        {
            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CaroNet",
                    "settings.txt");

                if (System.IO.File.Exists(path))
                {
                    var lines = System.IO.File.ReadAllLines(path);

                    if (lines.Length >= 1)
                        playerName = lines[0];

                    if (lines.Length >= 2)
                        serverHost = lines[1];

                    if (lines.Length >= 3 &&
                        int.TryParse(lines[2], out int p2))
                    {
                        serverPort = p2;
                    }
                }
            }
            catch
            {
            }
        }

        if (!string.IsNullOrEmpty(playerName))
            _viewModel.PlayerName = playerName;

        if (!string.IsNullOrEmpty(serverHost))
            _viewModel.ServerHost = serverHost;

        if (serverPort.HasValue)
            _viewModel.ServerPort = serverPort.Value;
    }

    private async void ConnectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.ConnectAsync();
    }

    private async void CreateRoomButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (await _viewModel.CreateRoomAsync())
        {
            Frame.Navigate(typeof(GamePage));
        }
    }

    private async void JoinRoomButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (await _viewModel.JoinRoomAsync())
        {
            Frame.Navigate(typeof(GamePage));
        }
    }

    private void HistoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(typeof(HistoryPage));
    }

    private void BestRecordButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Frame.Navigate(typeof(BestRecordPage), _viewModel);
    }
}