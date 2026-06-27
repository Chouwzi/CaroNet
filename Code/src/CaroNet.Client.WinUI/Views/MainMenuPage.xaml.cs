using CaroNet.Client.WinUI.Services;
using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    private readonly MainMenuViewModel _viewModel = new(AppServices.GameClient);

    public MainMenuPage()
    {
        InitializeComponent();
        DataContext = _viewModel;
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
    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(HistoryPage));
    }
}
