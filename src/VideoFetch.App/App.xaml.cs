using System.Windows;
using VideoFetch.App.Services;
using VideoFetch.App.ViewModels;

namespace VideoFetch.App;

/// <summary>
/// WPF application entry point and composition root.
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainViewModel viewModel = new(
            new ClientServiceFactory(),
            new WindowsFileDialogService());
        MainWindow window = new()
        {
            DataContext = viewModel,
        };
        MainWindow = window;
        window.Show();

        await viewModel.InitializeAsync();
    }
}
