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
        e.Effects = (e.Data.GetDataPresent("ToolboxItem") || e.Data.GetDataPresent("Template"))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (MainVm == null) return;
        var pos = e.GetPosition(designCanvas);

        // Single control from toolbox
        if (e.Data.GetData("ToolboxItem") is ToolboxItemDescriptor desc)
        {
            var def = desc.CreateDefinition();
            def.X = Math.Max(0, Math.Min(Snap(pos.X - def.Width / 2), MainVm.Canvas.CanvasWidth - def.Width));
            def.Y = Math.Max(0, Math.Min(Snap(pos.Y - def.Height / 2), MainVm.Canvas.CanvasHeight - def.Height));

            var vm = new DesignerItemViewModel(def);
            MainVm.ExecuteCanvasAddItem(vm);
            MainVm.Canvas.SelectedItem = vm;
            e.Handled = true;
            return;
        }

        // Template (multiple controls)
        if (e.Data.GetData("Template") is TemplateDescriptor template)
        {
            var baseX = Snap(pos.X);
            var baseY = Snap(pos.Y);
            var instances = template.CreateInstances(baseX, baseY);

            MainVm.Canvas.ClearSelection();
            foreach (var def in instances)
            {
                def.X = Snap(Math.Max(0, Math.Min(def.X, MainVm.Canvas.CanvasWidth - def.Width)));
                def.Y = Snap(Math.Max(0, Math.Min(def.Y, MainVm.Canvas.CanvasHeight - def.Height)));
                var vm = new DesignerItemViewModel(def);
                MainVm.ExecuteCanvasAddItem(vm);
                MainVm.Canvas.ToggleMultiSelect(vm);
            }
            e.Handled = true;
        }
    }

    // ── Background Click → Deselect / Rubber-band ───────────────────────

    private bool _isRubberBanding;
    private Point _rubberOrigin;

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!ReferenceEquals(e.Source, designCanvas)) return;

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
