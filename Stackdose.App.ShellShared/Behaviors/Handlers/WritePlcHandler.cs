namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 非同步寫入 PLC 暫存器。<br/>
/// <c>target</c>：PLC 位址（如 "Y001"、"D100"）<br/>
/// <c>value</c>：寫入值字串（如 "0"、"1"、"{value}"）<br/>
/// 寫入格式："{address},{value}" — 與 IPlcManager.WriteAsync 相容。
/// </summary>
public sealed class WritePlcHandler : IBehaviorActionHandler
{
    public string ActionType => "WritePlc";

    public void Execute(BehaviorActionContext ctx)
    {
        var address = ctx.Action.Target;
        if (string.IsNullOrEmpty(address)) return;

        var mgr = ctx.PlcManager;
        if (mgr is null || !mgr.IsConnected)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[WritePlcHandler] PLC 未連線，跳過寫入");
#endif
            return;
        }

        var value = ctx.Interpolate(ctx.Action.Value);
        _ = mgr.WriteAsync($"{address},{value}");
    }
}
