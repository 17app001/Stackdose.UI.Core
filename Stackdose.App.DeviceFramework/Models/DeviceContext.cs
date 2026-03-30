namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// �q�γ]�ƤW�U�� �X ���A�w�s�X 14 ���ݩʡA
/// ��� Labels �r�崣�ѰʺA���A�[�W�ֶq�֤����C
/// </summary>
public sealed class DeviceContext
{
    // �w�w �֤��ѧO �w�w
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;

    // �w�w �s�{�ʱ����n��} �w�w
    public string RunningAddress { get; set; } = "--";
    public string CompletedAddress { get; set; } = "--";
    public string AlarmAddress { get; set; } = "--";

    // �w�w Config �ɮ׸��| �w�w
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public List<string> PrintHeadConfigFiles { get; set; } = [];

    /// <summary>�O�_�b�]�ƭ�������� PlcDeviceEditor ���O</summary>
    public bool ShowPlcEditor { get; set; } = false;

    /// <summary>�]�ưʺA���}���Ʀ塿 Standard | SplitRight | Dashboard</summary>
    public string LayoutMode { get; set; } = "SplitRight";

    // �w�w �R�O��}�]Key = �R�O�W��, Value = PLC ��}�^ �w�w
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // �w�w �ʺA�������]Key = ��ܦW��, Value = PLC ��}�^ �w�w
    // ���P�]�ƥi�ۥѩw�q�U�ۻݭn���ʱ����C
    // �Ҧp�Q�L��: { "Total Tray": "D3400", "Current Layer": "D32", "Battery": "D120" }
    // �Ҧp�M�N�l: { "Oven Temp": "D100", "Cooling Temp": "D101", "Conveyor Speed": "D200" }
    public Dictionary<string, DeviceLabelInfo> Labels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // �w�w �ҥΪ��\��ҲզW�� �w�w
    public List<string> EnabledModules { get; set; } = [];

    /// <summary>數據變動事件清單</summary>
    public List<DataEventConfig> DataEvents { get; set; } = [];
}

/// <summary>
/// ��@������쪺����y�z�C
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
