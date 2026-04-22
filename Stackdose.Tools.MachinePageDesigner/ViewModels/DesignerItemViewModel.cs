using System.Collections.ObjectModel;
using System.Windows;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 嚙踝蕭@嚙踝蕭嚙踝蕭 ViewModel嚙稽props 嚙踝蕭嚙碾嚙篌嚙緩嚙稷
/// </summary>
public sealed class DesignerItemViewModel : ObservableObject
{
    private readonly DesignerItemDefinition _definition;
    private bool _isSelected;
    private UIElement? _preview;
    private ObservableCollection<BehaviorEventViewModel>? _events;

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
    /// ?湔閮剖?撅祆批潘?靘?UndoRedo Command 雿輻嚗?閫貊憿??賭誘嚗?
    /// </summary>
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
        var propName = key switch
        {
            "label" => nameof(Label),
            "address" => nameof(Address),
            "defaultValue" => nameof(DefaultValue),
            "valueFontSize" => nameof(ValueFontSize),
            "frameShape" => nameof(FrameShape),
            "valueColorTheme" => nameof(ValueColorTheme),
            "divisor" => nameof(Divisor),
            "stringFormat" => nameof(StringFormat),
            "displayAddress" => nameof(DisplayAddress),
            "commandAddress" => nameof(CommandAddress),
            "requiredLevel" => nameof(RequiredLevel),
            "theme" => nameof(Theme),
            "isLocked" => nameof(IsLocked),
            "title" => nameof(GroupTitle),
            "labelFontSize" => nameof(LabelFontSize),
            "labelAlignment" => nameof(LabelAlignment),
            "valueAlignment" => nameof(ValueAlignment),
            "labelForeground" => nameof(LabelForeground),
            "frameBackground" => nameof(FrameBackground),
            "staticText" => nameof(StaticText),
            "staticFontSize" => nameof(StaticFontSize),
            "staticFontWeight" => nameof(StaticFontWeight),
            "staticTextAlign" => nameof(StaticTextAlign),
            "staticForeground" => nameof(StaticForeground),
            "enableLiveRecord" => nameof(EnableLiveRecord),
            _ => null
        };
        if (propName != null) N(propName);
    }

    // 嚙緩嚙緩 嚙窯嚙踝蕭嚙豎性快梧蕭嚙編嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string Label
    {
        get => GetProp("label", ItemType);
        set
        {
            var old = GetProp("label", ItemType);
            if (old == value) return;
            SetPropDirect("label", value);
            N(nameof(DisplayName));
            PropCommitted?.Invoke("label", old, value);
        }
    }

    public string Address
    {
        get => GetProp("address");
        set
        {
            var old = GetProp("address");
            if (old == value) return;
            SetPropDirect("address", value);
            N(nameof(DisplayName));
            PropCommitted?.Invoke("address", old, value);
        }
    }

    public string DefaultValue
    {
        get => GetProp("defaultValue", "0");
        set
        {
            var old = GetProp("defaultValue", "0");
            if (old == value) return;
            SetPropDirect("defaultValue", value);
            PropCommitted?.Invoke("defaultValue", old, value);
        }
    }

    public double ValueFontSize
    {
        get => GetPropDouble("valueFontSize", 20);
        set
        {
            var old = GetPropDouble("valueFontSize", 20);
            if (old == value) return;
            SetPropDirect("valueFontSize", value);
            PropCommitted?.Invoke("valueFontSize", old, value);
        }
    }

    public string FrameShape
    {
        get => GetProp("frameShape", "Rectangle");
        set
        {
            var old = GetProp("frameShape", "Rectangle");
            if (old == value) return;
            SetPropDirect("frameShape", value);
            PropCommitted?.Invoke("frameShape", old, value);
        }
    }

    public string ValueColorTheme
    {
        get => GetProp("valueColorTheme", "NeonBlue");
        set
        {
            var old = GetProp("valueColorTheme", "NeonBlue");
            if (old == value) return;
            SetPropDirect("valueColorTheme", value);
            PropCommitted?.Invoke("valueColorTheme", old, value);
        }
    }

    public double Divisor
    {
        get => GetPropDouble("divisor", 1);
        set
        {
            var old = GetPropDouble("divisor", 1);
            if (old == value) return;
            SetPropDirect("divisor", value);
            PropCommitted?.Invoke("divisor", old, value);
        }
    }

    public string StringFormat
    {
        get => GetProp("stringFormat", "F0");
        set
        {
            var old = GetProp("stringFormat", "F0");
            if (old == value) return;
            SetPropDirect("stringFormat", value);
            PropCommitted?.Invoke("stringFormat", old, value);
        }
    }

    public string DisplayAddress
    {
        get => GetProp("displayAddress");
        set
        {
            var old = GetProp("displayAddress");
            if (old == value) return;
            SetPropDirect("displayAddress", value);
            N(nameof(DisplayName));
            PropCommitted?.Invoke("displayAddress", old, value);
        }
    }

    public string CommandAddress
    {
        get => GetProp("commandAddress");
        set
        {
            var old = GetProp("commandAddress");
            if (old == value) return;
            SetPropDirect("commandAddress", value);
            PropCommitted?.Invoke("commandAddress", old, value);
        }
    }

    public string RequiredLevel
    {
        get => GetProp("requiredLevel", "Operator");
        set
        {
            var old = GetProp("requiredLevel", "Operator");
            if (old == value) return;
            SetPropDirect("requiredLevel", value);
            PropCommitted?.Invoke("requiredLevel", old, value);
        }
    }

    public string Theme
    {
        get => GetProp("theme", "Primary");
        set
        {
            var old = GetProp("theme", "Primary");
            if (old == value) return;
            SetPropDirect("theme", value);
            PropCommitted?.Invoke("theme", old, value);
        }
    }

    public string GroupTitle
    {
        get => GetProp("title", "Group");
        set
        {
            var old = GetProp("title", "Group");
            if (old == value) return;
            SetPropDirect("title", value);
            PropCommitted?.Invoke("title", old, value);
        }
    }

    public double LabelFontSize
    {
        get => GetPropDouble("labelFontSize", 12);
        set
        {
            var old = GetPropDouble("labelFontSize", 12);
            if (old == value) return;
            SetPropDirect("labelFontSize", value);
            PropCommitted?.Invoke("labelFontSize", old, value);
        }
    }

    public string LabelAlignment
    {
        get => GetProp("labelAlignment", "Left");
        set
        {
            var old = GetProp("labelAlignment", "Left");
            if (old == value) return;
            SetPropDirect("labelAlignment", value);
            PropCommitted?.Invoke("labelAlignment", old, value);
        }
    }

    public string ValueAlignment
    {
        get => GetProp("valueAlignment", "Right");
        set
        {
            var old = GetProp("valueAlignment", "Right");
            if (old == value) return;
            SetPropDirect("valueAlignment", value);
            PropCommitted?.Invoke("valueAlignment", old, value);
        }
    }

    public string LabelForeground
    {
        get => GetProp("labelForeground", "Default");
        set
        {
            var old = GetProp("labelForeground", "Default");
            if (old == value) return;
            SetPropDirect("labelForeground", value);
            PropCommitted?.Invoke("labelForeground", old, value);
        }
    }

    public string FrameBackground
    {
        get => GetProp("frameBackground", "DarkBlue");
        set
        {
            var old = GetProp("frameBackground", "DarkBlue");
            if (old == value) return;
            SetPropDirect("frameBackground", value);
            PropCommitted?.Invoke("frameBackground", old, value);
        }
    }

    public string StaticText
    {
        get => GetProp("staticText", "Label");
        set
        {
            var old = GetProp("staticText", "Label");
            if (old == value) return;
            SetPropDirect("staticText", value);
            N(nameof(DisplayName));
            PropCommitted?.Invoke("staticText", old, value);
        }
    }

    public double StaticFontSize
    {
        get => GetPropDouble("staticFontSize", 16);
        set
        {
            var old = GetPropDouble("staticFontSize", 16);
            if (old == value) return;
            SetPropDirect("staticFontSize", value);
            PropCommitted?.Invoke("staticFontSize", old, value);
        }
    }

    public string StaticFontWeight
    {
        get => GetProp("staticFontWeight", "Normal");
        set
        {
            var old = GetProp("staticFontWeight", "Normal");
            if (old == value) return;
            SetPropDirect("staticFontWeight", value);
            PropCommitted?.Invoke("staticFontWeight", old, value);
        }
    }

    public string StaticTextAlign
    {
        get => GetProp("staticTextAlign", "Left");
        set
        {
            var old = GetProp("staticTextAlign", "Left");
            if (old == value) return;
            SetPropDirect("staticTextAlign", value);
            PropCommitted?.Invoke("staticTextAlign", old, value);
        }
    }

    public string StaticForeground
    {
        get => GetProp("staticForeground", "#E2E2F0");
        set
        {
            var old = GetProp("staticForeground", "#E2E2F0");
            if (old == value) return;
            SetPropDirect("staticForeground", value);
            PropCommitted?.Invoke("staticForeground", old, value);
        }
    }

    public bool EnableLiveRecord
    {
        get => _definition.Props.GetBool("enableLiveRecord", true);
        set
        {
            var old = _definition.Props.GetBool("enableLiveRecord", true);
            if (old == value) return;
            SetPropDirect("enableLiveRecord", value);
            PropCommitted?.Invoke("enableLiveRecord", old, value);
        }
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

    // ── Events（B6 行為編輯器）─────────────────────────────────────────────

    /// <summary>
    /// 當前控制項的行為事件清單。延遲初始化，改動直接寫回 _definition.Events。
    /// </summary>
    public ObservableCollection<BehaviorEventViewModel> Events
        => _events ??= BuildEventsCollection();

    private ObservableCollection<BehaviorEventViewModel> BuildEventsCollection()
    {
        var col = new ObservableCollection<BehaviorEventViewModel>(
            _definition.Events.Select(e => new BehaviorEventViewModel(e)));
        col.CollectionChanged += (_, _) =>
        {
            // 保持 _definition.Events 與 VM 集合同步
            _definition.Events.Clear();
            foreach (var vm in col)
                _definition.Events.Add(vm.ToModel());
        };
        return col;
    }

    public void AddEvent()
    {
        var model = new BehaviorEvent { On = "valueChanged" };
        _definition.Events.Add(model);
        Events.Add(new BehaviorEventViewModel(model));
    }

    public void RemoveEvent(BehaviorEventViewModel vm)
    {
        _definition.Events.Remove(vm.ToModel());
        Events.Remove(vm);
    }

    // ── Export ───────────────────────────────────────────────────────────
    public DesignerItemDefinition ToDefinition() => _definition;

    // 嚙緩嚙緩 嚙踝蕭雃W嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string DisplayName => ItemType switch
    {
        "PlcLabel" => $"PlcLabel [{Address}]",
        "PlcText" => $"PlcText [{Address}]",
        "PlcStatusIndicator" => $"StatusIndicator [{DisplayAddress}]",
        "SecuredButton" => $"Button [{Label}]",
        "Spacer" => "Spacer",
        "StaticLabel" => $"StaticLabel [{StaticText}]",
        _ => ItemType
    };

    // 嚙緩嚙緩 嚙箠嚙踝蕭嚙豎性列嚙踝蕭嚙稽嚙踝蕭 UI 嚙諸考） 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public static readonly string[] ColorThemes =
        ["NeonBlue", "NeonGreen", "NeonRed", "White", "Gray", "Warning", "Error", "Success", "Info", "Primary", "Default"];

    public static readonly string[] FrameShapes = ["Rectangle", "Circle"];
    public static readonly string[] StringFormats = ["F0", "F1", "F2", "F3"];
    public static readonly string[] ButtonThemes = ["Primary", "Success", "Danger", "Warning"];
    public static readonly string[] AccessLevels = ["Operator", "Instructor", "Supervisor", "Admin", "SuperAdmin"];
}
