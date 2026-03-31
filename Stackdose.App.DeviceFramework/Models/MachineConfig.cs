namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// �q�ξ��x�]�w �X �q JSON Config �ϧǦC�ơC
/// �Ҧ��]�Ʀ@�Φ����c�A�]�ƱM������b DetailLabels / Modules / Commands�C
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
    /// �ʺA�������]Key = ��ܦW��, Value = PLC ��}�^�C
    /// ���P�]�ƥi�ۥѩw�q�U�ۻݭn���ʱ����C
    /// </summary>
    public Dictionary<string, string> DetailLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// �R�O�]�w�]Key = �R�O�W��, Value = PLC ��}�^�C
    /// ���P�]�ƥi�ۥѩw�q Start / Stop / Pause / EmergencyStop �����N�R�O�C
    /// </summary>
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// �s�{�ʱ��I��C
    /// </summary>
    public ProcessMonitorProfile ProcessMonitor { get; set; } = new();

    /// <summary>
    /// �ŧi���]�ƻݭn�ҥέ��ǥ\��ҲաC
    /// �Ҧp: ["processControl", "printHead", "alarm", "sensor"]
    /// </summary>
    public List<string> Modules { get; set; } = [];

    /// <summary>�O�_�b�]�ƭ�������� PlcDeviceEditor ���O</summary>
    public bool ShowPlcEditor { get; set; } = false;

    /// <summary>是否在機台頁面顯示 LiveLog（只顯示此機台的日誌）</summary>
    public bool ShowLiveLog { get; set; } = false;

    /// <summary>�]�ưʺA���}���Ʀ塿 Standard | SplitRight | Dashboard</summary>
    public string LayoutMode { get; set; } = "SplitRight";

    /// <summary>SplitRight 模式右欄寬度比例（Star），預設 0.85</summary>
    public double RightColumnWidthStar { get; set; } = 0.85;

    /// <summary>左側指令面板固定寬度（px），預設 250</summary>
    public int LeftCommandWidthPx { get; set; } = 250;

    /// <summary>中央面板標題，預設 "Live Data"</summary>
    public string LiveDataTitle { get; set; } = "Live Data";

    /// <summary>DeviceStatus 面板標籤字典（製程進度類，如層數、時間、批號等）</summary>
    public Dictionary<string, string> DetailStatusLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>DeviceStatus 標籤視覺樣式覆寫 (Key = LabelName)</summary>
    public Dictionary<string, LabelStyleConfig> StatusLabelStyles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>DeviceStatus 面板標題，預設 "Device Status"</summary>
    public string DeviceStatusTitle { get; set; } = "Device Status";

    /// <summary>數據變動事件清單</summary>
    public List<DataEventConfig> DataEvents { get; set; } = [];

    /// <summary>
    /// Label 視覺樣式覆寫 (Key = LabelName)。
    /// 未設定的 key 採控件預設值（矩形、NeonBlue）。
    /// </summary>
    public Dictionary<string, LabelStyleConfig> DetailLabelStyles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 指令按鈕主題覆寫 (Key = CommandName)。
    /// 未設定的 key 由 ViewModel 依名稱自動推斷。
    /// </summary>
    public Dictionary<string, CommandStyleConfig> CommandStyles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LabelStyleConfig
{
    /// <summary>底框形狀：Rectangle | Circle</summary>
    public string FrameShape { get; set; } = "Rectangle";

    /// <summary>數值顏色主題：NeonBlue | Success | Warning | Error | Info | White | Gray 等</summary>
    public string ValueColorTheme { get; set; } = "NeonBlue";
}

public sealed class CommandStyleConfig
{
    /// <summary>按鈕主題：Primary | Success | Error | Warning | Info</summary>
    public string Theme { get; set; } = "Primary";
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
