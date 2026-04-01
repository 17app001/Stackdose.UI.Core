using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 單一 Zone 設定
/// </summary>
public sealed class ZoneDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Zone";

    [JsonPropertyName("columns")]
    public int Columns { get; set; } = 2;

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 0;

    [JsonPropertyName("items")]
    public List<DesignerItemDefinition> Items { get; set; } = [];
}
