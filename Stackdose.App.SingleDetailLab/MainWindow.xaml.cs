using Stackdose.App.SingleDetailLab.Services;
using Stackdose.UI.Templates.Pages;
using System.IO;
using System.Windows;

namespace Stackdose.App.SingleDetailLab;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configDirectory = ResolveConfigDirectory();
        var machine = LabMachineConfigLoader.LoadEnabledMachines(configDirectory).FirstOrDefault();
        if (machine is null)
        {
            MessageBox.Show("No enabled machine config found in Config/.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (MainShell.ShellContent is SingleDetailWorkspacePage workspacePage)
        {
            workspacePage.Initialize(
                machine.Machine.Name,
                machine.Machine.Id,
                machine.Plc.Ip,
                machine.Plc.Port,
                machine.Plc.PollIntervalMs,
                machine.Plc.AutoConnect,
                LabMonitorAddressBuilder.Build(machine));
        }

        MainShell.HeaderDeviceName = machine.Machine.Name;
        MainShell.PageTitle = $"{machine.Machine.Name} - Detail Lab";
    }

    private static string ResolveConfigDirectory()
    {
        var baseConfig = Path.Combine(AppContext.BaseDirectory, "Config");
        if (Directory.Exists(baseConfig))
        {
            return baseConfig;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            var projectConfig = Path.Combine(current.FullName, "Stackdose.App.SingleDetailLab", "Config");
            if (Directory.Exists(projectConfig))
            {
                return projectConfig;
            }

            current = current.Parent;
        }

        return baseConfig;
    }

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MainShell_OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
