using System.Windows;
using Stackdose.App.ModelE.SinglePage.Pages;
using Stackdose.App.ModelE.SinglePage.Services;
using Stackdose.App.ModelE.SinglePage.ViewModels;

namespace Stackdose.App.ModelE.SinglePage;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(
            runtimeService: new SinglePageRuntimeService("Stackdose.App.ModelE.SinglePage"),
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

            page.SecuredSampleButtonClicked -= OnSecuredSampleButtonClicked;
            page.SecuredSampleButtonClicked += OnSecuredSampleButtonClicked;
        }

        _viewModel.AttachEvents();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (MainShell.ShellContent is SingleDetailWorkspacePage page)
        {
            page.SecuredSampleButtonClicked -= OnSecuredSampleButtonClicked;
        }

        _viewModel.DetachEvents();
    }

    private void OnSecuredSampleButtonClicked(object? sender, EventArgs e) => _viewModel.SecuredSampleButtonCommand.Execute(null);

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e) => _viewModel.LogoutCommand.Execute(null);
    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e) => _viewModel.MinimizeCommand.Execute(null);
    private void MainShell_OnCloseRequested(object? sender, EventArgs e) => _viewModel.CloseCommand.Execute(null);
}
