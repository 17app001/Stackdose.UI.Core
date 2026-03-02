using System;
using System.Linq;
using System.Collections.ObjectModel;
using Stackdose.App.UbiDemo.Models;

namespace Stackdose.App.UbiDemo.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private string _machineConfigPath = @"Config\Machine*.config.json";
    private string _alarmConfigPath = string.Empty;
    private string _sensorConfigPath = string.Empty;
    private string _headProfilePath = string.Empty;
    private string _wavePath = @"Config\waves";
    private string _plcIpAddress = "192.168.22.39";
    private string _plcPort = "3000";
    private string _scanIntervalMs = "150";
    private string _monitorMap = "M200,3,D300,2,D400,10";
    private string _configRootPath = @"Config";
    private string _registeredMonitorCount = "0";
    public ObservableCollection<string> RegisteredMonitorDeviceItems { get; } = [];

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

    public string RegisteredMonitorCount
    {
        get => _registeredMonitorCount;
        set => SetProperty(ref _registeredMonitorCount, value);
    }

    public string ConfigRootPath
    {
        get => _configRootPath;
        set => SetProperty(ref _configRootPath, value);
    }

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

    public void ApplyMachineConfig(UbiMachineConfig config, string configRootPath)
    {
        ConfigRootPath = string.IsNullOrWhiteSpace(configRootPath) ? @"Config" : configRootPath;
        MachineConfigPath = @"Config\Machine*.config.json";
        AlarmConfigPath = config.AlarmConfigFile ?? string.Empty;
        SensorConfigPath = config.SensorConfigFile ?? string.Empty;
        HeadProfilePath = config.PrintHeadConfigs.FirstOrDefault() ?? string.Empty;
        PlcIpAddress = config.Plc.Ip;
        PlcPort = config.Plc.Port.ToString();
        ScanIntervalMs = config.Plc.PollIntervalMs.ToString();
    }
}
