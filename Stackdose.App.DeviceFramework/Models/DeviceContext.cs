namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// 通用設備上下文 — 不再硬編碼 14 個屬性，
/// 改用 Labels 字典提供動態欄位，加上少量核心欄位。
/// </summary>
public sealed class DeviceContext
{
    // ── 核心識別 ──
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;

    // ── 製程監控必要位址 ──
    public string RunningAddress { get; set; } = "--";
    public string CompletedAddress { get; set; } = "--";
    public string AlarmAddress { get; set; } = "--";

    // ── Config 檔案路徑 ──
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public List<string> PrintHeadConfigFiles { get; set; } = [];

    /// <summary>是否在設備頁底部顯示 PlcDeviceEditor 面板</summary>
    public bool ShowPlcEditor { get; set; } = false;

    // ── 命令位址（Key = 命令名稱, Value = PLC 位址） ──
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ── 動態標籤欄位（Key = 顯示名稱, Value = PLC 位址） ──
    // 不同設備可自由定義各自需要的監控欄位。
    // 例如噴印機: { "Total Tray": "D3400", "Current Layer": "D32", "Battery": "D120" }
    // 例如烘烤爐: { "Oven Temp": "D100", "Cooling Temp": "D101", "Conveyor Speed": "D200" }
    public Dictionary<string, DeviceLabelInfo> Labels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ── 啟用的功能模組名稱 ──
    public List<string> EnabledModules { get; set; } = [];
}

/// <summary>
/// 單一標籤欄位的完整描述。
/// </summary>
public sealed class DeviceLabelInfo
{
    public DeviceLabelInfo() { }

    public DeviceLabelInfo(string address, string defaultValue = "0", string dataType = "Word", int divisor = 1, string stringFormat = "")
    {
        Address = address;
        DefaultValue = defaultValue;
        DataType = dataType;
        Divisor = divisor;
        StringFormat = stringFormat;
    }

    public string Address { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = "0";
    public string DataType { get; set; } = "Word";
    public int Divisor { get; set; } = 1;
    public string StringFormat { get; set; } = string.Empty;
}
