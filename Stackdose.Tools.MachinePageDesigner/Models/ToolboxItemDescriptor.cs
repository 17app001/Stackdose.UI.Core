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
                ["requiredLevel"] = "Operator",
                ["theme"] = "Primary"
            }
        },
        "Spacer" => new DesignerItemDefinition
        {
            Type = "Spacer",
            Props = []
        },
        _ => new DesignerItemDefinition { Type = Type }
    };

    public static IReadOnlyList<ToolboxItemDescriptor> All { get; } =
    [
        new() { Type = "PlcLabel",            DisplayName = "PlcLabel",            Category = "PLC",  Icon = "\u25C8" },
        new() { Type = "PlcText",             DisplayName = "PlcText",             Category = "PLC",  Icon = "\u270E" },
        new() { Type = "PlcStatusIndicator",  DisplayName = "PlcStatusIndicator",  Category = "PLC",  Icon = "\u25CF" },
        new() { Type = "SecuredButton",       DisplayName = "SecuredButton",       Category = "Button", Icon = "\u25A3" },
        new() { Type = "Spacer",              DisplayName = "Spacer",              Category = "Layout", Icon = "\u25A1" },
    ];
}