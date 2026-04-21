using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// Standard 模式下的單一頁面定義。
/// DesignDocument.Pages[] 中的每一項代表 LeftNav 的一個頁面。
/// </summary>
public sealed class PageDefinition
{
    [JsonPropertyName("pageId")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Title { get; set; } = "";

    [JsonPropertyName("canvasWidth")]
    public double CanvasWidth { get; set; } = 1200;

    [JsonPropertyName("canvasHeight")]
    public double CanvasHeight { get; set; } = 750;

    [JsonPropertyName("canvasItems")]
    public List<DesignerItemDefinition> CanvasItems { get; set; } = [];
}
