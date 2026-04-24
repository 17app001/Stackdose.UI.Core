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
                ["authLevel"] = "Operator",
                ["theme"] = "Primary"
            },
            Width = 180, Height = 48
        },
        "StaticLabel" => new DesignerItemDefinition
        {
            Type = "StaticLabel",
            Props = new()
            {
                ["staticText"] = "Label",
                ["staticFontSize"] = 16.0,
                ["staticFontWeight"] = "Normal",
                ["staticTextAlign"] = "Left",
                ["staticForeground"] = "#E2E2F0"
            },
            Width = 200, Height = 36
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
            Props = [],
            Width = 420, Height = 280
        },
        "SensorViewer" => new DesignerItemDefinition
        {
            Type = "SensorViewer",
            Props = [],
            Width = 420, Height = 200
        },
        "ProcessStatusIndicator" => new DesignerItemDefinition
        {
            Type = "ProcessStatusIndicator",
            Props = new()
            {
                ["processState"] = "Running",
                ["label"] = "System Running"
            },
            Width = 120, Height = 120
        },
        "SystemClock" => new DesignerItemDefinition
        {
            Type = "SystemClock",
            Props = [],
            Width = 150, Height = 40
        },
        "PlcDeviceEditor" => new DesignerItemDefinition
        {
            Type = "PlcDeviceEditor",
            Props = new()
            {
                ["address"] = "D100"
            },
            Width = 300, Height = 160
        },
        "PrintHeadStatus" => new DesignerItemDefinition
        {
            Type = "PrintHeadStatus",
            Props = new()
            {
                ["configFile"] = "Config/feiyang_head1.json",
                ["headName"] = "PrintHead 1",
                ["headIndex"] = 0,
                ["autoConnect"] = false
            },
            Width = 320, Height = 100
        },
        "PrintHeadController" => new DesignerItemDefinition
        {
            Type = "PrintHeadController",
            Props = [],
            Width = 700, Height = 300
        },
        _ => new DesignerItemDefinition { Type = Type }
    };

    public static IReadOnlyList<ToolboxItemDescriptor> All { get; } =
    [
        new() { Type = "PlcLabel",           DisplayName = "PlcLabel",           Category = "PLC",    Icon = "\u25C8" },
        new() { Type = "PlcText",            DisplayName = "PlcText",            Category = "PLC",    Icon = "\u270E" },
        new() { Type = "PlcStatusIndicator", DisplayName = "PlcStatusIndicator", Category = "PLC",    Icon = "\u25CF" },
        new() { Type = "PlcDeviceEditor",    DisplayName = "PlcDeviceEditor",    Category = "PLC",    Icon = "\u2699" },
        new() { Type = "SecuredButton",      DisplayName = "SecuredButton",      Category = "Button", Icon = "\u25A3" },
        new() { Type = "PrintHeadStatus",    DisplayName = "PrintHeadStatus",    Category = "Hardware", Icon = "\u2631" },
        new() { Type = "PrintHeadController", DisplayName = "PrintHeadController", Category = "Hardware", Icon = "\u2699" },
        new() { Type = "Spacer",             DisplayName = "Spacer",             Category = "Layout", Icon = "\u25A1" },
        new() { Type = "StaticLabel",        DisplayName = "StaticLabel",        Category = "Layout", Icon = "T" },
        new() { Type = "SystemClock",        DisplayName = "SystemClock",        Category = "Layout", Icon = "\u23F0" },
        new() { Type = "ProcessStatusIndicator", DisplayName = "ProcessStatus",  Category = "Status", Icon = "\u231B" },
        new() { Type = "LiveLog",            DisplayName = "LiveLog",            Category = "Viewer", Icon = "\u2637" },
        new() { Type = "AlarmViewer",        DisplayName = "AlarmViewer",        Category = "Viewer", Icon = "\u26A0" },
        new() { Type = "SensorViewer",       DisplayName = "SensorViewer",       Category = "Viewer", Icon = "\u26A1" },
    ];
}
