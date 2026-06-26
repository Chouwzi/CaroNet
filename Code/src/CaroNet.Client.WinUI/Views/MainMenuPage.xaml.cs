using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    private readonly MainMenuViewModel _viewModel = new(AppServices.GameClient);

    public MainMenuPage()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        string? playerName = null;
        string? serverHost = null;
        int? serverPort = null;

        try
        {
            // 1. Chạy chế độ Đóng gói (Packaged) -> Đọc LocalSettings theo đúng yêu cầu Issue
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            if (localSettings.ContainsKey("PlayerName")) playerName = localSettings["PlayerName"] as string;
            if (localSettings.ContainsKey("ServerHost")) serverHost = localSettings["ServerHost"] as string;
            if (localSettings.ContainsKey("ServerPort") && int.TryParse(localSettings["ServerPort"]?.ToString(), out int p1)) serverPort = p1;
        }
        catch (InvalidOperationException)
        {
            // 2. Chạy chế độ Chưa đóng gói (Unpackaged) -> Tự động đọc file txt phòng hờ để không sập app
            try
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroNet", "settings.txt");
                if (System.IO.File.Exists(path))
                {
                    var lines = System.IO.File.ReadAllLines(path);
                    if (lines.Length >= 1) playerName = lines[0];
                    if (lines.Length >= 2) serverHost = lines[1];
                    if (lines.Length >= 3 && int.TryParse(lines[2], out int p2)) serverPort = p2;
                }
            }
            catch { /* Bỏ qua lỗi đọc file hệ thống nếu có */ }
        }

        // Đổ dữ liệu tìm được vào các ô nhập liệu trên Giao diện
        if (!string.IsNullOrEmpty(playerName))
        {
            _viewModel.PlayerName = playerName;
        }
        if (!string.IsNullOrEmpty(serverHost))
        {
            _viewModel.ServerHost = serverHost;
        }
        if (serverPort.HasValue)
        {
            _viewModel.ServerPort = serverPort.Value;
        }
    

        // Đổ dữ liệu tìm được vào Giao diện thông qua ViewModel
        if (!string.IsNullOrEmpty(playerName)) _viewModel.PlayerName = playerName;
        _viewModel.ServerHost = !string.IsNullOrEmpty(serverHost) ? serverHost : "127.0.0.1";
        if (serverPort.HasValue) _viewModel.ServerPort = serverPort.Value;
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ConnectAsync();
    }

    private async void CreateRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.CreateRoomAsync())
        {
            Frame.Navigate(typeof(GamePage));
        }
    }

    private async void JoinRoomButton_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.JoinRoomAsync())
        {
            Frame.Navigate(typeof(GamePage));
        }
    }
}
