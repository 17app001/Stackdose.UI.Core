using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 根資料模型（對應 JSON root）— .machinedesign.json
/// </summary>
public sealed class DesignDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("meta")]
    public DesignMeta Meta { get; set; } = new();

    [JsonPropertyName("layout")]
    public PageLayoutConfig Layout { get; set; } = new();

    [JsonPropertyName("zones")]
    public Dictionary<string, ZoneDefinition> Zones { get; set; } = new()
    {
        ["liveData"] = new ZoneDefinition { Title = "Live Data", Columns = 2 },
        ["deviceStatus"] = new ZoneDefinition { Title = "Device Status", Columns = 2 }
    };
}
