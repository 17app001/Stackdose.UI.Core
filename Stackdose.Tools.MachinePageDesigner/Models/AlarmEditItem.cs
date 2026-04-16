using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 報警項目編輯模型（設計器內使用）
/// </summary>
public sealed class AlarmEditItem : INotifyPropertyChanged
{
    private string _group = "General";
    private string _device = "D200";
    private int _bit;
    private string _description = "";

    public string Group
    {
        get => _group;
        set { if (_group != value) { _group = value; N(); } }
    }

    public string Device
    {
        get => _device;
        set { if (_device != value) { _device = value; N(); } }
    }

    public int Bit
    {
        get => _bit;
        set { if (_bit != value) { _bit = value; N(); } }
    }

    public string OperationDescription
    {
        get => _description;
        set { if (_description != value) { _description = value; N(); } }
    }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        ["group"] = Group,
        ["device"] = Device,
        ["bit"] = Bit,
        ["operationDescription"] = OperationDescription
    };

    public static AlarmEditItem FromDictionary(Dictionary<string, object?> dict) => new()
    {
        Group = dict.GetString("group", "General"),
        Device = dict.GetString("device", "D200"),
        Bit = dict.GetInt("bit", 0),
        OperationDescription = dict.GetString("operationDescription", "")
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
