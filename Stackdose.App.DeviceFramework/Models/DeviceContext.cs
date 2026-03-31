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

    /// <summary>是否在裝置頁面顯示 LiveLog（只顯示此機台的日誌）</summary>
    public bool ShowLiveLog { get; set; } = false;

    /// <summary>�]�ưʺA���}���Ʀ塿 Standard | SplitRight | Dashboard</summary>
    public string LayoutMode { get; set; } = "SplitRight";

    /// <summary>SplitRight 模式右欄寬度比例（相對於左欄的 Star 比，預設 0.85）</summary>
    public double RightColumnWidthStar { get; set; } = 0.85;

    /// <summary>左側指令面板固定寬度（px），預設 250</summary>
    public int LeftCommandWidthPx { get; set; } = 250;

    /// <summary>中央 Live Data 面板標題（預設 "Live Data"）</summary>
    public string LiveDataTitle { get; set; } = "Live Data";

    /// <summary>DeviceStatus 面板標籤（製程進度類：層數/時間/批號等）</summary>
    public Dictionary<string, DeviceLabelInfo> StatusLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>DeviceStatus 面板標題（預設 "Device Status"）</summary>
    public string DeviceStatusTitle { get; set; } = "Device Status";

    // �w�w �R�O��}�]Key = �R�O�W��, Value = PLC ��}�^ �w�w
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>指令顏色主題覆寫 (Key = 指令名稱, Value = Theme 字串)。未設定時由 ViewModel 自動推斷。</summary>
    public Dictionary<string, string> CommandThemes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>底框形狀：Rectangle | Circle</summary>
    public string FrameShape { get; set; } = "Rectangle";

    /// <summary>數值顏色主題：NeonBlue | Success | Warning | Error | Info | White | Gray 等</summary>
    public string ValueColorTheme { get; set; } = "NeonBlue";
}
