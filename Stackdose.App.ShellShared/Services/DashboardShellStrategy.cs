using System.Windows;

namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// Dashboard 策略：精簡生產模式，不包裝 Shell Chrome。
/// 畫布 1:1 填滿視窗，標題列保留，開發面板由 DesignRuntime 在此策略下自行隱藏。
/// PLC 連線由 DesignMeta.PlcIp / PlcPort 自動建立，不需手動輸入。
/// </summary>
public sealed class DashboardShellStrategy : IShellStrategy
{
    public string LayoutMode => "Dashboard";

    public UIElement Wrap(UIElement canvasViewport, string pageTitle, string deviceName)
        => canvasViewport;
}
