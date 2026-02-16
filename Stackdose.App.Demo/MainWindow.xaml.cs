using Stackdose.App.Demo.Services;
using Stackdose.UI.Templates.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Stackdose.App.Demo;

public partial class MainWindow : Window
{
    private DemoRuntimeContext? _runtime;
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
        _runtime = DemoRuntimeHost.Start(MainShell);
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

        if (!string.Equals(target, "MachineDetailPage", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_selectedMachineId) && _runtime.Machines.ContainsKey(_selectedMachineId))
        {
            NavigateToMachineDetail(_selectedMachineId);
            return;
        }

        var firstMachineId = _runtime.Machines.Keys.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstMachineId))
        {
            NavigateToMachineDetail(firstMachineId);
        }
    }

    private void NavigateToMachineDetail(string machineId)
    {
        if (_runtime is null || !_runtime.Machines.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;

        var detailPage = new MachineDetailPage
        {
            MachineTitle = config.Machine.Name,
            BatchNumber = GetTagAddress(config, "process", "batchNo"),
            RecipeName = GetTagAddress(config, "process", "recipeNo"),
            MachineState = GetTagAddress(config, "status", "isRunning"),
            AlarmState = GetTagAddress(config, "status", "isAlarm"),
            NozzleTempText = GetTagAddress(config, "process", "nozzleTemp"),
            AlarmConfigFile = GetAlarmConfigFile(config.Machine.Id),
            SensorConfigFile = GetSensorConfigFile(config.Machine.Id)
        };

        MainShell.ShellContent = detailPage;
        MainShell.CurrentMachineDisplayName = config.Machine.Name;
        MainShell.PageTitle = "Machine Detail";
    }

    private static string GetTagAddress(Models.DemoMachineConfig config, string section, string key)
    {
        Dictionary<string, Models.TagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status" => config.Tags.Status,
            "process" => config.Tags.Process,
            _ => null
        };

        if (tags is null || !tags.TryGetValue(key, out var tag) || string.IsNullOrWhiteSpace(tag.Address))
        {
            return "--";
        }

        return tag.Address;
    }

    private static string GetAlarmConfigFile(string machineId)
    {
        return string.Equals(machineId, "M1", StringComparison.OrdinalIgnoreCase)
            ? "Resources/Alarm/MachineA.alarms.json"
            : string.Empty;
    }

    private static string GetSensorConfigFile(string machineId)
    {
        return string.Equals(machineId, "M1", StringComparison.OrdinalIgnoreCase)
            ? "Sensor/MachineA.sensors.json"
            : string.Empty;
    }
}
