using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stackdose.Tools.ProjectGeneratorUI.ViewModels;

public sealed class DataEventRow : INotifyPropertyChanged
{
    private string _name      = string.Empty;
    private string _address   = string.Empty;
    private string _trigger   = "changed";
    private int    _threshold = 0;
    private string _dataType  = string.Empty;

    public string Name      { get => _name;      set { _name      = value; N(); } }
    public string Address   { get => _address;   set { _address   = value; N(); } }
    public string Trigger   { get => _trigger;   set { _trigger   = value; N(); } }
    public int    Threshold { get => _threshold; set { _threshold = value; N(); } }
    public string DataType  { get => _dataType;  set { _dataType  = value; N(); } }

    public string[] TriggerOptions { get; } = ["risingEdge", "fallingEdge", "above", "below", "equals", "changed"];
    public string[] DataTypeOptions { get; } = ["", "bit", "word"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}

public sealed class CommandRow : INotifyPropertyChanged
{
    private string _name    = string.Empty;
    private string _address = string.Empty;
    private string _theme   = string.Empty;   // 空字串 = 自動推斷

    public string Name    { get => _name;    set { _name    = value; N(); } }
    public string Address { get => _address; set { _address = value; N(); } }
    public string Theme   { get => _theme;   set { _theme   = value; N(); } }

    public static string[] ThemeOptions { get; } = ["", "Primary", "Success", "Error", "Warning", "Info"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}

public sealed class LabelRow : INotifyPropertyChanged
{
    private string _name            = string.Empty;
    private string _address         = string.Empty;
    private string _frameShape      = "Rectangle";
    private string _valueColorTheme = "NeonBlue";

    public string Name            { get => _name;            set { _name            = value; N(); } }
    public string Address         { get => _address;         set { _address         = value; N(); } }
    public string FrameShape      { get => _frameShape;      set { _frameShape      = value; N(); } }
    public string ValueColorTheme { get => _valueColorTheme; set { _valueColorTheme = value; N(); } }

    public static string[] FrameShapeOptions      { get; } = ["Rectangle", "Circle"];
    public static string[] ValueColorThemeOptions { get; } = ["NeonBlue", "Success", "Warning", "Error", "Info", "White", "Gray", "NeonGreen", "NeonRed", "Primary"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}

public sealed class MachineViewModel : INotifyPropertyChanged
{
    private string _machineId   = "M1";
    private string _machineName = "Machine 1";
    private string _plcIp       = "192.168.1.100";
    private int    _plcPort     = 3000;
    private int    _pollMs      = 200;
    private string _isRunning   = "M200";
    private string _isCompleted = "M202";
    private string _isAlarm     = "M201";

    // Modules
    private bool _modProcessControl = true;
    private bool _modSensors;
    private bool _modAlarm;
    private bool _modPrintHead;
    private bool _modRecipe;
    private bool _modSimulator;

    private bool _showLiveLog;

    public string MachineId   { get => _machineId;   set { _machineId   = value; N(); N(nameof(DisplayName)); } }
    public string MachineName { get => _machineName; set { _machineName = value; N(); N(nameof(DisplayName)); } }
    public string PlcIp       { get => _plcIp;       set { _plcIp       = value; N(); } }
    public int    PlcPort     { get => _plcPort;     set { _plcPort     = value; N(); } }
    public int    PollMs      { get => _pollMs;      set { _pollMs      = value; N(); } }
    public string IsRunning   { get => _isRunning;   set { _isRunning   = value; N(); } }
    public string IsCompleted { get => _isCompleted; set { _isCompleted = value; N(); } }
    public string IsAlarm     { get => _isAlarm;     set { _isAlarm     = value; N(); } }

    public bool ModProcessControl { get => _modProcessControl; set { _modProcessControl = value; N(); N(nameof(ModulesSummary)); } }
    public bool ModSensors        { get => _modSensors;        set { _modSensors        = value; N(); N(nameof(ModulesSummary)); } }
    public bool ModAlarm          { get => _modAlarm;          set { _modAlarm          = value; N(); N(nameof(ModulesSummary)); } }
    public bool ModPrintHead      { get => _modPrintHead;      set { _modPrintHead      = value; N(); N(nameof(ModulesSummary)); } }
    public bool ModRecipe         { get => _modRecipe;         set { _modRecipe         = value; N(); N(nameof(ModulesSummary)); } }
    public bool ModSimulator      { get => _modSimulator;      set { _modSimulator      = value; N(); N(nameof(ModulesSummary)); } }

    public bool ShowLiveLog       { get => _showLiveLog;       set { _showLiveLog       = value; N(); } }

    public ObservableCollection<CommandRow>   Commands     { get; } = [];
    public ObservableCollection<LabelRow>    Labels       { get; } = [];
    public ObservableCollection<LabelRow>    StatusLabels { get; } = [];
    public ObservableCollection<DataEventRow> DataEvents  { get; } = [];

    public string DisplayName    => $"[{MachineId}] {MachineName}";
    public string ModulesSummary => string.Join(", ", GetEnabledModules());

    public string ModulesString => string.Join(";", GetEnabledModules());

    public IEnumerable<string> GetEnabledModules()
    {
        if (ModProcessControl) yield return "processControl";
        if (ModSensors)        yield return "sensors";
        if (ModAlarm)          yield return "alarm";
        if (ModPrintHead)      yield return "printHead";
        if (ModRecipe)         yield return "recipe";
        if (ModSimulator)      yield return "simulator";
    }

    public void ApplyModulesString(string modules)
    {
        var list = modules.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                          .Select(m => m.Trim().ToLower())
                          .ToHashSet();
        ModProcessControl = list.Contains("processcontrol");
        ModSensors        = list.Contains("sensors");
        ModAlarm          = list.Contains("alarm");
        ModPrintHead      = list.Contains("printhead");
        ModRecipe         = list.Contains("recipe");
        ModSimulator      = list.Contains("simulator");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}
