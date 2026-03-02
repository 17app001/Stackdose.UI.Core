using Stackdose.App.ShellShared.Services;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using System.Windows;

namespace Stackdose.App.SecondDemo;

public partial class MainWindow : Window
{
    private ShellRuntimeContext? _runtime;
    private string _defaultPageTitle = "Machine Overview";
    private string? _selectedMachineId;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _runtime = ShellRuntimeHost.Start(MainShell);
        if (_runtime is null)
        {
            return;
        }

        _defaultPageTitle = MainShell.PageTitle;
        _runtime.OverviewPage.MachineSelected += OnMachineSelected;
        MainShell.NavigationRequested += OnNavigationRequested;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.OverviewPage.MachineSelected -= OnMachineSelected;
        }

        MainShell.NavigationRequested -= OnNavigationRequested;
    }

    private void OnMachineSelected(string machineId)
    {
        NavigateToMachineDetail(machineId);
    }

    private void OnNavigationRequested(object? sender, string target)
    {
        if (_runtime is null)
        {
            return;
        }

        if (string.Equals(target, ShellNavigationTargets.Overview, StringComparison.OrdinalIgnoreCase))
        {
            MainShell.ShellContent = _runtime.OverviewPage;
            MainShell.PageTitle = _defaultPageTitle;
            if (!string.IsNullOrWhiteSpace(_selectedMachineId)
                && _runtime.Machines.TryGetValue(_selectedMachineId, out var selectedMachine))
            {
                MainShell.CurrentMachineDisplayName = selectedMachine.Machine.Name;
            }

            return;
        }

        if (!string.Equals(target, ShellNavigationTargets.Detail, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var targetMachine = !string.IsNullOrWhiteSpace(_selectedMachineId)
            ? _selectedMachineId
            : _runtime.Machines.Keys.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(targetMachine))
        {
            NavigateToMachineDetail(targetMachine);
        }
    }

    private void NavigateToMachineDetail(string machineId)
    {
        if (_runtime is null || !_runtime.Machines.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;
        var detailMeta = _runtime.Meta.DetailPage;
        var detailPage = new MachineDetailPage
        {
            MachineTitle = config.Machine.Name,
            BatchNumber = ShellOverviewBinder.GetTagAddress(config, detailMeta.BatchTagSection, detailMeta.BatchTagKey),
            RecipeName = ShellOverviewBinder.GetTagAddress(config, detailMeta.RecipeTagSection, detailMeta.RecipeTagKey),
            MachineState = ShellOverviewBinder.GetTagAddress(config, detailMeta.MachineStateTagSection, detailMeta.MachineStateTagKey),
            AlarmState = ShellOverviewBinder.GetTagAddress(config, detailMeta.AlarmStateTagSection, detailMeta.AlarmStateTagKey),
            NozzleTempText = ShellOverviewBinder.GetTagAddress(config, detailMeta.NozzleTempTagSection, detailMeta.NozzleTempTagKey),
            AlarmConfigFile = _runtime.GetAlarmConfigFile(config.Machine.Id),
            SensorConfigFile = _runtime.GetSensorConfigFile(config.Machine.Id)
        };

        MainShell.ShellContent = detailPage;
        MainShell.CurrentMachineDisplayName = config.Machine.Name;
        MainShell.PageTitle = string.IsNullOrWhiteSpace(detailMeta.PageTitle)
            ? ShellRouteCatalog.DefaultTitles[ShellNavigationTargets.Detail]
            : detailMeta.PageTitle;
    }
}
