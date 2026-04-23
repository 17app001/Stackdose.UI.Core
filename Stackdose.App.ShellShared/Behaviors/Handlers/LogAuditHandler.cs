using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 寫入 FDA 21 CFR Part 11 稽核日誌。<br/>
/// <c>message</c>：訊息內文（支援 {value}）
/// <para>
/// 優先使用 <see cref="BehaviorActionContext.AuditLogger"/> 委派（DesignRuntime 注入）；
/// 退而使用 <see cref="ComplianceContext.LogUser"/>（已完整的 App 環境）。
/// </para>
/// </summary>
public sealed class LogAuditHandler : IBehaviorActionHandler
{
    public string ActionType => "LogAudit";

    public void Execute(BehaviorActionContext ctx)
    {
        var message = ctx.Interpolate(ctx.Action.Message);
        if (string.IsNullOrEmpty(message)) return;

        if (ctx.AuditLogger is not null)
        {
            ctx.AuditLogger(message);
            return;
        }

        // 備援：使用全域 ComplianceContext
        ComplianceContext.LogSystem(message, LogLevel.Info);
    }
}
