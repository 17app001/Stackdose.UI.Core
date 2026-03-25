namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// 通用機台設定 — 從 JSON Config 反序列化。
/// 所有設備共用此結構，設備專屬欄位放在 DetailLabels / Modules / Commands。
/// </summary>
public sealed class MachineConfig
{
    public MachineInfo Machine { get; set; } = new();
    public PlcInfo Plc { get; set; } = new();
    public TagSections Tags { get; set; } = new();
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public List<string> PrintHeadConfigs { get; set; } = [];

    /// <summary>
    /// 動態標籤欄位（Key = 顯示名稱, Value = PLC 位址）。
    /// 不同設備可自由定義各自需要的監控欄位。
    /// </summary>
    public Dictionary<string, string> DetailLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 命令設定（Key = 命令名稱, Value = PLC 位址）。
    /// 不同設備可自由定義 Start / Stop / Pause / EmergencyStop 等任意命令。
    /// </summary>
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 製程監控點位。
    /// </summary>
    public ProcessMonitorProfile ProcessMonitor { get; set; } = new();

    /// <summary>
    /// 宣告此設備需要啟用哪些功能模組。
    /// 例如: ["processControl", "printHead", "alarm", "sensor"]
    /// </summary>
    public List<string> Modules { get; set; } = [];

    /// <summary>是否在設備頁底部顯示 PlcDeviceEditor 面板</summary>
    public bool ShowPlcEditor { get; set; } = false;
}

public sealed class MachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
}

public sealed class PlcInfo
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
    public int PollIntervalMs { get; set; } = 300;
    public bool AutoConnect { get; set; } = true;
    public List<string> MonitorAddresses { get; set; } = [];
}

public sealed class TagSections
{
    public Dictionary<string, TagConfig> Status { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TagConfig> Process { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TagConfig
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int Length { get; set; } = 1;
}

public sealed class ProcessMonitorProfile
{
    public string IsRunning { get; set; } = string.Empty;
    public string IsCompleted { get; set; } = string.Empty;
    public string IsAlarm { get; set; } = string.Empty;
}
