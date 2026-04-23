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

    /// <summary>
    /// Shell 外殼模式：FreeCanvas（預設）/ SinglePage / Standard。<br/>
    /// 決定 DesignRuntime / DesignPlayer 載入此文件時使用哪種 Shell 策略包裝畫布。<br/>
    /// 注意：此值會與 Layout.Mode 同步（Dashboard 對應 FreeCanvas）。
    /// </summary>
    [JsonPropertyName("shellMode")]
    public string ShellMode
    {
        get => Layout.Mode;
        set => Layout.Mode = value;
    }

    /// <summary>
    /// Standard 模式多頁面定義。<br/>
    /// 非空時，DesignRuntime 以此建立 LeftNav 並忽略根層 canvasItems。
    /// 空清單時退回單頁模式（向後相容）。
    /// </summary>
    [JsonPropertyName("pages")]
    public List<PageDefinition> Pages { get; set; } = [];

    /// <summary>畫布邏輯寬度（px），預設 1200</summary>
    [JsonPropertyName("canvasWidth")]
    public double CanvasWidth { get; set; } = 1200;

    /// <summary>畫布邏輯高度（px），預設 750</summary>
    [JsonPropertyName("canvasHeight")]
    public double CanvasHeight { get; set; } = 750;
}
