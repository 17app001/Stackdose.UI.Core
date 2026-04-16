using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 單一頁面資料模型（多頁面設計的基本單元）
/// </summary>
public sealed class DesignPage
{
    [JsonPropertyName("pageId")]
    public string PageId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Page 1";

    [JsonPropertyName("canvasWidth")]
    public double CanvasWidth { get; set; } = 1200;

    [JsonPropertyName("canvasHeight")]
    public double CanvasHeight { get; set; } = 750;

    [JsonPropertyName("canvasItems")]
    public List<DesignerItemDefinition> CanvasItems { get; set; } = [];
}
