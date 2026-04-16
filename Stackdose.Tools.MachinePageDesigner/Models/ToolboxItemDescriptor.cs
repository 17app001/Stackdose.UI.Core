namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// Toolbox item descriptor
/// </summary>
public sealed class ToolboxItemDescriptor
{
    public required string Type { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }
    public string Icon { get; init; } = "#";

    /// <summary>
    /// Create DesignerItemDefinition with default props
    /// </summary>
    public DesignerItemDefinition CreateDefinition() => Type switch
    {
        "PlcLabel" => new DesignerItemDefinition
        {
            Type = "PlcLabel",
            Props = new()
            {
                ["label"] = "Label",
                ["address"] = "D100",
                ["defaultValue"] = "0",
                ["valueFontSize"] = 20.0,
                ["frameShape"] = "Rectangle",
                ["valueColorTheme"] = "NeonBlue",
                ["divisor"] = 1.0,
                ["stringFormat"] = "F0"
            }
        },
        "PlcText" => new DesignerItemDefinition
        {
            Type = "PlcText",
            Props = new()
            {
                ["label"] = "Parameter",
                ["address"] = "D100"
            }
        },
        "PlcStatusIndicator" => new DesignerItemDefinition
        {
            Type = "PlcStatusIndicator",
            Props = new()
            {
                ["label"]          = "Status",
                ["displayAddress"] = "M100"
            }
        },
        "SecuredButton" => new DesignerItemDefinition
        {
            Type = "SecuredButton",
            Props = new()
            {
                ["label"] = "Command",
                ["commandAddress"] = "M100",
                ["requiredLevel"] = "Operator",
                ["theme"] = "Primary",
                ["writeValue"] = "1",
                ["commandType"] = "write",
                ["pulseMs"] = 300.0
            }
        },
        "Spacer" => new DesignerItemDefinition
        {
            Type = "Spacer",
            Props = new() { ["title"] = "Group" },
            Width = 300, Height = 200
        },
        "LiveLog" => new DesignerItemDefinition
        {
            Type = "LiveLog",
            Props = [],
            Width = 420, Height = 200
        },
        "AlarmViewer" => new DesignerItemDefinition
        {
            Type = "AlarmViewer",
            Props = new() { ["configFile"] = "" },
            Width = 420, Height = 280
        },
        "SensorViewer" => new DesignerItemDefinition
        {
            Type = "SensorViewer",
            Props = new() { ["configFile"] = "" },
            Width = 420, Height = 200
        },
        "StaticLabel" => new DesignerItemDefinition
        {
            Type = "StaticLabel",
            Props = new()
            {
                ["staticText"]       = "標題文字",
                ["staticFontSize"]   = 16.0,
                ["staticFontWeight"] = "Normal",
                ["staticTextAlign"]  = "Left",
                ["staticForeground"] = "#E2E2F0"
            },
            Width = 200, Height = 40
        },
        _ => new DesignerItemDefinition { Type = Type }
    };

    public static IReadOnlyList<ToolboxItemDescriptor> All { get; } =
    [
        new() { Type = "PlcLabel",           DisplayName = "PlcLabel",           Category = "PLC",    Icon = "\u25C8" },
        new() { Type = "PlcText",            DisplayName = "PlcText",            Category = "PLC",    Icon = "\u270E" },
        new() { Type = "PlcStatusIndicator", DisplayName = "PlcStatusIndicator", Category = "PLC",    Icon = "\u25CF" },
        new() { Type = "SecuredButton",      DisplayName = "SecuredButton",      Category = "Button", Icon = "\u25A3" },
        new() { Type = "Spacer",             DisplayName = "GroupBox",           Category = "Layout", Icon = "\u25A1" },
        new() { Type = "LiveLog",            DisplayName = "LiveLog",            Category = "Viewer", Icon = "\u2637" },
        new() { Type = "AlarmViewer",        DisplayName = "AlarmViewer",        Category = "Viewer", Icon = "\u26A0" },
        new() { Type = "SensorViewer",       DisplayName = "SensorViewer",       Category = "Viewer", Icon = "\u26A1" },
        new() { Type = "StaticLabel",        DisplayName = "StaticLabel",        Category = "Layout", Icon = "T" },
    ];
}