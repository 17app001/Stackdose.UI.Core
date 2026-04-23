using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 文件 metadata
/// </summary>
public sealed class DesignMeta
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Machine Page";

    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "M1";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("plcIp")]
    public string PlcIp { get; set; } = string.Empty;

    [JsonPropertyName("plcPort")]
    public int PlcPort { get; set; } = 3000;

    [JsonPropertyName("scanInterval")]
    public int ScanInterval { get; set; } = 200;
}
