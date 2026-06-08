using System.Threading;
using CaroNet.Client.WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ServerPortTextBox.Text, out var port))
        {
            ConnectionStatusText.Text = "Port server không hợp lệ.";
            return;
        }

        await AppServices.GameClient.ConnectAsync(
            new ConnectionRequest(PlayerNameTextBox.Text, ServerHostTextBox.Text, port),
            CancellationToken.None);

        ConnectionStatusText.Text = $"Đã connect tới {ServerHostTextBox.Text}:{port}";
    }

    private async void CreateRoomButton_Click(object sender, RoutedEventArgs e)
    {
        await AppServices.GameClient.CreateRoomAsync(CancellationToken.None);
        Frame.Navigate(typeof(GamePage));
    }

    private async void JoinRoomButton_Click(object sender, RoutedEventArgs e)
    {
        await AppServices.GameClient.JoinRoomAsync(RoomIdTextBox.Text, CancellationToken.None);
        Frame.Navigate(typeof(GamePage));
    }
}
