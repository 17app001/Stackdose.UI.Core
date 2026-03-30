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

    /// <summary>�]�ưʺA���}���Ʀ塿 Standard | SplitRight | Dashboard</summary>
    public string LayoutMode { get; set; } = "SplitRight";

    /// <summary>數據變動事件清單</summary>
    public List<DataEventConfig> DataEvents { get; set; } = [];
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
