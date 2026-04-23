namespace Stackdose.App.ShellShared.Behaviors.Handlers;

/// <summary>
/// 修改目標控制項的 prop 值。<br/>
/// 使用 <see cref="ControlRuntimeTag.PropSetters"/> 字典執行更新，
/// 由 RuntimeControlFactory 在建立控制項時注入可設定的 prop setter。
/// <para>
/// 支援的 JSON 欄位：<c>target</c>（id 或 "self"）、<c>prop</c>、<c>value</c>（支援 {value}）
/// </para>
/// </summary>
public sealed class SetPropHandler : IBehaviorActionHandler
{
    public string ActionType => "SetProp";

    public void Execute(BehaviorActionContext ctx)
    {
        var propName = ctx.Action.Prop;
        if (string.IsNullOrEmpty(propName)) return;

        var control = ctx.ResolveTarget();
        if (control?.Tag is not ControlRuntimeTag tag) return;

        if (!tag.PropSetters.TryGetValue(propName.ToLowerInvariant(), out var setter))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[SetPropHandler] 控制項 '{tag.Id}' 不支援 prop '{propName}'");
#endif
            return;
        }

        setter(ctx.Interpolate(ctx.Action.Value));
    }
}
