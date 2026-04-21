using System.Windows;

namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// FreeCanvas 策略：不包裝任何 Shell，直接回傳原始畫布視圖。
/// 對應 DesignRuntime 預設行為，保持現有 ScrollViewer + Canvas 結構不變。
/// </summary>
public sealed class FreeCanvasShellStrategy : IShellStrategy
{
    public string LayoutMode => "FreeCanvas";

    public UIElement Wrap(UIElement canvasViewport, string pageTitle, string deviceName)
        => canvasViewport;
}
