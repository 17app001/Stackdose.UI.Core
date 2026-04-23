using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class FreeCanvas : UserControl
{
    public FreeCanvas()
    {
        InitializeComponent();
    }

    private MainViewModel? MainVm => Tag as MainViewModel;

    // ── Toolbox Drop ─────────────────────────────────────────────────────

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("ToolboxItem")
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var vm = MainVm?.Canvas;
            if (vm == null) return;

            if (e.Delta > 0) vm.ZoomIn();
            else vm.ZoomOut();

            e.Handled = true;
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("ToolboxItem") is not ToolboxItemDescriptor desc) return;
        if (MainVm == null) return;

        var pos = e.GetPosition(designCanvas);
        var def = desc.CreateDefinition();

        def.X = Math.Max(0, Math.Min(Snap(pos.X - def.Width / 2), MainVm.Canvas.CanvasWidth - def.Width));
        def.Y = Math.Max(0, Math.Min(Snap(pos.Y - def.Height / 2), MainVm.Canvas.CanvasHeight - def.Height));

        var vm = new DesignerItemViewModel(def);
        MainVm.ExecuteCanvasAddItem(vm);
        MainVm.Canvas.SelectedItem = vm;
        e.Handled = true;
    }

    // ── Background Click → Deselect / Rubber-band ───────────────────────
    private bool  _isRubberBanding;
    private Point _rubberOrigin;

    // ── Keyboard: arrow keys move selected items ─────────────────────────

    internal void FocusCanvas() => Focus();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = MainVm;
        if (vm == null) return;

        double step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;

        double dx = 0, dy = 0;
        switch (e.Key)
        {
            case Key.Left:  dx = -step; break;
            case Key.Right: dx =  step; break;
            case Key.Up:    dy = -step; break;
            case Key.Down:  dy =  step; break;
            default: return;
        }

        var selected = vm.Canvas.SelectedItem;
        if (selected == null) return;

        e.Handled = true;

        if (dx != 0) selected.X += dx;
        if (dy != 0) selected.Y += dy;
    }

    // ── Background Click → Deselect / Rubber-band ───────────────────────

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(e.Source, designCanvas)) return;

        Focus();
        MainVm?.Canvas.ClearSelection();

        _rubberOrigin = e.GetPosition(designCanvas);
        _isRubberBanding = true;
        designCanvas.CaptureMouse();

        Canvas.SetLeft(selectionRect, _rubberOrigin.X);
        Canvas.SetTop(selectionRect, _rubberOrigin.Y);
        selectionRect.Width = 0;
        selectionRect.Height = 0;
        selectionRect.Visibility = Visibility.Visible;

        e.Handled = true;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isRubberBanding) return;

        var pos = e.GetPosition(designCanvas);
        var x = Math.Min(pos.X, _rubberOrigin.X);
        var y = Math.Min(pos.Y, _rubberOrigin.Y);
        Canvas.SetLeft(selectionRect, x);
        Canvas.SetTop(selectionRect, y);
        selectionRect.Width = Math.Abs(pos.X - _rubberOrigin.X);
        selectionRect.Height = Math.Abs(pos.Y - _rubberOrigin.Y);
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isRubberBanding) return;
        _isRubberBanding = false;
        designCanvas.ReleaseMouseCapture();
        selectionRect.Visibility = Visibility.Collapsed;

        var w = selectionRect.Width;
        var h = selectionRect.Height;
        if (w < 6 || h < 6) return; // 視為空白點擊，已 ClearSelection

        var selX = Canvas.GetLeft(selectionRect);
        var selY = Canvas.GetTop(selectionRect);
        var selRect = new Rect(selX, selY, w, h);

        var vm = MainVm;
        if (vm == null) return;

        foreach (var item in vm.Canvas.CanvasItems)
        {
            if (item.ItemType == "Spacer") continue; // GroupBox 不納入框選，只能點 Header 單選
            var itemRect = new Rect(item.X, item.Y, item.Width, item.Height);
            if (selRect.IntersectsWith(itemRect))
                vm.Canvas.ToggleMultiSelect(item);
        }
    }

    // ── Snap helper ──────────────────────────────────────────────────────

    private double Snap(double value)
    {
        var vm = MainVm;
        if (vm == null || !vm.SnapToGrid) return value;
        return Math.Round(value / vm.SnapGridSize) * vm.SnapGridSize;
    }
}
