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
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        string? serverHost = null;
        int? serverPort = null;

        try
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

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

                    if (lines.Length >= 3)
                    {
                        serverHost = lines[1];
                        if (int.TryParse(lines[2], out int p2))
                        {
                            serverPort = p2;
                        }
                    }
                    else if (lines.Length >= 2)
                    {
                        serverHost = lines[0];
                        if (int.TryParse(lines[1], out int p2))
                        {
                            serverPort = p2;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        if (!string.IsNullOrEmpty(serverHost))
            _viewModel.ServerHost = serverHost;

        if (serverPort.HasValue)
            _viewModel.ServerPort = serverPort.Value;
    }

    private void PasswordInput_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        _viewModel.Password = PasswordInput.Password;
    }

    private async void LoginButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.LoginAsync();
    }

    private async void CreateAccountLink_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ShowRegisterDialogAsync();
    }

    private async System.Threading.Tasks.Task ShowRegisterDialogAsync()
    {
        var usernameBox = new TextBox
        {
            Header = "Tên đăng nhập",
            PlaceholderText = "Ví dụ: annie123"
        };

        var passwordBox = new PasswordBox
        {
            Header = "Mật khẩu",
            PlaceholderText = "Tối thiểu 4 ký tự"
        };

        var displayNameBox = new TextBox
        {
            Header = "Tên hiển thị",
            PlaceholderText = "Ví dụ: Annie"
        };

        var errorText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        var content = new StackPanel
        {
            Spacing = 12
        };

        content.Children.Add(usernameBox);
        content.Children.Add(passwordBox);
        content.Children.Add(displayNameBox);
        content.Children.Add(errorText);

        var dialog = new ContentDialog
        {
            Title = "Tạo tài khoản",
            Content = content,
            PrimaryButtonText = "Tạo tài khoản",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            ContentDialogButtonClickDeferral deferral = args.GetDeferral();
            try
            {
                _viewModel.Username = usernameBox.Text;
                _viewModel.Password = passwordBox.Password;
                _viewModel.DisplayName = displayNameBox.Text;

                bool registered = await _viewModel.RegisterAsync();
                args.Cancel = !registered;
                errorText.Text = registered ? string.Empty : _viewModel.AuthStatus;
            }
            finally
            {
                deferral.Complete();
            }
        };

        await dialog.ShowAsync();
    }

    private async void QuickMatchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (await _viewModel.QuickMatchAsync())
        {
            Frame.Navigate(typeof(GamePage));
        }
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
        var roomIdBox = new TextBox
        {
            Header = "Mã phòng",
            PlaceholderText = "Nhập mã phòng",
            Text = _viewModel.RoomId
        };

        var dialog = new ContentDialog
        {
            Title = "Vào phòng",
            Content = roomIdBox,
            PrimaryButtonText = "Vào",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        _viewModel.RoomId = roomIdBox.Text;

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
