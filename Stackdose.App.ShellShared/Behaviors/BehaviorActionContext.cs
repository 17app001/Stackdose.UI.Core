using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Models;
using System.Windows;

namespace Stackdose.App.ShellShared.Behaviors;

/// <summary>
/// 執行一個 <see cref="BehaviorAction"/> 時傳遞給 <see cref="IBehaviorActionHandler"/> 的上下文。
/// </summary>
public sealed class BehaviorActionContext
{
    /// <summary>本次要執行的動作定義。</summary>
    public required BehaviorAction Action { get; init; }

    /// <summary>觸發此事件的來源名稱（valueChanged / click / connected / disconnected）。</summary>
    public required string EventOn { get; init; }

    /// <summary>觸發事件的控制項 Id（DesignerItemDefinition.Id）。</summary>
    public required string ControlId { get; init; }

    /// <summary>觸發值（valueChanged 時為 PLC 數值；click 傳 0）。</summary>
    public double TriggerValue { get; init; }

    /// <summary>觸發事件的 WPF 控制項實體（可為 null，例如 connected 事件）。</summary>
    public FrameworkElement? SourceControl { get; init; }

    /// <summary>
    /// 根據 DesignerItemDefinition.Id 查找 WPF 控制項。<br/>
    /// 傳入 <c>"self"</c> 或 null 時回傳 <see cref="SourceControl"/>。
    /// </summary>
    public Func<string, FrameworkElement?>? ControlResolver { get; init; }

    /// <summary>PLC Manager（WritePlc / SetStatus 使用）。可為 null（未連線時）。</summary>
    public IPlcManager? PlcManager { get; init; }

    /// <summary>稽核日誌寫入委派（LogAudit 使用）。可為 null（未設定時靜默）。</summary>
    public Action<string>? AuditLogger { get; init; }

    /// <summary>頁面導覽委派（Navigate 使用，B7 實作）。參數為 pageName。</summary>
    public Action<string>? Navigator { get; init; }

    // ── 便利方法 ────────────────────────────────────────────────────────────

    /// <summary>解析 <c>{value}</c> 佔位符為實際觸發值字串。</summary>
    public string Interpolate(string? template)
        => template?.Replace("{value}", TriggerValue.ToString("G")) ?? string.Empty;

    /// <summary>取得目標控制項：target == "self" 或空時回傳 SourceControl，否則透過 ControlResolver 查找。</summary>
    public FrameworkElement? ResolveTarget()
    {
        var target = Action.Target;
        if (string.IsNullOrEmpty(target) || target.Equals("self", StringComparison.OrdinalIgnoreCase))
            return SourceControl;
        return ControlResolver?.Invoke(target);
    }
}
