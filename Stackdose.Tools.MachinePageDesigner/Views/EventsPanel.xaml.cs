using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>
/// B6 行為事件編輯面板。
/// DataContext 應為 <see cref="DesignerItemViewModel"/>。
/// </summary>
public partial class EventsPanel : UserControl
{
    private DesignerItemViewModel? _vm;
    private BehaviorEventViewModel? _currentEvent;
    private BehaviorActionViewModel? _currentAction;
    private bool _suppressHandlers;

    public EventsPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // ── DataContext 切換 ──────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _vm = DataContext as DesignerItemViewModel;
        _suppressHandlers = true;
        eventsListBox.ItemsSource = _vm?.Events;
        eventsListBox.SelectedItem = null;
        ShowEventDetail(null);
        _suppressHandlers = false;
    }

    // ── 事件列表操作 ─────────────────────────────────────────────────────

    private void OnAddEvent(object sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        _vm.AddEvent();
        eventsListBox.SelectedItem = _vm.Events.Last();
    }

    private void OnRemoveEvent(object sender, RoutedEventArgs e)
    {
        if (_vm is null || _currentEvent is null) return;
        _vm.RemoveEvent(_currentEvent);
        eventsListBox.SelectedItem = _vm.Events.LastOrDefault();
    }

    private void OnEventSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressHandlers) return;
        var evt = eventsListBox.SelectedItem as BehaviorEventViewModel;
        ShowEventDetail(evt);
    }

    private void ShowEventDetail(BehaviorEventViewModel? evt)
    {
        _currentEvent = evt;
        _suppressHandlers = true;

        if (evt is null)
        {
            noSelectionHint.Visibility = Visibility.Visible;
            detailForm.Visibility = Visibility.Collapsed;
            _suppressHandlers = false;
            return;
        }

        noSelectionHint.Visibility = Visibility.Collapsed;
        detailForm.Visibility = Visibility.Visible;

        // 觸發來源
        cboOn.SelectedItem = evt.On;

        // 條件
        chkHasCondition.IsChecked = evt.HasCondition;
        whenForm.Visibility = evt.HasCondition ? Visibility.Visible : Visibility.Collapsed;
        cboWhenOp.SelectedItem = evt.WhenOp;
        txtWhenValue.Text = evt.WhenValue.ToString("G");

        // 動作清單
        actionsListBox.ItemsSource = evt.Actions;
        actionsListBox.SelectedItem = null;
        ShowActionDetail(null);

        _suppressHandlers = false;
    }

    // ── 事件詳細欄位回寫 ─────────────────────────────────────────────────

    private void OnEventOnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressHandlers || _currentEvent is null) return;
        if (cboOn.SelectedItem is string on)
            _currentEvent.On = on;
    }

    private void OnHasConditionChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressHandlers || _currentEvent is null) return;
        _currentEvent.HasCondition = chkHasCondition.IsChecked == true;
        whenForm.Visibility = _currentEvent.HasCondition ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnWhenOpChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressHandlers || _currentEvent is null) return;
        if (cboWhenOp.SelectedItem is string op)
            _currentEvent.WhenOp = op;
    }

    private void OnWhenValueChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressHandlers || _currentEvent is null) return;
        if (double.TryParse(txtWhenValue.Text, out var d))
            _currentEvent.WhenValue = d;
        else
            txtWhenValue.Text = _currentEvent.WhenValue.ToString("G");
    }

    // ── 動作列表操作 ─────────────────────────────────────────────────────

    private void OnAddAction(object sender, RoutedEventArgs e)
    {
        if (_currentEvent is null) return;
        _currentEvent.AddAction();
        actionsListBox.SelectedItem = _currentEvent.Actions.Last();
    }

    private void OnRemoveAction(object sender, RoutedEventArgs e)
    {
        if (_currentEvent is null || _currentAction is null) return;
        _currentEvent.RemoveAction(_currentAction);
        actionsListBox.SelectedItem = _currentEvent.Actions.LastOrDefault();
    }

    private void OnActionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressHandlers) return;
        var action = actionsListBox.SelectedItem as BehaviorActionViewModel;
        ShowActionDetail(action);
    }

    private void ShowActionDetail(BehaviorActionViewModel? action)
    {
        _currentAction = action;
        _suppressHandlers = true;

        if (action is null)
        {
            actionDetail.Visibility = Visibility.Collapsed;
            _suppressHandlers = false;
            return;
        }

        actionDetail.Visibility = Visibility.Visible;
        cboActionType.SelectedItem = action.ActionType;

        txtTarget.Text  = action.Target;
        txtProp.Text    = action.Prop;
        txtValue.Text   = action.Value;
        txtMessage.Text = action.Message;
        txtTitle.Text   = action.Title;
        txtPage.Text    = action.Page;

        UpdateActionFieldVisibility(action);

        _suppressHandlers = false;
    }

    private static Visibility V(bool show) => show ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateActionFieldVisibility(BehaviorActionViewModel action)
    {
        targetField.Visibility  = V(action.ShowTarget);
        propField.Visibility    = V(action.ShowProp);
        valueField.Visibility   = V(action.ShowValue);
        messageField.Visibility = V(action.ShowMessage);
        titleField.Visibility   = V(action.ShowTitle);
        pageField.Visibility    = V(action.ShowPage);
    }

    // ── 動作詳細欄位回寫 ─────────────────────────────────────────────────

    private void OnActionTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressHandlers || _currentAction is null) return;
        if (cboActionType.SelectedItem is string t)
        {
            _currentAction.ActionType = t;
            UpdateActionFieldVisibility(_currentAction);
        }
    }

    private void OnActionFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressHandlers || _currentAction is null) return;
        if (sender == txtTarget)  _currentAction.Target  = txtTarget.Text;
        if (sender == txtProp)    _currentAction.Prop    = txtProp.Text;
        if (sender == txtValue)   _currentAction.Value   = txtValue.Text;
        if (sender == txtMessage) _currentAction.Message = txtMessage.Text;
        if (sender == txtTitle)   _currentAction.Title   = txtTitle.Text;
        if (sender == txtPage)    _currentAction.Page    = txtPage.Text;
    }

    private void OnNumericKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (e.Key == Key.Enter)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            if (tb == txtWhenValue && _currentEvent is not null
                && double.TryParse(txtWhenValue.Text, out var d))
                _currentEvent.WhenValue = d;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            e.Handled = true;
        }
    }

    private void OnEnterCommit(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is TextBox tb)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            // 觸發 LostFocus 邏輯
            OnActionFieldChanged(tb, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
