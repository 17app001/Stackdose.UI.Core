using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 根資料模型（對應 JSON root）— .machinedesign.json
/// </summary>
public sealed class DesignDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("meta")]
    public DesignMeta Meta { get; set; } = new();

    [JsonPropertyName("layout")]
    public PageLayoutConfig Layout { get; set; } = new();

    /// <summary>
    /// 多頁面清單（v2.0+，DesignFileService.Load 保證此欄位一定有值）
    /// </summary>
    [JsonPropertyName("pages")]
    public List<DesignPage>? Pages { get; set; }

    // ── 下列欄位為向後相容（v1.0 格式 / Runtime 讀取第一頁用） ──────────

    /// <summary>第一頁元件清單（Runtime 相容用，Save 時同步自 Pages[0]）</summary>
    [JsonPropertyName("canvasItems")]
    public List<DesignerItemDefinition> CanvasItems { get; set; } = [];

    /// <summary>第一頁畫布寬度（Runtime 相容用）</summary>
    [JsonPropertyName("canvasWidth")]
    public double CanvasWidth { get; set; } = 1200;

    /// <summary>第一頁畫布高度（Runtime 相容用）</summary>
    [JsonPropertyName("canvasHeight")]
    public double CanvasHeight { get; set; } = 750;
}
