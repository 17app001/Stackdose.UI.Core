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

    /// <summary>
    /// 自由畫布元件清單（含 x/y/width/height）
    /// </summary>
    [JsonPropertyName("canvasItems")]
    public List<DesignerItemDefinition> CanvasItems { get; set; } = [];

    /// <summary>畫布邏輯寬度（px），預設 1200</summary>
    [JsonPropertyName("canvasWidth")]
    public double CanvasWidth { get; set; } = 1200;

    /// <summary>畫布邏輯高度（px），預設 750</summary>
    [JsonPropertyName("canvasHeight")]
    public double CanvasHeight { get; set; } = 750;
}
