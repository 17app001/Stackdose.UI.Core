using System.Collections.ObjectModel;
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

    // 嚙緩嚙緩 Props 嚙編嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public Dictionary<string, object?> Props => _definition.Props;

    public string GetProp(string key, string fallback = "")
        => _definition.Props.GetString(key, fallback);

    public double GetPropDouble(string key, double fallback = 0)
        => _definition.Props.GetDouble(key, fallback);

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
        _definition.Props[key] = value;
        N(nameof(Props));
        // ????惇?批?蝔曹誑?湔 UI 蝬?
        NotifyPropKey(key);
        RefreshPreview();
    }

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
            _ => null
        };
        if (propName != null) N(propName);
    }

    // 嚙緩嚙緩 嚙窯嚙踝蕭嚙豎性快梧蕭嚙編嚙踝蕭 嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩嚙緩
    public string Label
    {
        get => GetProp("label", ItemType);
        set { SetProp("label", value); N(); }
    }

    public string Address
    {
        get => GetProp("address");
        set { SetProp("address", value); N(); }
    }

    public string DefaultValue
    {
        get => GetProp("defaultValue", "0");
        set { SetProp("defaultValue", value); N(); }
    }

    public double ValueFontSize
    {
        get => GetPropDouble("valueFontSize", 20);
        set { SetProp("valueFontSize", value); N(); }
    }

    public string FrameShape
    {
        get => GetProp("frameShape", "Rectangle");
        set { SetProp("frameShape", value); N(); }
    }

    public string ValueColorTheme
    {
        get => GetProp("valueColorTheme", "NeonBlue");
        set { SetProp("valueColorTheme", value); N(); }
    }

    public double Divisor
    {
        get => GetPropDouble("divisor", 1);
        set { SetProp("divisor", value); N(); }
    }

    public string StringFormat
    {
        get => GetProp("stringFormat", "F0");
        set { SetProp("stringFormat", value); N(); }
    }

    public string DisplayAddress
    {
        get => GetProp("displayAddress");
        set { SetProp("displayAddress", value); N(); }
    }

    public string CommandAddress
    {
        get => GetProp("commandAddress");
        set { SetProp("commandAddress", value); N(); }
    }

    public string RequiredLevel
    {
        get => GetProp("requiredLevel", "Operator");
        set { SetProp("requiredLevel", value); N(); }
    }

    public string Theme
    {
        get => GetProp("theme", "Primary");
        set { SetProp("theme", value); N(); }
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
        "PlcLabel" => $"PlcLabel [{Address}]",
        "PlcText" => $"PlcText [{Address}]",
        "PlcStatusIndicator" => $"StatusIndicator [{DisplayAddress}]",
        "SecuredButton" => $"Button [{Label}]",
        "Spacer" => "Spacer",
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
