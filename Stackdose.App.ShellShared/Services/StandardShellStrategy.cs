using System.Windows;
using Stackdose.UI.Templates.Shell;

namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// Standard 策略：將畫布包裝進完整的 MainContainer
/// （AppHeader + LeftNav + Content + BottomBar）。
/// </summary>
/// <remarks>
/// B3 目標：顯示完整 Shell Chrome 作為預覽。
/// B7 將完成 Standard 模式的 LeftNav 頁面對應與 BuildNavFromPages 邏輯。
/// </remarks>
public sealed class StandardShellStrategy : IShellStrategy
{
    public string LayoutMode => "Standard";

    public UIElement Wrap(UIElement canvasViewport, string pageTitle, string deviceName)
    {
        return new MainContainer
        {
            PageTitle        = pageTitle,
            HeaderDeviceName = string.IsNullOrWhiteSpace(deviceName) ? "DEVICE" : deviceName,
            ShellContent     = canvasViewport,
        };
    }
}
