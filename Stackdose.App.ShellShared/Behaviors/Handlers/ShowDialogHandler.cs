using Stackdose.UI.Core.Controls;

namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 顯示 CyberMessageBox 對話框。<br/>
/// <c>title</c>：標題（支援 {value}）<br/>
/// <c>message</c>：訊息內文（支援 {value}）
/// </summary>
public sealed class ShowDialogHandler : IBehaviorActionHandler
{
    public string ActionType => "ShowDialog";

    public void Execute(BehaviorActionContext ctx)
    {
        var title   = ctx.Interpolate(ctx.Action.Title)   is { Length: > 0 } t ? t : "提示";
        var message = ctx.Interpolate(ctx.Action.Message) is { Length: > 0 } m ? m : "";
        if (string.IsNullOrEmpty(message)) return;

        CyberMessageBox.Show(message, title);
    }
}
