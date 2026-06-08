using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectionStatusText.Text = $"Đã nhập {ServerHostTextBox.Text}:{ServerPortTextBox.Text}";
    }

    private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(GamePage));
    }

    private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(GamePage));
    }
}
