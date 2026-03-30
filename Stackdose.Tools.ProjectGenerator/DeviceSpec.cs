namespace Stackdose.Tools.ProjectGenerator;

/// <summary>
/// Parsed result from a Device-Spec CSV file.
/// </summary>
public sealed class DeviceSpec
{
    public ProjectInfo Project { get; set; } = new();
    public List<MachineInfo> Machines { get; set; } = [];
    public List<CommandInfo> Commands { get; set; } = [];
    public List<LabelInfo> Labels { get; set; } = [];
    public List<TagInfo> Tags { get; set; } = [];
    public List<PanelInfo> Panels { get; set; } = [];
    public List<MaintenanceItemInfo> MaintenanceItems { get; set; } = [];
    public List<DataEventInfo> DataEvents { get; set; } = [];
}

public sealed class DataEventInfo
{
    public string MachineId  { get; set; } = string.Empty;
    public string Name       { get; set; } = string.Empty;
    public string Address    { get; set; } = string.Empty;
    public string Trigger    { get; set; } = "changed";
    public int    Threshold  { get; set; } = 0;
    public string DataType   { get; set; } = string.Empty;
}

public sealed class ProjectInfo
{
    public string ProjectName { get; set; } = string.Empty;
    public string HeaderDeviceName { get; set; } = string.Empty;
    public string Version { get; set; } = "v1.0.0";
    public string PageMode { get; set; } = "DynamicDevicePage";
    public string LayoutMode { get; set; } = "SplitRight";
    public bool AutoConnect { get; set; } = false;

    /// <summary>Short name derived from ProjectName, e.g. "Stackdose.App.OvenControl" �� "OvenControl"</summary>
    public string ShortName => ProjectName.Contains('.')
        ? ProjectName[(ProjectName.LastIndexOf('.') + 1)..]
        : ProjectName;
}

public sealed class MachineInfo
{
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string PlcIp { get; set; } = "127.0.0.1";
    public int PlcPort { get; set; } = 3000;
    public int PollIntervalMs { get; set; } = 200;
    public string ProcessMonitorIsRunning { get; set; } = "M200";
    public string ProcessMonitorIsCompleted { get; set; } = "M202";
    public string ProcessMonitorIsAlarm { get; set; } = "M201";

    /// <summary>
    /// 啟用的 UI 模組，分號分隔。可用值：
    ///   processControl  — 製程狀態 + 指令按鈕（預設）
    ///   sensors         — SensorViewer + 自動產生 sensors.json 範本
    ///   alarm           — AlarmViewer  + 自動產生 alarms.json 範本
    ///   printHead       — PrintHeadController + 自動產生 printhead1.json 範本
    ///   recipe          — RecipeLoader 控制項
    ///   simulator       — SimulatorControlPanel 控制項
    /// </summary>
    public string Modules { get; set; } = "processControl";
}

public sealed class CommandInfo
{
    public string MachineId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public sealed class LabelInfo
{
    public string MachineId { get; set; } = string.Empty;
    public string LabelName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public sealed class TagInfo
{
    public string MachineId { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = "int16";
    public string Access { get; set; } = "read";
    public int Length { get; set; } = 1;
}

public sealed class PanelInfo
{
    public string PanelType { get; set; } = string.Empty;
    public string MachineId { get; set; } = "*";
    public string Position { get; set; } = "Separate";
    public string Title { get; set; } = string.Empty;
    public string RequiredLevel { get; set; } = "Supervisor";
}

public sealed class MaintenanceItemInfo
{
    public string MachineId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    /// <summary>toggle | momentary | editor | readonly</summary>
    public string Type { get; set; } = "editor";
    public string Label { get; set; } = string.Empty;
}
