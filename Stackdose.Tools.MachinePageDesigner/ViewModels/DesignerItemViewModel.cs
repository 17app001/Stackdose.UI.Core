using System.Windows;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 嚙踝蕭@嚙踝蕭嚙踝蕭 ViewModel嚙稽props 嚙踝蕭嚙碾嚙篌嚙緩嚙稷
/// </summary>
public sealed class DesignerItemViewModel : ObservableObject
{
    private readonly DesignerItemDefinition _definition;
    private bool _isSelected;
    private UIElement? _preview;

    public DesignerItemViewModel(DesignerItemDefinition definition)
    {
        _definition = definition;
        RefreshPreview();
    }

    // 嚙緩嚙緩 Identity 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string Id => _definition.Id;
    public string ItemType => _definition.Type;

    public int Order
    {
        get => _definition.Order;
        set { _definition.Order = value; N(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    public bool IsLocked
    {
        get => _definition.IsLocked;
        set
        {
            if (_definition.IsLocked == value) return;
            var old = _definition.IsLocked;
            _definition.IsLocked = value;
            N();
            PropCommitted?.Invoke("isLocked", old, value);
        }
    }

    // 嚙緩嚙緩 Props 嚙編嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public Dictionary<string, object?> Props => _definition.Props;

    public string GetProp(string key, string fallback = "")
        => _definition.Props.GetString(key, fallback);

    public double GetPropDouble(string key, double fallback = 0)
        => _definition.Props.GetDouble(key, fallback);

    /// <summary>
    /// 屬性提交事件：(propKey, oldValue, newValue)
    /// UI 雙向綁定改值後觸發，供 MainViewModel 記錄至 UndoRedo。
    /// </summary>
    public event Action<string, object?, object?>? PropCommitted;

    public void SetProp(string key, object? value)
    {
        _definition.Props[key] = value;
        N(nameof(Props));
        RefreshPreview();
    }

    /// <summary>
    /// 字串屬性 set 輔助：比較→寫入→通知→提交
    /// </summary>
    private bool CommitStr(string key, string value, string fallback = "", params string[] extraNotify)
    {
        var old = GetProp(key, fallback);
        if (old == value) return false;
        SetPropDirect(key, value);
        foreach (var n in extraNotify) N(n);
        PropCommitted?.Invoke(key, old, value);
        return true;
    }

    /// <summary>
    /// 數值屬性 set 輔助：比較→寫入→通知→提交
    /// </summary>
    private bool CommitDbl(string key, double value, double fallback = 0)
    {
        var old = GetPropDouble(key, fallback);
        if (old == value) return false;
        SetPropDirect(key, value);
        PropCommitted?.Invoke(key, old, value);
        return true;
    }

    public void SetPropDirect(string key, object? value)
    {
        var d = ToDouble(value);
        switch (key)
        {
            case "isLocked":
                _definition.IsLocked = value is bool b ? b :
                    bool.TryParse(value?.ToString(), out var b2) && b2;
                N(nameof(IsLocked));
                return;
            case "x":      _definition.X = Math.Max(0, d);        N(nameof(X));      return;
            case "y":      _definition.Y = Math.Max(0, d);        N(nameof(Y));      return;
            case "width":  _definition.Width = Math.Max(40, d);   N(nameof(Width));  return;
            case "height": _definition.Height = Math.Max(30, d);  N(nameof(Height)); return;
        }
        _definition.Props[key] = value;
        N(nameof(Props));
        NotifyPropKey(key);
        RefreshPreview();
    }

    private static double ToDouble(object? value) => value switch
    {
        double d => d,
        int i => i,
        string s when double.TryParse(s, out var r) => r,
        _ => 0
    };

    private void NotifyPropKey(string key)
    {
        // 撠?prop key ????VM 撅祆批?隞亥孛??UI ?湔
        // "labelForeground" is shared by LabelForeground (PlcLabel) and IndicatorLabelForeground (PlcStatusIndicator)
        if (key == "labelForeground")
        {
            N(nameof(LabelForeground));
            N(nameof(IndicatorLabelForeground));
            return;
        }
        var propName = key switch
        {
            "label" => nameof(Label),
            "address" => nameof(Address),
            "defaultValue" => nameof(DefaultValue),
            "valueFontSize" => nameof(ValueFontSize),
            "frameShape" => nameof(FrameShape),
            "valueColorTheme" => nameof(ValueColorTheme),
            "frameBackground" => nameof(FrameBackground),
            "labelAlignment" => nameof(LabelAlignment),
            "valueAlignment" => nameof(ValueAlignment),
            "labelFontSize" => nameof(LabelFontSize),
            "plcTextMode" => nameof(PlcTextMode),
            "divisor" => nameof(Divisor),
            "stringFormat" => nameof(StringFormat),
            "displayAddress" => nameof(DisplayAddress),
            "cardBackground" => nameof(IndicatorCardBackground),
            "commandAddress" => nameof(CommandAddress),
            "requiredLevel" => nameof(RequiredLevel),
            "theme" => nameof(Theme),
            "writeValue" => nameof(WriteValue),
            "commandType" => nameof(CommandType),
            "pulseMs" => nameof(PulseMs),
            "sequenceDefinition" => nameof(SequenceDefinition),
            "isLocked" => nameof(IsLocked),
            "title"            => nameof(GroupTitle),
            "groupHeaderTheme" => nameof(GroupHeaderTheme),
            "configFile" => nameof(ConfigFile),
            "staticText" => nameof(StaticText),
            "staticFontSize" => nameof(StaticFontSize),
            "staticFontWeight" => nameof(StaticFontWeight),
            "staticTextAlign" => nameof(StaticTextAlign),
            "staticForeground" => nameof(StaticForeground),
            _ => null
        };
        if (propName != null) N(propName);
    }

    // 嚙緩嚙緩 嚙窯嚙踝蕭嚙豎性快梧蕭嚙編嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string Label
    {
        get => GetProp("label", ItemType);
        set => CommitStr("label", value, ItemType, nameof(DisplayName));
    }

    public string Address
    {
        get => GetProp("address");
        set => CommitStr("address", value, "", nameof(DisplayName));
    }

    public string DefaultValue
    {
        get => GetProp("defaultValue", "0");
        set => CommitStr("defaultValue", value, "0");
    }

    public double ValueFontSize
    {
        get => GetPropDouble("valueFontSize", 20);
        set => CommitDbl("valueFontSize", value, 20);
    }

    public string FrameShape
    {
        get => GetProp("frameShape", "Rectangle");
        set => CommitStr("frameShape", value, "Rectangle");
    }

    public string ValueColorTheme
    {
        get => GetProp("valueColorTheme", "NeonBlue");
        set => CommitStr("valueColorTheme", value, "NeonBlue");
    }

    public string FrameBackground
    {
        get => GetProp("frameBackground", "DarkBlue");
        set => CommitStr("frameBackground", value, "DarkBlue");
    }

    public string LabelAlignment
    {
        get => GetProp("labelAlignment", "Left");
        set => CommitStr("labelAlignment", value, "Left");
    }

    public string ValueAlignment
    {
        get => GetProp("valueAlignment", "Right");
        set => CommitStr("valueAlignment", value, "Right");
    }

    public double LabelFontSize
    {
        get => GetPropDouble("labelFontSize", 12);
        set => CommitDbl("labelFontSize", value, 12);
    }

    public string LabelForeground
    {
        get => GetProp("labelForeground", "Default");
        set => CommitStr("labelForeground", value, "Default");
    }

    public string PlcTextMode
    {
        get => GetProp("plcTextMode", "Word");
        set => CommitStr("plcTextMode", value, "Word");
    }

    public double Divisor
    {
        get => GetPropDouble("divisor", 1);
        set => CommitDbl("divisor", value, 1);
    }

    public string StringFormat
    {
        get => GetProp("stringFormat", "F0");
        set => CommitStr("stringFormat", value, "F0");
    }

    public string DisplayAddress
    {
        get => GetProp("displayAddress");
        set => CommitStr("displayAddress", value, "", nameof(DisplayName));
    }

    public string IndicatorCardBackground
    {
        get => GetProp("cardBackground", "");
        set => CommitStr("cardBackground", value, "");
    }

    public string IndicatorLabelForeground
    {
        get => GetProp("labelForeground", "#9090B0");
        set => CommitStr("labelForeground", value, "#9090B0");
    }

    public string CommandAddress
    {
        get => GetProp("commandAddress");
        set => CommitStr("commandAddress", value);
    }

    public string RequiredLevel
    {
        get => GetProp("requiredLevel", "Operator");
        set => CommitStr("requiredLevel", value, "Operator");
    }

    public string Theme
    {
        get => GetProp("theme", "Primary");
        set => CommitStr("theme", value, "Primary");
    }

    public string WriteValue
    {
        get => GetProp("writeValue", "1");
        set => CommitStr("writeValue", value, "1");
    }

    public string CommandType
    {
        get => GetProp("commandType", "write");
        set => CommitStr("commandType", value, "write");
    }

    public double PulseMs
    {
        get => GetPropDouble("pulseMs", 300);
        set => CommitDbl("pulseMs", value, 300);
    }

    /// <summary>序列定義 JSON（commandType=sequence 時使用）</summary>
    public string SequenceDefinition
    {
        get => GetProp("sequenceDefinition", "");
        set => CommitStr("sequenceDefinition", value);
    }

    public string GroupTitle
    {
        get => GetProp("title", "Group");
        set => CommitStr("title", value, "Group");
    }

    public string GroupHeaderTheme
    {
        get => GetProp("groupHeaderTheme", "Primary");
        set => CommitStr("groupHeaderTheme", value, "Primary");
    }

    public string ConfigFile
    {
        get => GetProp("configFile");
        set => CommitStr("configFile", value);
    }

    public string StaticText
    {
        get => GetProp("staticText", "Text");
        set => CommitStr("staticText", value, "Text", nameof(DisplayName));
    }

    public double StaticFontSize
    {
        get => GetPropDouble("staticFontSize", 16);
        set => CommitDbl("staticFontSize", value, 16);
    }

    public string StaticFontWeight
    {
        get => GetProp("staticFontWeight", "Normal");
        set => CommitStr("staticFontWeight", value, "Normal");
    }

    public string StaticTextAlign
    {
        get => GetProp("staticTextAlign", "Left");
        set => CommitStr("staticTextAlign", value, "Left");
    }

    public string StaticForeground
    {
        get => GetProp("staticForeground", "#E2E2F0");
        set => CommitStr("staticForeground", value, "#E2E2F0");
    }

    // ── 自由畫布空間屬性 ──────────────────────────────────────────────────
    public double X
    {
        get => _definition.X;
        set
        {
            var v = Math.Max(0, value);
            if (_definition.X == v) return;
            var old = _definition.X;
            _definition.X = v; N();
            PropCommitted?.Invoke("x", old, v);
        }
    }

    public double Y
    {
        get => _definition.Y;
        set
        {
            var v = Math.Max(0, value);
            if (_definition.Y == v) return;
            var old = _definition.Y;
            _definition.Y = v; N();
            PropCommitted?.Invoke("y", old, v);
        }
    }

    public double Width
    {
        get => _definition.Width;
        set
        {
            var v = Math.Max(40, value);
            if (_definition.Width == v) return;
            var old = _definition.Width;
            _definition.Width = v; N();
            PropCommitted?.Invoke("width", old, v);
        }
    }

    public double Height
    {
        get => _definition.Height;
        set
        {
            var v = Math.Max(30, value);
            if (_definition.Height == v) return;
            var old = _definition.Height;
            _definition.Height = v; N();
            PropCommitted?.Invoke("height", old, v);
        }
    }

    // 嚙緩嚙緩 嚙踝蕭嚙踐項嚙緩嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public UIElement? Preview
    {
        get => _preview;
        private set => Set(ref _preview, value);
    }

    public void RefreshPreview()
    {
        Preview = DesignTimeControlFactory.Create(_definition);
    }

    // 嚙緩嚙緩 嚙論出 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public DesignerItemDefinition ToDefinition() => _definition;

    // 嚙緩嚙緩 嚙踝蕭雃W嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string DisplayName => ItemType switch
    {
        "PlcLabel"           => $"PlcLabel [{Address}]",
        "PlcText"            => $"PlcText [{Address}]",
        "PlcStatusIndicator" => $"StatusIndicator [{DisplayAddress}]",
        "SecuredButton"      => $"Button [{Label}]",
        "Spacer"             => "Spacer",
        "LiveLog"            => "LiveLog",
        "AlarmViewer"        => $"AlarmViewer [{ConfigFile}]",
        "SensorViewer"       => $"SensorViewer [{ConfigFile}]",
        "StaticLabel"        => $"Text [{StaticText}]",
        _ => ItemType
    };

    // 嚙緩嚙緩 嚙箠嚙踝蕭嚙豎性列嚙踝蕭嚙稽嚙踝蕭 UI 嚙諸考） 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public static readonly string[] ColorThemes =
        ["NeonBlue", "NeonGreen", "NeonRed", "White", "Gray", "Warning", "Error", "Success", "Info", "Primary", "Default"];

    public static readonly string[] FrameShapes = ["Rectangle", "Circle"];
    public static readonly string[] StringFormats = ["F0", "F1", "F2", "F3"];
    public static readonly string[] ButtonThemes = ["Primary", "Success", "Danger", "Warning"];
    public static readonly string[] AccessLevels = ["Operator", "Instructor", "Supervisor", "Admin", "SuperAdmin"];
    public static readonly string[] CommandTypes = ["write", "pulse", "toggle", "sequence"];
    public static readonly string[] FontWeightOptions       = ["Normal", "Bold"];
    public static readonly string[] TextAlignOptions        = ["Left", "Center", "Right"];
    public static readonly string[] GroupHeaderThemeOptions = ["Primary", "Info", "Success", "Warning", "Error", "Dark"];
    public static readonly string[] ForegroundOptions =
        ["#E2E2F0", "#FFFFFF", "#6C8EEF", "#4EC994", "#EF5350", "#FFB74D", "#90CAF9", "#AAAAAA", "#000000"];
}
