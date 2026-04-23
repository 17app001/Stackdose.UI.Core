using Stackdose.UI.Core.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 包裝 <see cref="BehaviorAction"/>，供 EventsPanel 雙向綁定。
/// 直接寫回底層 POCO，不參與 UndoRedo（B6 簡化設計）。
/// </summary>
public sealed class BehaviorActionViewModel : ObservableObject
{
    private readonly BehaviorAction _action;

    public BehaviorActionViewModel(BehaviorAction action) => _action = action;

    public BehaviorAction ToModel() => _action;

    // ── 動作類型 ──────────────────────────────────────────────────────────

    public string ActionType
    {
        get => _action.Action;
        set { if (_action.Action == value) return; _action.Action = value; N(); N(nameof(ShowTarget)); N(nameof(ShowProp)); N(nameof(ShowValue)); N(nameof(ShowMessage)); N(nameof(ShowTitle)); N(nameof(ShowPage)); N(nameof(Summary)); }
    }

    // ── 動作欄位 ──────────────────────────────────────────────────────────

    public string Target
    {
        get => _action.Target ?? "";
        set { if (_action.Target == value) return; _action.Target = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    public string Prop
    {
        get => _action.Prop ?? "";
        set { if (_action.Prop == value) return; _action.Prop = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    public string Value
    {
        get => _action.Value ?? "";
        set { if (_action.Value == value) return; _action.Value = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    public string Message
    {
        get => _action.Message ?? "";
        set { if (_action.Message == value) return; _action.Message = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    public string Title
    {
        get => _action.Title ?? "";
        set { if (_action.Title == value) return; _action.Title = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    public string Page
    {
        get => _action.Page ?? "";
        set { if (_action.Page == value) return; _action.Page = string.IsNullOrEmpty(value) ? null : value; N(); N(nameof(Summary)); }
    }

    // ── 欄位可見性（依 ActionType 顯示） ─────────────────────────────────

    public bool ShowTarget  => ActionType is "SetProp" or "WritePlc";
    public bool ShowProp    => ActionType is "SetProp";
    public bool ShowValue   => ActionType is "SetProp" or "WritePlc" or "SetStatus";
    public bool ShowMessage => ActionType is "LogAudit" or "ShowDialog";
    public bool ShowTitle   => ActionType is "ShowDialog";
    public bool ShowPage    => ActionType is "Navigate";

    // ── 清單顯示摘要 ──────────────────────────────────────────────────────

    public string Summary => ActionType switch
    {
        "SetProp"    => $"SetProp  {Target}.{Prop} = {Value}",
        "WritePlc"   => $"WritePlc {Target} \u2190 {Value}",
        "LogAudit"   => $"LogAudit '{Message}'",
        "ShowDialog" => $"ShowDialog '{Title}' / {Message}",
        "Navigate"   => $"Navigate \u2192 {Page}",
        "SetStatus"  => $"SetStatus {Value}",
        _            => ActionType,
    };

    // ── 靜態清單 ──────────────────────────────────────────────────────────

    public static readonly string[] ActionTypes =
        ["SetProp", "WritePlc", "LogAudit", "ShowDialog", "Navigate", "SetStatus"];
}
