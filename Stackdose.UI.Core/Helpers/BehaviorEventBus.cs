using System.Windows;

namespace Stackdose.UI.Core.Helpers;

/// <summary>
/// 輕量事件匯流排，讓 UI.Core 控制項（SecuredButton 等）
/// 能發出行為事件而不需直接依賴 ShellShared/BehaviorEngine。
/// BehaviorEngine 訂閱此匯流排後即可接收所有控制項事件。
/// </summary>
public static class BehaviorEventBus
{
    /// <summary>
    /// 控制項事件發布（controlId, eventOn, triggerValue）。<br/>
    /// 在 UI 執行緒上觸發，subscriber 可直接操作 WPF 物件。
    /// </summary>
    public static event Action<string, string, double>? ControlEventFired;

    /// <summary>
    /// 由控制項呼叫以發布事件（自動切換至 UI 執行緒）。
    /// </summary>
    /// <param name="controlId">DesignerItemDefinition.Id</param>
    /// <param name="eventOn">事件名稱：click / connected / disconnected</param>
    /// <param name="triggerValue">數值（click 傳 0）</param>
    public static void Fire(string controlId, string eventOn, double triggerValue = 0)
    {
        if (string.IsNullOrEmpty(controlId)) return;
        Application.Current?.Dispatcher.BeginInvoke(
            () => ControlEventFired?.Invoke(controlId, eventOn, triggerValue));
    }
}
