using System.Collections.ObjectModel;
using Stackdose.UI.Core.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 包裝 <see cref="BehaviorEvent"/>，供 EventsPanel 雙向綁定。
/// Actions 為 ObservableCollection，新增/刪除直接寫回底層 POCO。
/// </summary>
public sealed class BehaviorEventViewModel : ObservableObject
{
    private readonly BehaviorEvent _event;
    private BehaviorActionViewModel? _selectedAction;

    public BehaviorEventViewModel(BehaviorEvent evt)
    {
        _event = evt;
        Actions = new ObservableCollection<BehaviorActionViewModel>(
            evt.Do.Select(a => new BehaviorActionViewModel(a)));
        Actions.CollectionChanged += (_, _) => N(nameof(Summary));
    }

    public BehaviorEvent ToModel() => _event;

    // ── 觸發來源 ──────────────────────────────────────────────────────────

    public string On
    {
        get => _event.On;
        set
        {
            if (_event.On == value) return;
            _event.On = value;
            N(); N(nameof(Summary));
        }
    }

    // ── 條件（when）──────────────────────────────────────────────────────

    public bool HasCondition
    {
        get => _event.When is not null;
        set
        {
            if (HasCondition == value) return;
            _event.When = value ? new BehaviorCondition { Op = "==", Value = 0 } : null;
            N(); N(nameof(WhenOp)); N(nameof(WhenValue)); N(nameof(Summary));
        }
    }

    public string WhenOp
    {
        get => _event.When?.Op ?? "==";
        set
        {
            if (_event.When is null) _event.When = new BehaviorCondition();
            if (_event.When.Op == value) return;
            _event.When.Op = value;
            N(); N(nameof(Summary));
        }
    }

    public double WhenValue
    {
        get => _event.When?.Value ?? 0;
        set
        {
            if (_event.When is null) _event.When = new BehaviorCondition();
            if (_event.When.Value == value) return;
            _event.When.Value = value;
            N(); N(nameof(Summary));
        }
    }

    // ── 動作清單 ──────────────────────────────────────────────────────────

    public ObservableCollection<BehaviorActionViewModel> Actions { get; }

    public BehaviorActionViewModel? SelectedAction
    {
        get => _selectedAction;
        set => Set(ref _selectedAction, value);
    }

    public void AddAction()
    {
        var model = new BehaviorAction { Action = "SetProp", Target = "self", Prop = "background", Value = "Red" };
        _event.Do.Add(model);
        var vm = new BehaviorActionViewModel(model);
        Actions.Add(vm);
        SelectedAction = vm;
    }

    public void RemoveAction(BehaviorActionViewModel vm)
    {
        _event.Do.Remove(vm.ToModel());
        Actions.Remove(vm);
        SelectedAction = Actions.LastOrDefault();
    }

    // ── 清單顯示摘要 ──────────────────────────────────────────────────────

    public string Summary
    {
        get
        {
            var cond = HasCondition ? $" when {WhenOp} {WhenValue}" : "";
            var actions = Actions.Count > 0 ? $" → {Actions.Count} 個動作" : " （無動作）";
            return $"{On}{cond}{actions}";
        }
    }

    // ── 靜態清單 ──────────────────────────────────────────────────────────

    public static readonly string[] OnTypes =
        ["valueChanged", "click", "connected", "disconnected"];

    public static readonly string[] WhenOps =
        [">", ">=", "<", "<=", "==", "!="];
}
