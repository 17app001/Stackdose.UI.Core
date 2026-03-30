using Stackdose.App.DeviceFramework.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Stackdose.App.DeviceFramework.ViewModels;

/// <summary>
/// �q�γ]�ƭ��� ViewModel �X ���A�� 14 �ӵw�s�X�ݩʡA
/// ��� Labels �r�� + Commands �r��ʺA�X�ʡC
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
    private bool _showLiveLog = false;
    private string _layoutMode = "SplitRight";

    public DevicePageViewModel()
    {
        ExecuteCommandCommand = new RelayCommand(
            param => OnCommandExecuted(param as string ?? string.Empty),
            param => param is string cmd && !string.IsNullOrWhiteSpace(cmd));
    }

    // �w�w �֤��ݩ� �w�w

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
        set { SetProperty(ref _alarmConfigFile, value); OnPropertyChanged(nameof(HasAlarmConfig)); OnPropertyChanged(nameof(HasAnyViewer)); }
    }

    public string SensorConfigFile
    {
        get => _sensorConfigFile;
        set { SetProperty(ref _sensorConfigFile, value); OnPropertyChanged(nameof(HasSensorConfig)); OnPropertyChanged(nameof(HasAnyViewer)); }
    }

    public bool HasAlarmConfig  => !string.IsNullOrEmpty(_alarmConfigFile);
    public bool HasSensorConfig => !string.IsNullOrEmpty(_sensorConfigFile);
    public bool HasAnyViewer    => HasAlarmConfig || HasSensorConfig;

    public string LayoutMode
    {
        get => _layoutMode;
        set => SetProperty(ref _layoutMode, value);
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

    public bool ShowLiveLog
    {
        get => _showLiveLog;
        set => SetProperty(ref _showLiveLog, value);
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

    // �w�w �ʺA���� �w�w

    /// <summary>
    /// �ʺA���Ҷ��X�AUI �z�L ItemsControl ô�������X�۰ʲ��� PlcLabel�C
    /// </summary>
    public ObservableCollection<DeviceLabelViewModel> Labels { get; } = [];

    // �w�w �ʺA�R�O �w�w

    /// <summary>
    /// �R�O���X�AUI �z�L ItemsControl ô�������X�۰ʲ��ͫ��s�C
    /// </summary>
    public ObservableCollection<DeviceCommandViewModel> Commands { get; } = [];

    /// <summary>
    /// PrintHead �]�w�ɸ��|�C
    /// </summary>
    public ObservableCollection<string> PrintHeadConfigFiles { get; } = [];

    /// <summary>
    /// �ҥΪ��\��ҲզW�١C
    /// </summary>
    public ObservableCollection<string> EnabledModules { get; } = [];

    /// <summary>
    /// �Τ@�R�O����J�f�]�ѫ��s CommandParameter �ǤJ�R�O�W�١^�C
    /// </summary>
    public ICommand ExecuteCommandCommand { get; }

    /// <summary>
    /// �R�O�����Ĳ�o�]�l���O�Υ~���i�мg/�q�\�^�C
    /// </summary>
    public event Action<string>? CommandExecuted;

    // �w�w �M�� DeviceContext �w�w

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
        ShowLiveLog = context.ShowLiveLog;
        LayoutMode = context.LayoutMode;
        CurrentProcessState = ProcessState.Idle;

        // �ʺA����
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

            // �p�G�O ElapsedTime�A�P�B��M���ݩ�
            if (name.Equals("Elapsed Time", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ElapsedTime", StringComparison.OrdinalIgnoreCase))
            {
                ElapsedTimeAddress = info.Address;
            }
        }

        // �ʺA�R�O
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

    // �w�w �s�{���A �w�w

    public void MarkProcessRunning() => CurrentProcessState = ProcessState.Running;
    public void MarkProcessCompleted() => CurrentProcessState = ProcessState.Completed;
    public void MarkProcessFaulted() => CurrentProcessState = ProcessState.Faulted;
    public void MarkProcessStopped() => CurrentProcessState = ProcessState.Stopped;

    // �w�w ���� �w�w

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
        // camelCase / PascalCase �� "Start Process"
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        result = System.Text.RegularExpressions.Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result);
    }
}

/// <summary>
/// ��@�ʺA���Ҫ� ViewModel�C
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
/// ��@�ʺA�R�O�� ViewModel�C
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
