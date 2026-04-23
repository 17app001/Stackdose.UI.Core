using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 設定機器全域狀態。<br/>
/// <c>value</c>：狀態名稱（支援 {value}）。<br/>
/// 目前透過 <see cref="ComplianceContext.LogSystem"/> 記錄狀態變更；
/// B6/B7 完成機器狀態管理後可擴充為更新 MachineState。
/// </summary>
public sealed class SetStatusHandler : IBehaviorActionHandler
{
    public string ActionType => "SetStatus";

    public void Execute(BehaviorActionContext ctx)
    {
        var status = ctx.Interpolate(ctx.Action.Value);
        if (string.IsNullOrEmpty(status)) return;

        // 記錄狀態變更（稽核日誌）
        var message = $"[SetStatus] 機器狀態設定為：{status}（by behavior engine, controlId={ctx.ControlId}）";
        if (ctx.AuditLogger is not null)
            ctx.AuditLogger(message);
        else
            ComplianceContext.LogSystem(message, LogLevel.Info, showInUi: false);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[SetStatusHandler] {message}");
#endif
    }
}
