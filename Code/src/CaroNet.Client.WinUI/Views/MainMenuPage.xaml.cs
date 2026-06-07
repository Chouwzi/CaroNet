using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class MainMenuPage : Page
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private void QuickPlayButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(GamePage));
    }
}
