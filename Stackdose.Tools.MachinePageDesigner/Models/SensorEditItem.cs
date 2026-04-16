using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 感測器項目編輯模型（設計器內使用）
/// </summary>
public sealed class SensorEditItem : INotifyPropertyChanged
{
    private string _group = "General";
    private string _device = "D90";
    private string _bit = "";
    private string _value = "0";
    private string _mode = "AND";
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

    /// <summary>Bit 索引，支援逗號分隔多值</summary>
    public string Bit
    {
        get => _bit;
        set { if (_bit != value) { _bit = value; N(); } }
    }

    /// <summary>期望值，支援比較運算（>75, ==100）</summary>
    public string Value
    {
        get => _value;
        set { if (_value != value) { _value = value; N(); } }
    }

    /// <summary>AND / OR / COMPARE</summary>
    public string Mode
    {
        get => _mode;
        set { if (_mode != value) { _mode = value; N(); } }
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
        ["value"] = Value,
        ["mode"] = Mode,
        ["operationDescription"] = OperationDescription
    };

    public static SensorEditItem FromDictionary(Dictionary<string, object?> dict) => new()
    {
        Group = dict.GetString("group", "General"),
        Device = dict.GetString("device", "D90"),
        Bit = dict.GetString("bit", ""),
        Value = dict.GetString("value", "0"),
        Mode = dict.GetString("mode", "AND"),
        OperationDescription = dict.GetString("operationDescription", "")
    };

    public static readonly string[] Modes = ["AND", "OR", "COMPARE"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
