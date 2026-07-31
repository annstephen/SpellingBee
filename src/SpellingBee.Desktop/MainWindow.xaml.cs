using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace SpellingBee.Desktop;

public partial class MainWindow : Window
{
    private Microsoft.AspNetCore.Builder.WebApplication? _app;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _app = ApiHost.Build(["--urls=http://127.0.0.1:0"]);
        await _app.StartAsync();

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        var webViewDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpellingBee", "WebView2");
        var webViewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: webViewDataFolder);
        await WebView.EnsureCoreWebView2Async(webViewEnvironment);

        WebView.Source = new Uri(address);
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isClosing || _app is null)
            return;

        e.Cancel = true;
        _isClosing = true;
        await _app.StopAsync();
        _app = null;
        Close();
    }
}
