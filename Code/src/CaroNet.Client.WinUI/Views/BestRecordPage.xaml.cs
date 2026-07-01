using CaroNet.Client.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CaroNet.Client.WinUI.Views;

public sealed partial class BestRecordPage : Page
{
    public BestRecordPage()
    {
        this.InitializeComponent();
    }

    // Hàm này tự động chạy khi màn hình BestRecordPage được kích hoạt mở lên
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Lấy ViewModel được truyền từ MainMenuPage sang và gán vào DataContext
        if (e.Parameter is MainMenuViewModel mainMenuViewModel)
        {
            DataContext = mainMenuViewModel;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}