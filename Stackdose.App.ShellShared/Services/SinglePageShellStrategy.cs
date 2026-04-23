using System.Windows;
using Stackdose.UI.Templates.Shell;

namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// SinglePage 策略：將畫布包裝進 SinglePageContainer（僅有 AppHeader，無 LeftNav）。
/// 適合單一操作頁面、Kiosk 模式或無需頁面切換的機台介面。
/// </summary>
public sealed class SinglePageShellStrategy : IShellStrategy
{
    public string LayoutMode => "SinglePage";

    public UIElement Wrap(UIElement canvasViewport, string pageTitle, string deviceName)
    {
        return new SinglePageContainer
        {
            PageTitle        = pageTitle,
            HeaderDeviceName = string.IsNullOrWhiteSpace(deviceName) ? "DEVICE" : deviceName,
            ShellContent     = canvasViewport,
        };
    }
}
