using Stackdose.App.DeviceFramework.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Stackdose.App.DeviceFramework.ViewModels;

/// <summary>
/// 通用設備頁面 ViewModel — 不再有 14 個硬編碼屬性，
/// 改用 Labels 字典 + Commands 字典動態驅動。
/// </summary>
public class DevicePageViewModel : ViewModelBase
{
    private string _machineId = string.Empty;
    private string _machineName = "Machine";
    private string _runningAddress = "--";
    private string _completedAddress = "--";
    private string _alarmAddress = "--";
    private string _alarmConfigFile = string.Empty;
    private string _sensorConfigFile = string.Empty;
    private ProcessState _currentProcessState = ProcessState.Idle;
    private string _currentProcessStateText = "Idle";
    private string _elapsedTimeAddress = "--";
    private bool _showPlcEditor = false;

    public DevicePageViewModel()
    {
        ExecuteCommandCommand = new RelayCommand(
            param => OnCommandExecuted(param as string ?? string.Empty),
            param => param is string cmd && !string.IsNullOrWhiteSpace(cmd));
    }

    // ── 核心屬性 ──

    public string MachineId
    {
        get => _machineId;
        set => SetProperty(ref _machineId, value);
    }

    public string MachineName
    {
        get => _machineName;
        set => SetProperty(ref _machineName, value);
    }

    public string RunningAddress
    {
        get => _runningAddress;
        set => SetProperty(ref _runningAddress, value);
    }

    public string CompletedAddress
    {
        get => _completedAddress;
        set => SetProperty(ref _completedAddress, value);
    }

    public string AlarmAddress
    {
        get => _alarmAddress;
        set => SetProperty(ref _alarmAddress, value);
    }

    public string AlarmConfigFile
    {
        get => _alarmConfigFile;
        set => SetProperty(ref _alarmConfigFile, value);
    }

    public string SensorConfigFile
    {
        get => _sensorConfigFile;
        set => SetProperty(ref _sensorConfigFile, value);
    }

    public string ElapsedTimeAddress
    {
        get => _elapsedTimeAddress;
        set => SetProperty(ref _elapsedTimeAddress, value);
    }

    public bool ShowPlcEditor
    {
        get => _showPlcEditor;
        set => SetProperty(ref _showPlcEditor, value);
    }

    public ProcessState CurrentProcessState
    {
        get => _currentProcessState;
        set
        {
            SetProperty(ref _currentProcessState, value);
            CurrentProcessStateText = value.ToString();
        }
    }

    public string CurrentProcessStateText
    {
        get => _currentProcessStateText;
        private set => SetProperty(ref _currentProcessStateText, value);
    }

    // ── 動態標籤 ──

    /// <summary>
    /// 動態標籤集合，UI 透過 ItemsControl 繫結此集合自動產生 PlcLabel。
    /// </summary>
    public ObservableCollection<DeviceLabelViewModel> Labels { get; } = [];

    // ── 動態命令 ──

    /// <summary>
    /// 命令集合，UI 透過 ItemsControl 繫結此集合自動產生按鈕。
    /// </summary>
    public ObservableCollection<DeviceCommandViewModel> Commands { get; } = [];

    /// <summary>
    /// PrintHead 設定檔路徑。
    /// </summary>
    public ObservableCollection<string> PrintHeadConfigFiles { get; } = [];

    /// <summary>
    /// 啟用的功能模組名稱。
    /// </summary>
    public ObservableCollection<string> EnabledModules { get; } = [];

    /// <summary>
    /// 統一命令執行入口（由按鈕 CommandParameter 傳入命令名稱）。
    /// </summary>
    public ICommand ExecuteCommandCommand { get; }

    /// <summary>
    /// 命令執行時觸發（子類別或外部可覆寫/訂閱）。
    /// </summary>
    public event Action<string>? CommandExecuted;

    // ── 套用 DeviceContext ──

    public virtual void ApplyDeviceContext(DeviceContext context)
    {
        MachineId = context.MachineId;
        MachineName = context.MachineName;
        RunningAddress = context.RunningAddress;
        CompletedAddress = context.CompletedAddress;
        AlarmAddress = context.AlarmAddress;
        AlarmConfigFile = context.AlarmConfigFile;
        SensorConfigFile = context.SensorConfigFile;
        ShowPlcEditor = context.ShowPlcEditor;
        CurrentProcessState = ProcessState.Idle;

        // 動態標籤
        Labels.Clear();
        foreach (var (name, info) in context.Labels)
        {
            Labels.Add(new DeviceLabelViewModel
            {
                Label = name,
                Address = info.Address,
                DefaultValue = info.DefaultValue,
                DataType = info.DataType,
                Divisor = info.Divisor,
                StringFormat = info.StringFormat
            });

            // 如果是 ElapsedTime，同步到專屬屬性
            if (name.Equals("Elapsed Time", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ElapsedTime", StringComparison.OrdinalIgnoreCase))
            {
                ElapsedTimeAddress = info.Address;
            }
        }

        // 動態命令
        Commands.Clear();
        foreach (var (name, address) in context.Commands)
        {
            var theme = InferCommandTheme(name);
            Commands.Add(new DeviceCommandViewModel
            {
                Name = name,
                DisplayName = FormatCommandDisplayName(name),
                Address = address,
                Theme = theme
            });
        }

        // PrintHead
        PrintHeadConfigFiles.Clear();
        foreach (var file in context.PrintHeadConfigFiles)
        {
            PrintHeadConfigFiles.Add(file);
        }

        // Modules
        EnabledModules.Clear();
        foreach (var module in context.EnabledModules)
        {
            EnabledModules.Add(module);
        }
    }

    // ── 製程狀態 ──

    public void MarkProcessRunning() => CurrentProcessState = ProcessState.Running;
    public void MarkProcessCompleted() => CurrentProcessState = ProcessState.Completed;
    public void MarkProcessFaulted() => CurrentProcessState = ProcessState.Faulted;
    public void MarkProcessStopped() => CurrentProcessState = ProcessState.Stopped;

    // ── 內部 ──

    protected virtual void OnCommandExecuted(string commandName)
    {
        CommandExecuted?.Invoke(commandName);
    }

    private static string InferCommandTheme(string commandName)
    {
        var lower = commandName.ToLowerInvariant();
        if (lower.Contains("start")) return "Success";
        if (lower.Contains("stop") || lower.Contains("emergency")) return "Error";
        if (lower.Contains("pause") || lower.Contains("spit") || lower.Contains("clean") || lower.Contains("warning")) return "Warning";
        if (lower.Contains("init") || lower.Contains("reset") || lower.Contains("grating")) return "Primary";
        return "Info";
    }

    private static string FormatCommandDisplayName(string name)
    {
        // camelCase / PascalCase → "Start Process"
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        result = System.Text.RegularExpressions.Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result);
    }
}

/// <summary>
/// 單一動態標籤的 ViewModel。
/// </summary>
public sealed class DeviceLabelViewModel : ViewModelBase
{
    private string _label = string.Empty;
    private string _address = string.Empty;
    private string _defaultValue = "0";
    private string _dataType = "Word";
    private int _divisor = 1;
    private string _stringFormat = string.Empty;

    public string Label { get => _label; set => SetProperty(ref _label, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string DefaultValue { get => _defaultValue; set => SetProperty(ref _defaultValue, value); }
    public string DataType { get => _dataType; set => SetProperty(ref _dataType, value); }
    public int Divisor { get => _divisor; set => SetProperty(ref _divisor, value); }
    public string StringFormat { get => _stringFormat; set => SetProperty(ref _stringFormat, value); }
}

/// <summary>
/// 單一動態命令的 ViewModel。
/// </summary>
public sealed class DeviceCommandViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _displayName = string.Empty;
    private string _address = string.Empty;
    private string _theme = "Primary";

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string Theme { get => _theme; set => SetProperty(ref _theme, value); }
}
