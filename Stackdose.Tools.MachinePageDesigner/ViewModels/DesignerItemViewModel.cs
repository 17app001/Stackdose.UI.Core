using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 單一設計項目的 ViewModel，封裝 DesignerItemDefinition 並處理屬性同步。
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

    // -- Identity -----------------------------------------------------------
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

    // -- Props 存取 ---------------------------------------------------------
    public Dictionary<string, object?> Props => _definition.Props;

    public string GetProp(string key, string fallback = "")
        => _definition.Props.GetString(key, fallback);

    public double GetPropDouble(string key, double fallback = 0)
        => _definition.Props.GetDouble(key, fallback);

    public event Action<string, object?, object?>? PropCommitted;

    public void SetProp(string key, object? value)
    {
        _definition.Props[key] = value;
        N(nameof(Props));
        RefreshPreview();
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
        var propName = key switch
        {
            "label" => nameof(Label),
            "address" => nameof(Address),
            "defaultValue" => nameof(DefaultValue),
            "dataType" => nameof(DataType),
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
            "viewerTitle" => nameof(ViewerTitle),
            "configFile" => nameof(ConfigFile),
            "defaultShowActiveOnly" => nameof(DefaultShowActiveOnly),
            "enableGrouping" => nameof(EnableGrouping),
            "processState" => nameof(ProcessState),
            "headName" => nameof(HeadName),
            "headIndex" => nameof(HeadIndex),
            "autoConnect" => nameof(AutoConnect),
            "tabs" => nameof(TabTitles),
            _ => null
        };
        if (propName != null) N(propName);
    }

    // -- 屬性包裝 -----------------------------------------------------------

    public string Label
    {
        get => GetProp("label", ItemType);
        set { var old = GetProp("label", ""); if (old == value) return; SetPropDirect("label", value); PropCommitted?.Invoke("label", old, value); }
    }

    public string ProcessState
    {
        get => GetProp("processState", "Running");
        set { var old = GetProp("processState", ""); if (old == value) return; SetPropDirect("processState", value); PropCommitted?.Invoke("processState", old, value); }
    }

    public string Address
    {
        get => GetProp("address");
        set { var old = GetProp("address"); if (old == value) return; SetPropDirect("address", value); N(nameof(DisplayName)); PropCommitted?.Invoke("address", old, value); }
    }

    public string DefaultValue
    {
        get => GetProp("defaultValue", "0");
        set { var old = GetProp("defaultValue", "0"); if (old == value) return; SetPropDirect("defaultValue", value); PropCommitted?.Invoke("defaultValue", old, value); }
    }

    public string DataType
    {
        get => GetProp("dataType", "Word");
        set { var old = GetProp("dataType", "Word"); if (old == value) return; SetPropDirect("dataType", value); PropCommitted?.Invoke("dataType", old, value); }
    }

    public double ValueFontSize
    {
        get => GetPropDouble("valueFontSize", 20);
        set { var old = GetPropDouble("valueFontSize", 20); if (old == value) return; SetPropDirect("valueFontSize", value); PropCommitted?.Invoke("valueFontSize", old, value); }
    }

    public string FrameShape
    {
        get => GetProp("frameShape", "Rectangle");
        set { var old = GetProp("frameShape", "Rectangle"); if (old == value) return; SetPropDirect("frameShape", value); PropCommitted?.Invoke("frameShape", old, value); }
    }

    public string ValueColorTheme
    {
        get => GetProp("valueColorTheme", "NeonBlue");
        set { var old = GetProp("valueColorTheme", "NeonBlue"); if (old == value) return; SetPropDirect("valueColorTheme", value); PropCommitted?.Invoke("valueColorTheme", old, value); }
    }

    public double Divisor
    {
        get => GetPropDouble("divisor", 1);
        set { var old = GetPropDouble("divisor", 1); if (old == value) return; SetPropDirect("divisor", value); PropCommitted?.Invoke("divisor", old, value); }
    }

    public string StringFormat
    {
        get => GetProp("stringFormat", "F0");
        set { var old = GetProp("stringFormat", "F0"); if (old == value) return; SetPropDirect("stringFormat", value); PropCommitted?.Invoke("stringFormat", old, value); }
    }

    public string DisplayAddress
    {
        get => GetProp("displayAddress");
        set { var old = GetProp("displayAddress"); if (old == value) return; SetPropDirect("displayAddress", value); N(nameof(DisplayName)); PropCommitted?.Invoke("displayAddress", old, value); }
    }

    public string CommandAddress
    {
        get => GetProp("commandAddress");
        set { var old = GetProp("commandAddress"); if (old == value) return; SetPropDirect("commandAddress", value); PropCommitted?.Invoke("commandAddress", old, value); }
    }

    public string RequiredLevel
    {
        get => GetProp("requiredLevel", "Operator");
        set { var old = GetProp("requiredLevel", "Operator"); if (old == value) return; SetPropDirect("requiredLevel", value); PropCommitted?.Invoke("requiredLevel", old, value); }
    }

    public string Theme
    {
        get => GetProp("theme", "Primary");
        set { var old = GetProp("theme", "Primary"); if (old == value) return; SetPropDirect("theme", value); PropCommitted?.Invoke("theme", old, value); }
    }

    public string GroupTitle
    {
        get => GetProp("title", "Group");
        set { var old = GetProp("title", "Group"); if (old == value) return; SetPropDirect("title", value); PropCommitted?.Invoke("title", old, value); }
    }

    public int GridColumns
    {
        get => (int)Math.Max(1, GetPropDouble("gridColumns", 2));
        set { var v = Math.Max(1, value); var old = (int)Math.Max(1, GetPropDouble("gridColumns", 2)); if (old == v) return; _definition.Props["gridColumns"] = (double)v; N(); }
    }

    public double LabelFontSize
    {
        get => GetPropDouble("labelFontSize", 12);
        set { var old = GetPropDouble("labelFontSize", 12); if (old == value) return; SetPropDirect("labelFontSize", value); PropCommitted?.Invoke("labelFontSize", old, value); }
    }

    public string LabelAlignment
    {
        get => GetProp("labelAlignment", "Left");
        set { var old = GetProp("labelAlignment", "Left"); if (old == value) return; SetPropDirect("labelAlignment", value); PropCommitted?.Invoke("labelAlignment", old, value); }
    }

    public string ValueAlignment
    {
        get => GetProp("valueAlignment", "Right");
        set { var old = GetProp("valueAlignment", "Right"); if (old == value) return; SetPropDirect("valueAlignment", value); PropCommitted?.Invoke("valueAlignment", old, value); }
    }

    public string LabelForeground
    {
        get => GetProp("labelForeground", "Default");
        set { var old = GetProp("labelForeground", "Default"); if (old == value) return; SetPropDirect("labelForeground", value); PropCommitted?.Invoke("labelForeground", old, value); }
    }

    public string FrameBackground
    {
        get => GetProp("frameBackground", "DarkBlue");
        set { var old = GetProp("frameBackground", "DarkBlue"); if (old == value) return; SetPropDirect("frameBackground", value); PropCommitted?.Invoke("frameBackground", old, value); }
    }

    public string StaticText
    {
        get => GetProp("staticText", "Label");
        set { var old = GetProp("staticText", "Label"); if (old == value) return; SetPropDirect("staticText", value); N(nameof(DisplayName)); PropCommitted?.Invoke("staticText", old, value); }
    }

    public double StaticFontSize
    {
        get => GetPropDouble("staticFontSize", 16);
        set { var old = GetPropDouble("staticFontSize", 16); if (old == value) return; SetPropDirect("staticFontSize", value); PropCommitted?.Invoke("staticFontSize", old, value); }
    }

    public string StaticFontWeight
    {
        get => GetProp("staticFontWeight", "Normal");
        set { var old = GetProp("staticFontWeight", "Normal"); if (old == value) return; SetPropDirect("staticFontWeight", value); PropCommitted?.Invoke("staticFontWeight", old, value); }
    }

    public string StaticTextAlign
    {
        get => GetProp("staticTextAlign", "Left");
        set { var old = GetProp("staticTextAlign", "Left"); if (old == value) return; SetPropDirect("staticTextAlign", value); PropCommitted?.Invoke("staticTextAlign", old, value); }
    }

    public string StaticForeground
    {
        get => GetProp("staticForeground", "#E2E2F0");
        set { var old = GetProp("staticForeground", "#E2E2F0"); if (old == value) return; SetPropDirect("staticForeground", value); PropCommitted?.Invoke("staticForeground", old, value); }
    }

    public bool EnableLiveRecord
    {
        get => _definition.Props.GetBool("enableLiveRecord", true);
        set { var old = _definition.Props.GetBool("enableLiveRecord", true); if (old == value) return; SetPropDirect("enableLiveRecord", value); PropCommitted?.Invoke("enableLiveRecord", old, value); }
    }

    public string ViewerTitle
    {
        get => GetProp("viewerTitle", "");
        set { var old = GetProp("viewerTitle", ""); if (old == value) return; SetPropDirect("viewerTitle", value); PropCommitted?.Invoke("viewerTitle", old, value); }
    }

    public string ConfigFile
    {
        get => GetProp("configFile", "");
        set { var old = GetProp("configFile", ""); if (old == value) return; SetPropDirect("configFile", value); PropCommitted?.Invoke("configFile", old, value); }
    }

    public bool DefaultShowActiveOnly
    {
        get => _definition.Props.GetBool("defaultShowActiveOnly", false);
        set { var old = _definition.Props.GetBool("defaultShowActiveOnly", false); if (old == value) return; SetPropDirect("defaultShowActiveOnly", value); PropCommitted?.Invoke("defaultShowActiveOnly", old, value); }
    }

    public bool EnableGrouping
    {
        get => _definition.Props.GetBool("enableGrouping", false);
        set { var old = _definition.Props.GetBool("enableGrouping", false); if (old == value) return; SetPropDirect("enableGrouping", value); PropCommitted?.Invoke("enableGrouping", old, value); }
    }

    // -- PrintHead ----------------------------------------------------------

    public string HeadName
    {
        get => GetProp("headName", "PrintHead 1");
        set { var old = GetProp("headName", "PrintHead 1"); if (old == value) return; SetPropDirect("headName", value); PropCommitted?.Invoke("headName", old, value); }
    }

    public int HeadIndex
    {
        get => (int)GetPropDouble("headIndex", 0);
        set { var old = (int)GetPropDouble("headIndex", 0); if (old == value) return; SetPropDirect("headIndex", value); PropCommitted?.Invoke("headIndex", old, value); }
    }

    public bool AutoConnect
    {
        get => _definition.Props.GetBool("autoConnect", false);
        set { var old = _definition.Props.GetBool("autoConnect", false); if (old == value) return; SetPropDirect("autoConnect", value); PropCommitted?.Invoke("autoConnect", old, value); }
    }

    // -- TabPanel -----------------------------------------------------------

    /// <summary>
    /// Tab 標題（逗號分隔），供屬性面板顯示/編輯。
    /// 寫入時更新 props["tabs"] 中每個 entry 的 title，不破壞 items 陣列。
    /// </summary>
    public string TabTitles
    {
        get
        {
            if (!_definition.Props.TryGetValue("tabs", out var raw)) return "";
            try
            {
                var je = raw is JsonElement j ? j : JsonSerializer.SerializeToElement(raw);
                var titles = je.EnumerateArray()
                    .Select(e => e.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "")
                    .ToArray();
                return string.Join(", ", titles);
            }
            catch { return ""; }
        }
        set
        {
            var titles = value.Split(',').Select(s => s.Trim()).ToArray();

            // Deserialize existing tabs to preserve items
            TabEntryJson[]? existing = null;
            if (_definition.Props.TryGetValue("tabs", out var raw))
            {
                try
                {
                    var je = raw is JsonElement j ? j : JsonSerializer.SerializeToElement(raw);
                    existing = JsonSerializer.Deserialize<TabEntryJson[]>(je.GetRawText());
                }
                catch { }
            }

            var updated = titles.Select((t, i) => new TabEntryJson
            {
                title = t,
                items = existing != null && i < existing.Length ? existing[i].items : []
            }).ToArray();

            _definition.Props["tabs"] = JsonSerializer.SerializeToElement(updated);
            N(nameof(TabTitles));
            RefreshPreview();
        }
    }

    private sealed class TabEntryJson
    {
        public string? title { get; set; }
        public object[]? items { get; set; }
    }

    // -- 畫布幾何 -----------------------------------------------------------
    public double X { get => _definition.X; set { var v = Math.Max(0, value); if (_definition.X == v) return; var old = _definition.X; _definition.X = v; N(); PropCommitted?.Invoke("x", old, v); } }
    public double Y { get => _definition.Y; set { var v = Math.Max(0, value); if (_definition.Y == v) return; var old = _definition.Y; _definition.Y = v; N(); PropCommitted?.Invoke("y", old, v); } }
    public double Width { get => _definition.Width; set { var v = Math.Max(40, value); if (_definition.Width == v) return; var old = _definition.Width; _definition.Width = v; N(); PropCommitted?.Invoke("width", old, v); } }
    public double Height { get => _definition.Height; set { var v = Math.Max(30, value); if (_definition.Height == v) return; var old = _definition.Height; _definition.Height = v; N(); PropCommitted?.Invoke("height", old, v); } }

    public UIElement? Preview { get => _preview; private set => Set(ref _preview, value); }

    public void RefreshPreview() { Preview = DesignTimeControlFactory.Create(_definition); }

    public ObservableCollection<BehaviorEventViewModel> Events => _events ??= BuildEventsCollection();

    private ObservableCollection<BehaviorEventViewModel> BuildEventsCollection()
    {
        var col = new ObservableCollection<BehaviorEventViewModel>(_definition.Events.Select(e => new BehaviorEventViewModel(e)));
        col.CollectionChanged += (_, _) => { _definition.Events.Clear(); foreach (var vm in col) _definition.Events.Add(vm.ToModel()); };
        return col;
    }

    public void AddEvent() { var m = new BehaviorEvent { On = "valueChanged" }; _definition.Events.Add(m); Events.Add(new BehaviorEventViewModel(m)); }
    public void RemoveEvent(BehaviorEventViewModel vm) { _definition.Events.Remove(vm.ToModel()); Events.Remove(vm); }

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

    public DesignerItemDefinition ToDefinition() => _definition;

    public static readonly string[] ColorThemes = ["NeonBlue", "NeonGreen", "NeonRed", "White", "Gray", "Warning", "Error", "Success", "Info", "Primary", "Default"];
    public static readonly string[] FrameShapes = ["Rectangle", "Circle"];
    public static readonly string[] StringFormats = ["F0", "F1", "F2", "F3"];
    public static readonly string[] DataTypes = ["Word", "DWord", "Float", "Bit"];
    public static readonly string[] ButtonThemes = ["Normal", "Primary", "Success", "Warning", "Error", "Info"];
    public static readonly string[] AccessLevels = ["Operator", "Instructor", "Supervisor", "Admin", "SuperAdmin"];
}
