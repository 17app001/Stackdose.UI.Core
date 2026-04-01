using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 版型設定（mode / widths / toggles）
/// </summary>
public sealed class PageLayoutConfig
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "SplitRight";

    [JsonPropertyName("leftCommandWidthPx")]
    public int LeftCommandWidthPx { get; set; } = 250;

    [JsonPropertyName("rightColumnWidthStar")]
    public double RightColumnWidthStar { get; set; } = 0.85;

    [JsonPropertyName("showLiveLog")]
    public bool ShowLiveLog { get; set; } = true;

    [JsonPropertyName("showAlarmViewer")]
    public bool ShowAlarmViewer { get; set; } = true;

    [JsonPropertyName("showSensorViewer")]
    public bool ShowSensorViewer { get; set; }
}
