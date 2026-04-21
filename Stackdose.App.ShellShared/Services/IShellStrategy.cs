using System.Windows;

namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// Shell 外殼策略介面。
/// 決定如何將自由畫布（Canvas viewport）包裝成對應的 Shell 容器。
/// </summary>
/// <remarks>
/// 主旨：讓設備廠商不必懂 XAML，只在 .machinedesign.json 設定 shellMode 即可選擇 Shell 外觀。
/// FreeCanvas → 裸畫布（DesignRuntime 預設）
/// SinglePage  → 帶 Header 的單頁容器（SinglePageContainer）
/// Standard    → 完整 Shell（MainContainer：Header + LeftNav + BottomBar），詳見 B7
/// </remarks>
public interface IShellStrategy
{
    /// <summary>策略識別名稱，對應 DesignDocument.ShellMode</summary>
    string LayoutMode { get; }

    /// <summary>
    /// 將畫布視圖（ScrollViewer + Canvas）包裝成目標 Shell 容器。
    /// </summary>
    /// <param name="canvasViewport">已含控制項的 ScrollViewer（或裸畫布）</param>
    /// <param name="pageTitle">頁面標題（來自 DesignMeta.Title）</param>
    /// <param name="deviceName">設備名稱（來自 DesignMeta.MachineId）</param>
    /// <returns>可直接放入 Window 的 UIElement</returns>
    UIElement Wrap(UIElement canvasViewport, string pageTitle, string deviceName);
}
