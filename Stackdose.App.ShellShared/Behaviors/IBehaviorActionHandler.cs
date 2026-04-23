namespace Stackdose.App.ShellShared.Behaviors;

/// <summary>
/// Behavior Engine 的動作處理器介面。
/// 內建處理器（SetProp / WritePlc / LogAudit 等）與自訂處理器（CloseValve 等）
/// 都實作此介面，並向 <see cref="BehaviorEngine"/> 註冊。
/// </summary>
public interface IBehaviorActionHandler
{
    /// <summary>
    /// 動作類型識別名稱（對應 BehaviorAction.Action 欄位，比對時忽略大小寫）。
    /// </summary>
    string ActionType { get; }

    /// <summary>
    /// 執行動作。若需要操作 WPF 元素，呼叫方已保證在 UI 執行緒。
    /// </summary>
    void Execute(BehaviorActionContext ctx);
}
