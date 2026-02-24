using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private sealed record AlarmBitPoint(string Device, int Bit);
    private sealed record OverviewAddressMap(string BatchAddress, string RecipeAddress, string NozzleAddress, string RunningAddress, string AlarmAddress);

    private UbiRuntimeContext? _runtime;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly SettingsPage _settingsPage = new();
    private readonly Dictionary<string, UbiDevicePage> _devicePages = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultPageTitle = "Machine Overview";
    private string? _selectedMachineId;
    private readonly Dictionary<string, OverviewAddressMap> _machineOverviewAddressMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<AlarmBitPoint>> _machineAlarmMap = new(StringComparer.OrdinalIgnoreCase);

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
        _runtime.OverviewPage.PlcScanUpdated += OnPlcScanUpdated;
        MainShell.NavigationRequested += OnNavigationRequested;

        BuildOverviewAddressMap();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.OverviewPage.MachineSelected -= OnMachineSelected;
            _runtime.OverviewPage.PlcScanUpdated -= OnPlcScanUpdated;
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
            _settingsPage.SetMonitorAddresses(_runtime.OverviewPage.PlcMonitorAddresses);
            MainShell.ShellContent = _settingsPage;
            MainShell.PageTitle = "Maintenance Mode";
        }
    }

    private void NavigateToDevicePage(string machineId)
    {
        if (_runtime is null || !_runtime.Machines.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;
        if (!_devicePages.TryGetValue(machineId, out var devicePage))
        {
            devicePage = new UbiDevicePage();
            _devicePages[machineId] = devicePage;
        }

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
            AlarmConfigFile = UbiRuntimeMapper.GetAlarmConfigFile(config),
            SensorConfigFile = UbiRuntimeMapper.GetSensorConfigFile(config),
            PrintHead1ConfigFile = printHeadConfigs.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = printHeadConfigs.ElementAtOrDefault(1) ?? string.Empty,

            TotalTrayAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "totalTray", "D3400"),
            CurrentTrayAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "currentTray", "D33"),
            TotalLayerAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "totalLayer", "D3401"),
            CurrentLayerAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "currentLayer", "D32"),
            SwitchGraphicLayerAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "switchGraphicLayer", "D510"),
            SwitchAreaLayerAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "switchAreaLayer", "D512"),
            MessageIdAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "messageId", "D85"),
            BatteryAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "battery", "D120"),
            ElapsedTimeAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "elapsedTime", "D86"),
            PrintHeadCountAddress = UbiRuntimeMapper.GetDetailLabelAddress(config, "printHeadCount", "D87")
        });

        MainShell.ShellContent = devicePage;
        MainShell.CurrentMachineDisplayName = config.Machine.Name;
        MainShell.PageTitle = "Machine Detail";
    }

    private void BuildOverviewAddressMap()
    {
        _machineOverviewAddressMap.Clear();
        _machineAlarmMap.Clear();
        if (_runtime is null)
        {
            return;
        }

        foreach (var pair in _runtime.Machines)
        {
            var config = pair.Value;
            _machineOverviewAddressMap[pair.Key] = new OverviewAddressMap(
                UbiRuntimeMapper.GetTagAddress(config, "process", "batchNo"),
                UbiRuntimeMapper.GetTagAddress(config, "process", "recipeNo"),
                UbiRuntimeMapper.GetTagAddress(config, "process", "nozzleTemp"),
                UbiRuntimeMapper.GetTagAddress(config, "status", "isRunning"),
                UbiRuntimeMapper.GetTagAddress(config, "status", "isAlarm"));

            _machineAlarmMap[pair.Key] = LoadAlarmBitPoints(UbiRuntimeMapper.GetAlarmConfigFile(config));
        }
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        if (_runtime?.OverviewPage.MachineCards is null || _machineOverviewAddressMap.Count == 0)
        {
            return;
        }

        foreach (var card in _runtime.OverviewPage.MachineCards)
        {
            if (!_machineOverviewAddressMap.TryGetValue(card.MachineId, out var map))
            {
                continue;
            }

            var isRunning = ReadBoolAddress(manager, map.RunningAddress);
            var isAlarm = ReadBoolAddress(manager, map.AlarmAddress);
            var alarmCount = GetActiveAlarmCount(manager, card.MachineId);

            card.StatusText = isRunning ? "Running" : "Idle";
            card.StatusBrush = isAlarm
                ? System.Windows.Media.Brushes.OrangeRed
                : isRunning
                    ? System.Windows.Media.Brushes.LimeGreen
                    : System.Windows.Media.Brushes.SlateGray;

            card.LeftBottomLabel = "Alarm";
            card.LeftBottomValue = alarmCount.ToString();
            card.RightTopLabel = "Nozzle";
            card.RightTopValue = ReadWordText(manager, map.NozzleAddress);
            card.BatchValue = ReadWordText(manager, map.BatchAddress);
            card.RecipeText = ReadWordText(manager, map.RecipeAddress);
        }
    }

    private static bool ReadBoolAddress(IPlcManager manager, string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address == "--")
        {
            return false;
        }

        if (address.Contains('.', StringComparison.Ordinal))
        {
            var parts = address.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[1], out var bitIndex))
            {
                return manager.Monitor?.GetBit(parts[0], bitIndex) ?? false;
            }
        }

        return manager.ReadBit(address) ?? false;
    }

    private static string ReadWordText(IPlcManager manager, string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address == "--")
        {
            return "--";
        }

        var value = manager.ReadWord(address);
        return value?.ToString() ?? "--";
    }

    private int GetActiveAlarmCount(IPlcManager manager, string machineId)
    {
        if (!_machineAlarmMap.TryGetValue(machineId, out var alarmPoints) || alarmPoints.Count == 0)
        {
            return 0;
        }

        var active = 0;
        foreach (var point in alarmPoints)
        {
            var word = manager.ReadWord(point.Device);
            if (word.HasValue && ((word.Value >> point.Bit) & 1) == 1)
            {
                active++;
            }
        }

        return active;
    }

    private static IReadOnlyList<AlarmBitPoint> LoadAlarmBitPoints(string alarmConfigPath)
    {
        if (string.IsNullOrWhiteSpace(alarmConfigPath) || !File.Exists(alarmConfigPath))
        {
            return [];
        }

        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(alarmConfigPath));
            if (!json.RootElement.TryGetProperty("Alarms", out var alarms) || alarms.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var points = new List<AlarmBitPoint>();
            foreach (var item in alarms.EnumerateArray())
            {
                if (!item.TryGetProperty("Device", out var deviceProp) || deviceProp.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (!item.TryGetProperty("Bit", out var bitProp) || !bitProp.TryGetInt32(out var bit))
                {
                    continue;
                }

                var device = deviceProp.GetString();
                if (string.IsNullOrWhiteSpace(device))
                {
                    continue;
                }

                points.Add(new AlarmBitPoint(device.Trim().ToUpperInvariant(), bit));
            }

            return points;
        }
        catch
        {
            return [];
        }
    }
}
