namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 切換頁面（Standard Shell 導覽）。<br/>
/// <c>page</c>：目標頁面識別名稱。<br/>
/// 需由 DesignRuntime / App 注入 <see cref="BehaviorActionContext.Navigator"/> 委派。
/// B7 完成 Standard Shell 導覽接線後可完整使用。
/// </summary>
public sealed class NavigateHandler : IBehaviorActionHandler
{
    public string ActionType => "Navigate";

    public void Execute(BehaviorActionContext ctx)
    {
        var page = ctx.Action.Page;
        if (string.IsNullOrEmpty(page)) return;

        if (ctx.Navigator is null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[NavigateHandler] Navigator 未注入，無法導覽至 '{page}'（B7 接線後可用）");
#endif
            return;
        }

        ctx.Navigator(page);
    }
}
