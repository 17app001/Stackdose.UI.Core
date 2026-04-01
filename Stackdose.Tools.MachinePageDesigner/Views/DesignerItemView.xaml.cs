using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class DesignerItemView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;

    public DesignerItemView()
    {
        InitializeComponent();
    }

    private void OnCardClicked(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not DesignerItemViewModel item) return;

        // 找到 DesignCanvasViewModel 並設定選取
        var canvas = this.FindAncestor<DesignCanvas>();
        if (canvas?.DataContext is DesignCanvasViewModel canvasVm)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Shift+Click 多選
                canvasVm.ToggleMultiSelect(item);
            }
            else
            {
                // 單選
                canvasVm.SelectSingle(item);
            }
        }

        e.Handled = true;
    }

    private void OnRemoveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DesignerItemViewModel item) return;

        // 找到所屬 ZoneViewModel 並移除
        var zoneView = this.FindAncestor<ZoneView>();
        if (zoneView?.DataContext is ZoneViewModel zone)
        {
            // 先清除選取
            var canvas = this.FindAncestor<DesignCanvas>();
            if (canvas?.DataContext is DesignCanvasViewModel canvasVm && canvasVm.SelectedItem == item)
                canvasVm.SelectedItem = null;

            // 透過 UndoRedo 刪除
            var mainVm = (canvas?.Tag as MainViewModel);
            if (mainVm != null)
                mainVm.ExecuteRemoveItem(zone, item);
            else
                zone.RemoveItem(item);
        }
    }

    // ── 拖拉把手（?）事件 ──

    private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
        e.Handled = true;
    }

    private void OnDragHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (_isDragging) return;

        var pos = e.GetPosition(this);
        var diff = pos - _dragStartPoint;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragging = true;

            if (DataContext is DesignerItemViewModel item)
            {
                var data = new DataObject("DesignerItem", item);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }

            _isDragging = false;
        }
    }
}