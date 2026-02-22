using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Pages;
using System;
using System.Linq;
using System.Windows;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private UbiRuntimeContext? _runtime;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly SettingsPage _settingsPage = new();
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
        _runtime = UbiRuntimeHost.Start(MainShell);
        if (_runtime is null)
        {
            return;
        }

        SecurityContext.QuickLogin(AccessLevel.SuperAdmin);

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
        NavigateToDevicePage(machineId);
    }

    private void OnNavigationRequested(object? sender, string target)
    {
        if (_runtime is null)
        {
            return;
        }

        if (string.Equals(target, "MachineOverviewPage", StringComparison.OrdinalIgnoreCase))
        {
            MainShell.ShellContent = _runtime.OverviewPage;
            MainShell.PageTitle = _defaultPageTitle;
            if (!string.IsNullOrWhiteSpace(_selectedMachineId) && _runtime.Machines.TryGetValue(_selectedMachineId, out var selectedMachine))
            {
                MainShell.CurrentMachineDisplayName = selectedMachine.Machine.Name;
            }

            return;
        }

        if (string.Equals(target, "MachineDetailPage", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(_selectedMachineId) && _runtime.Machines.ContainsKey(_selectedMachineId))
            {
                NavigateToDevicePage(_selectedMachineId);
                return;
            }

            var firstMachineId = _runtime.Machines.Keys.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstMachineId))
            {
                NavigateToDevicePage(firstMachineId);
            }

            return;
        }

        if (string.Equals(target, "LogViewerPage", StringComparison.OrdinalIgnoreCase))
        {
            MainShell.ShellContent = _logViewerPage;
            MainShell.PageTitle = "Log Viewer";
            return;
        }

        if (string.Equals(target, "UserManagementPage", StringComparison.OrdinalIgnoreCase))
        {
            MainShell.ShellContent = _userManagementPage;
            MainShell.PageTitle = "User Management";
            return;
        }

        if (string.Equals(target, "SettingsPage", StringComparison.OrdinalIgnoreCase))
        {
            MainShell.ShellContent = _settingsPage;
            MainShell.PageTitle = "Settings";
        }
    }

    private void NavigateToDevicePage(string machineId)
    {
        if (_runtime is null || !_runtime.Machines.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;
        var devicePage = new UbiDevicePage();
        var printHeadConfigs = UbiRuntimeMapper.GetPrintHeadConfigFiles(config);
        devicePage.SetDeviceContext(new DeviceContext
        {
            MachineId = config.Machine.Id,
            MachineName = config.Machine.Name,
            BatchAddress = UbiRuntimeMapper.GetTagAddress(config, "process", "batchNo"),
            RecipeAddress = UbiRuntimeMapper.GetTagAddress(config, "process", "recipeNo"),
            NozzleAddress = UbiRuntimeMapper.GetTagAddress(config, "process", "nozzleTemp"),
            RunningAddress = UbiRuntimeMapper.GetTagAddress(config, "status", "isRunning"),
            AlarmAddress = UbiRuntimeMapper.GetTagAddress(config, "status", "isAlarm"),
            AlarmConfigFile = UbiRuntimeMapper.GetAlarmConfigFile(config.Machine.Id),
            SensorConfigFile = UbiRuntimeMapper.GetSensorConfigFile(config.Machine.Id),
            PrintHead1ConfigFile = printHeadConfigs.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = printHeadConfigs.ElementAtOrDefault(1) ?? string.Empty
        });

        MainShell.ShellContent = devicePage;
        MainShell.CurrentMachineDisplayName = config.Machine.Name;
        MainShell.PageTitle = "Machine Detail";
    }
}
