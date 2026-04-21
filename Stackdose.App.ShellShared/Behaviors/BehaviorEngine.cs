using Stackdose.Abstractions.Hardware;
using Stackdose.App.ShellShared.Behaviors.Handlers;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Windows;

namespace Stackdose.App.ShellShared.Behaviors;

/// <summary>
/// B5 核心：訂閱控制項事件，評估 when 條件，依序執行 do 動作。
/// <para>
/// 使用流程：
/// <code>
/// var engine = new BehaviorEngine { PlcManager = mgr, AuditLogger = log };
/// engine.BindDocument(doc.CanvasItems, controlMap);
/// // ...
/// engine.Dispose(); // 頁面卸載時
/// </code>
/// </para>
/// 自訂 Handler：<c>engine.Register(new CloseValveHandler());</c>
/// </summary>
public sealed class BehaviorEngine : IDisposable
{
    private readonly Dictionary<string, IBehaviorActionHandler> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IControlWithBehaviors> _defs = [];
    private readonly Dictionary<string, FrameworkElement> _controls = [];

    // ── 外部依賴（可在 BindDocument 前設定）──────────────────────────────────
    public IPlcManager? PlcManager { get; set; }
    public Action<string>? AuditLogger { get; set; }
    public Action<string>? Navigator { get; set; }

    public BehaviorEngine()
    {
        // 註冊所有內建 Handler
        Register(new SetPropHandler());
        Register(new WritePlcHandler());
        Register(new LogAuditHandler());
        Register(new ShowDialogHandler());
        Register(new NavigateHandler());
        Register(new SetStatusHandler());

        PlcEventContext.ControlValueChanged += OnControlValueChanged;
        BehaviorEventBus.ControlEventFired  += OnControlEventFired;
    }

    // ── 公開 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 註冊自訂（或覆寫內建）Handler。同名 ActionType 後者覆蓋前者。
    /// </summary>
    public void Register(IBehaviorActionHandler handler)
        => _handlers[handler.ActionType] = handler;

    /// <summary>
    /// 綁定目前頁面的文件定義與 WPF 控制項對照表。<br/>
    /// 每次載入新文件時呼叫以替換舊的綁定。
    /// </summary>
    /// <param name="items">IControlWithBehaviors 清單（DesignerItemDefinition 已實作此介面）</param>
    /// <param name="controls">id → FrameworkElement 對照</param>
    public void BindDocument(
        IEnumerable<IControlWithBehaviors> items,
        IEnumerable<KeyValuePair<string, FrameworkElement>> controls)
    {
        _defs.Clear();
        _controls.Clear();
        foreach (var item in items)
            _defs[item.Id] = item;
        foreach (var kv in controls)
            _controls[kv.Key] = kv.Value;
    }

    /// <summary>手動觸發任意控制項事件（測試 / 外部觸發用）。</summary>
    public void Dispatch(string controlId, string eventOn, double triggerValue = 0)
        => DispatchCore(controlId, eventOn, triggerValue,
            _controls.GetValueOrDefault(controlId));

    public void Dispose()
    {
        PlcEventContext.ControlValueChanged -= OnControlValueChanged;
        BehaviorEventBus.ControlEventFired  -= OnControlEventFired;
    }

    // ── 事件訂閱 ────────────────────────────────────────────────────────────

    private void OnControlValueChanged(object? sender, PlcValueChangedEventArgs args)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.Tag is not ControlRuntimeTag tag) return;

        double.TryParse(args.RawValue?.ToString(), out var numValue);
        DispatchCore(tag.Id, "valueChanged", numValue, fe);
    }

    private void OnControlEventFired(string controlId, string eventOn, double value)
        => DispatchCore(controlId, eventOn, value, _controls.GetValueOrDefault(controlId));

    // ── 核心派發 ────────────────────────────────────────────────────────────

    private void DispatchCore(
        string controlId, string eventOn, double triggerValue,
        FrameworkElement? sourceControl)
    {
        if (!_defs.TryGetValue(controlId, out var def)) return;
        if (def.Events.Count == 0) return;

        foreach (var evt in def.Events)
        {
            if (!evt.On.Equals(eventOn, StringComparison.OrdinalIgnoreCase)) continue;
            if (evt.When is not null && !Evaluate(evt.When, triggerValue)) continue;

            foreach (var action in evt.Do)
            {
                if (string.IsNullOrEmpty(action.Action)) continue;
                if (!_handlers.TryGetValue(action.Action, out var handler))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[BehaviorEngine] 未知 action '{action.Action}'，已跳過");
#endif
                    continue;
                }

                var ctx = new BehaviorActionContext
                {
                    Action          = action,
                    EventOn         = eventOn,
                    ControlId       = controlId,
                    TriggerValue    = triggerValue,
                    SourceControl   = sourceControl,
                    ControlResolver = id => _controls.GetValueOrDefault(id),
                    PlcManager      = PlcManager,
                    AuditLogger     = AuditLogger,
                    Navigator       = Navigator,
                };

                try   { handler.Execute(ctx); }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[BehaviorEngine] Handler '{action.Action}' 執行失敗: {ex.Message}");
#endif
                }
            }
        }
    }

    private static bool Evaluate(BehaviorCondition cond, double value)
        => cond.Op switch
        {
            ">"  => value >  cond.Value,
            ">=" => value >= cond.Value,
            "<"  => value <  cond.Value,
            "<=" => value <= cond.Value,
            "==" => Math.Abs(value - cond.Value) < 1e-9,
            "!=" => Math.Abs(value - cond.Value) >= 1e-9,
            _    => false,
        };
}
