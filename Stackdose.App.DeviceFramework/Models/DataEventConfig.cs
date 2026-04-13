namespace Stackdose.App.DeviceFramework.Models;

public sealed class DataEventConfig
{
    public string Name      { get; set; } = string.Empty;  // handler method name, e.g. "OnMachineStarted"
    public string Address   { get; set; } = string.Empty;  // PLC address, e.g. "M200", "D400"
    public string Trigger   { get; set; } = "changed";     // risingEdge/fallingEdge/above/below/equals/changed
    public int    Threshold { get; set; } = 0;             // used by above/below/equals
    public string DataType  { get; set; } = string.Empty;  // "bit"/"word", empty = auto-detect from address prefix
}
