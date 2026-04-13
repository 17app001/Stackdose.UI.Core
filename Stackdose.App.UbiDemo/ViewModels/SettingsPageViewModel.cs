using System;
using System.Linq;
using System.Collections.ObjectModel;
using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.ViewModels;

namespace Stackdose.App.UbiDemo.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    // ── Global fields (不隨機台切換) ──
    private string _plcIpAddress = "192.168.22.39";
    private string _plcPort = "3000";
    private string _scanIntervalMs = "150";
    private string _monitorMap = "M200,3,D300,2,D400,10";
    private string _configRootPath = @"Config";
    private string _registeredMonitorCount = "0";

    // ── Per-Machine fields (隨機台切換) ──
    private string _machineConfigPath = @"Config\Machine*.config.json";
    private string _alarmConfigPath = string.Empty;
    private string _sensorConfigPath = string.Empty;
    private string _headProfilePath = string.Empty;
    private string _wavePath = @"Config\waves";
    private string _machineIpAddress = string.Empty;

    private string? _selectedMachineId;
    private Dictionary<string, MachineConfig> _machineConfigs = new(StringComparer.OrdinalIgnoreCase);

    // ── Global collections ──
    public ObservableCollection<string> RegisteredMonitorDeviceItems { get; } = [];

    /// <summary>
    /// 可選擇的機台清單
    /// </summary>
    public ObservableCollection<MachineOption> MachineOptions { get; } = [];

    #region Machine Selector

    /// <summary>
    /// 目前選擇的機台 ID
    /// </summary>
    public string? SelectedMachineId
    {
        get => _selectedMachineId;
        set
        {
            if (string.Equals(_selectedMachineId, value, StringComparison.Ordinal))
                return;

            SetProperty(ref _selectedMachineId, value);
            OnSelectedMachineChanged();
        }
    }

    #endregion

    #region Global Properties

    public string PlcIpAddress
    {
        get => _plcIpAddress;
        set => SetProperty(ref _plcIpAddress, value);
    }

    public string PlcPort
    {
        get => _plcPort;
        set => SetProperty(ref _plcPort, value);
    }

    public string ScanIntervalMs
    {
        get => _scanIntervalMs;
        set => SetProperty(ref _scanIntervalMs, value);
    }

    public string MonitorMap
    {
        get => _monitorMap;
        set => SetProperty(ref _monitorMap, value);
    }

    public string ConfigRootPath
    {
        get => _configRootPath;
        set => SetProperty(ref _configRootPath, value);
    }

    public string RegisteredMonitorCount
    {
        get => _registeredMonitorCount;
        set => SetProperty(ref _registeredMonitorCount, value);
    }

    #endregion

    #region Per-Machine Properties

    public string MachineConfigPath
    {
        get => _machineConfigPath;
        set => SetProperty(ref _machineConfigPath, value);
    }

    public string AlarmConfigPath
    {
        get => _alarmConfigPath;
        set => SetProperty(ref _alarmConfigPath, value);
    }

    public string SensorConfigPath
    {
        get => _sensorConfigPath;
        set => SetProperty(ref _sensorConfigPath, value);
    }

    public string HeadProfilePath
    {
        get => _headProfilePath;
        set => SetProperty(ref _headProfilePath, value);
    }

    public string WavePath
    {
        get => _wavePath;
        set => SetProperty(ref _wavePath, value);
    }

    /// <summary>
    /// 目前選取機台的 PLC IP（唯讀顯示）
    /// </summary>
    public string MachineIpAddress
    {
        get => _machineIpAddress;
        set => SetProperty(ref _machineIpAddress, value);
    }

    #endregion

    #region Global Actions

    public void ApplyMonitorAddresses(string monitorAddresses)
    {
        MonitorMap = monitorAddresses;

        var tokens = monitorAddresses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var lines = new List<string>();
        var blockCount = 0;
        for (var i = 0; i < tokens.Length; i++)
        {
            var address = tokens[i];
            if (int.TryParse(address, out _))
            {
                continue;
            }

            var length = 1;
            if (i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out var parsedLength) && parsedLength > 0)
            {
                length = parsedLength;
                i++;
            }

            lines.Add($"{address} (x{length})");
            blockCount++;
        }

        RegisteredMonitorCount = blockCount.ToString();

        RegisteredMonitorDeviceItems.Clear();
        foreach (var line in lines)
        {
            RegisteredMonitorDeviceItems.Add(line);
        }
    }

    /// <summary>
    /// 設定全域 PLC 連線參數（只在初始化時呼叫一次）
    /// </summary>
    public void ApplyGlobalPlcSettings(MachineConfig config, string configRootPath)
    {
        ConfigRootPath = string.IsNullOrWhiteSpace(configRootPath) ? @"Config" : configRootPath;
        PlcIpAddress = config.Plc.Ip;
        PlcPort = config.Plc.Port.ToString();
        ScanIntervalMs = config.Plc.PollIntervalMs.ToString();
    }

    #endregion

    #region Machine Actions

    /// <summary>
    /// 載入所有機台清單，建立選擇器選項
    /// </summary>
    public void ApplyMachines(IReadOnlyDictionary<string, MachineConfig> machines, string configRootPath, string? defaultMachineId)
    {
        _machineConfigs = new Dictionary<string, MachineConfig>(machines, StringComparer.OrdinalIgnoreCase);
        ConfigRootPath = string.IsNullOrWhiteSpace(configRootPath) ? @"Config" : configRootPath;

        MachineOptions.Clear();
        foreach (var kvp in machines)
        {
            MachineOptions.Add(new MachineOption(kvp.Key, kvp.Value.Machine.Name));
        }

        var initialId = !string.IsNullOrWhiteSpace(defaultMachineId) && machines.ContainsKey(defaultMachineId)
            ? defaultMachineId
            : machines.Keys.FirstOrDefault();

        if (initialId != null && machines.TryGetValue(initialId, out var initialConfig))
        {
            ApplyGlobalPlcSettings(initialConfig, configRootPath);
        }

        SelectedMachineId = initialId;
    }

    /// <summary>
    /// 套用單機 Per-Machine 設定
    /// </summary>
    private void ApplyMachineSpecificConfig(MachineConfig config)
    {
        MachineConfigPath = @"Config\Machine*.config.json";
        AlarmConfigPath = config.AlarmConfigFile ?? string.Empty;
        SensorConfigPath = config.SensorConfigFile ?? string.Empty;
        HeadProfilePath = config.PrintHeadConfigs.FirstOrDefault() ?? string.Empty;
        MachineIpAddress = $"{config.Plc.Ip}:{config.Plc.Port}";
    }

    private void OnSelectedMachineChanged()
    {
        if (string.IsNullOrWhiteSpace(_selectedMachineId)) return;
        if (!_machineConfigs.TryGetValue(_selectedMachineId, out var config)) return;

        ApplyMachineSpecificConfig(config);
    }

    #endregion

    #region Backward Compatibility

    /// <summary>
    /// 保留向下相容（外部如果還有呼叫此方法）
    /// </summary>
    public void ApplyMachineConfig(MachineConfig config, string configRootPath)
    {
        ApplyGlobalPlcSettings(config, configRootPath);
        ApplyMachineSpecificConfig(config);
    }

    #endregion
}

/// <summary>
/// 機台選擇器選項
/// </summary>
public sealed class MachineOption
{
    public string MachineId { get; }
    public string DisplayName { get; }

    public MachineOption(string machineId, string displayName)
    {
        MachineId = machineId;
        DisplayName = $"{displayName} ({machineId})";
    }

    public override string ToString() => DisplayName;
}
