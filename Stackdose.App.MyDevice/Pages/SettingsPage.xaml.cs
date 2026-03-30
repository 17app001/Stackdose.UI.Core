using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Stackdose.App.MyDevice.Pages;

public partial class SettingsPage : UserControl, INotifyPropertyChanged
{
    private string _plcIpAddress = string.Empty;
    private string _plcPort = string.Empty;
    private string _monitorMap = string.Empty;
    private string _configRootPath = string.Empty;
    private string _selectedMachineId = string.Empty;
    private string _machineConfigPath = string.Empty;
    private string _alarmConfigPath = string.Empty;
    private string _machineIpAddress = string.Empty;

    public SettingsPage() { InitializeComponent(); DataContext = this; }

    public string PlcIpAddress { get => _plcIpAddress; set { _plcIpAddress = value; N(); } }
    public string PlcPort { get => _plcPort; set { _plcPort = value; N(); } }
    public string MonitorMap { get => _monitorMap; set { _monitorMap = value; N(); } }
    public string ConfigRootPath { get => _configRootPath; set { _configRootPath = value; N(); } }
    public string SelectedMachineId { get => _selectedMachineId; set { _selectedMachineId = value; N(); } }
    public string MachineConfigPath { get => _machineConfigPath; set { _machineConfigPath = value; N(); } }
    public string AlarmConfigPath { get => _alarmConfigPath; set { _alarmConfigPath = value; N(); } }
    public string MachineIpAddress { get => _machineIpAddress; set { _machineIpAddress = value; N(); } }
    public ObservableCollection<MachineOption> MachineOptions { get; } = [];
    public ObservableCollection<string> RegisteredMonitorDeviceItems { get; } = [];

    public void SetMonitorAddresses(string monitorAddresses) => MonitorMap = monitorAddresses;

    public void SetMachines(IReadOnlyDictionary<string, MachineConfig> machines, string configRootPath, string? defaultMachineId)
    {
        ConfigRootPath = configRootPath;
        MachineOptions.Clear();
        foreach (var (id, cfg) in machines)
            MachineOptions.Add(new MachineOption(id, cfg.Machine.Name));
        if (!string.IsNullOrWhiteSpace(defaultMachineId)) SelectedMachineId = defaultMachineId;
        else if (MachineOptions.Count > 0) SelectedMachineId = MachineOptions[0].MachineId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public record MachineOption(string MachineId, string DisplayName);
