using System.Windows;
using Stackdose.App.MySinglePage1.Pages;
using Stackdose.App.MySinglePage1.Services;
using Stackdose.App.MySinglePage1.ViewModels;

namespace Stackdose.App.MySinglePage1;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(
            runtimeService: new SinglePageRuntimeService("Stackdose.App.MySinglePage1"),
            closeAction: Close,
            minimizeAction: () => WindowState = WindowState.Minimized);

        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryInitialize(out var runtime))
        {
            return;
        }

        if (MainShell.ShellContent is SingleDetailWorkspacePage page)
        {
            page.Initialize(
                runtime.MachineName,
                runtime.MachineId,
                runtime.Ip,
                runtime.Port,
                runtime.PollIntervalMs,
                runtime.AutoConnect,
                runtime.MonitorAddress,
                runtime.SensorConfigPath,
                runtime.AlarmConfigPath);
        }

        _viewModel.AttachEvents();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.DetachEvents();
    }

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e) => _viewModel.LogoutCommand.Execute(null);
    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e) => _viewModel.MinimizeCommand.Execute(null);
    private void MainShell_OnCloseRequested(object? sender, EventArgs e) => _viewModel.CloseCommand.Execute(null);
}
