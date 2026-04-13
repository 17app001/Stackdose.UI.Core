using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class FreeCanvasItem : UserControl
{
    private const double HalfThumb = 4.0;

    // ── Move state ───────────────────────────────────────────────────────
    private bool _isDragging;
    private Point _dragOrigin;
    private double _dragStartX, _dragStartY;

    // ── Resize state ─────────────────────────────────────────────────────
    private double _resizeStartX, _resizeStartY, _resizeStartW, _resizeStartH;

    public FreeCanvasItem()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PositionThumbs();
    }

    private MainViewModel? MainVm => Tag as MainViewModel;
    private DesignerItemViewModel? Item => DataContext as DesignerItemViewModel;

    // ── Snap helper ───────────────────────────────────────────────────────

    private double Snap(double value)
    {
        var vm = MainVm;
        if (vm == null || !vm.SnapToGrid) return value;
        return Math.Round(value / vm.SnapGridSize) * vm.SnapGridSize;
    }

    // ── Thumb positioning ─────────────────────────────────────────────────

    private void PositionThumbs()
    {
        var w = ActualWidth;
        var h = ActualHeight;

        SetThumb(thumbNW, -HalfThumb,         -HalfThumb);
        SetThumb(thumbN,  w / 2 - HalfThumb,  -HalfThumb);
        SetThumb(thumbNE, w - HalfThumb,       -HalfThumb);
        SetThumb(thumbW,  -HalfThumb,          h / 2 - HalfThumb);
        SetThumb(thumbE,  w - HalfThumb,       h / 2 - HalfThumb);
        SetThumb(thumbSW, -HalfThumb,          h - HalfThumb);
        SetThumb(thumbS,  w / 2 - HalfThumb,  h - HalfThumb);
        SetThumb(thumbSE, w - HalfThumb,       h - HalfThumb);
    }

    private static void SetThumb(Thumb t, double left, double top)
    {
        Canvas.SetLeft(t, left);
        Canvas.SetTop(t, top);
    }

    // ── Move (drag whole item) ────────────────────────────────────────────

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Thumb) { e.Handled = true; return; }
        if (Item == null) return;

        // 統一先標記 handled，防止事件冒泡到 Canvas 觸發框選
        e.Handled = true;

        if (Item.IsLocked) { MainVm?.Canvas.SelectSingle(Item); return; }

        MainVm?.Canvas.SelectSingle(Item);

        var parentCanvas = FindParentCanvas();
        if (parentCanvas == null) return;

        _isDragging = true;
        _dragOrigin = e.GetPosition(parentCanvas);
        _dragStartX = Item.X;
        _dragStartY = Item.Y;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || Item == null) return;
        if (e.LeftButton != MouseButtonState.Pressed) { StopDrag(cancelled: true); return; }

        var parentCanvas = FindParentCanvas();
        if (parentCanvas == null) return;

        var pos = e.GetPosition(parentCanvas);
        Item.SetPropDirect("x", Math.Max(0, Snap(_dragStartX + (pos.X - _dragOrigin.X))));
        Item.SetPropDirect("y", Math.Max(0, Snap(_dragStartY + (pos.Y - _dragOrigin.Y))));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || Item == null) { StopDrag(cancelled: true); return; }

        var endX = Item.X;
        var endY = Item.Y;
        StopDrag(cancelled: false);

        if (endX != _dragStartX || endY != _dragStartY)
            MainVm?.RecordCanvasMove(Item, _dragStartX, _dragStartY, endX, endY);
    }

    private void StopDrag(bool cancelled)
    {
        if (cancelled && _isDragging && Item != null)
        {
            Item.SetPropDirect("x", _dragStartX);
            Item.SetPropDirect("y", _dragStartY);
        }
        _isDragging = false;
        ReleaseMouseCapture();
    }

    // ── Resize ────────────────────────────────────────────────────────────

    private void OnResizeStarted(object sender, DragStartedEventArgs e)
    {
        if (Item == null || Item.IsLocked) return;
        _resizeStartX = Item.X;
        _resizeStartY = Item.Y;
        _resizeStartW = Item.Width;
        _resizeStartH = Item.Height;
        MainVm?.Canvas.SelectSingle(Item);
    }

    private void OnNWDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldW = Item.Width; var oldH = Item.Height;
        var newW = Math.Max(40, oldW - e.HorizontalChange);
        var newH = Math.Max(30, oldH - e.VerticalChange);
        Item.SetPropDirect("x", Item.X + oldW - newW);
        Item.SetPropDirect("y", Item.Y + oldH - newH);
        Item.SetPropDirect("width",  newW);
        Item.SetPropDirect("height", newH);
    }

    private void OnNDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldH = Item.Height;
        var newH = Math.Max(30, oldH - e.VerticalChange);
        Item.SetPropDirect("y",      Item.Y + oldH - newH);
        Item.SetPropDirect("height", newH);
    }

    private void OnNEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldH = Item.Height;
        var newH = Math.Max(30, oldH - e.VerticalChange);
        Item.SetPropDirect("y",      Item.Y + oldH - newH);
        Item.SetPropDirect("width",  Math.Max(40, Item.Width + e.HorizontalChange));
        Item.SetPropDirect("height", newH);
    }

    private void OnWDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldW = Item.Width;
        var newW = Math.Max(40, oldW - e.HorizontalChange);
        Item.SetPropDirect("x",     Item.X + oldW - newW);
        Item.SetPropDirect("width", newW);
    }

    private void OnEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("width", Math.Max(40, Item.Width + e.HorizontalChange));
    }

    private void OnSWDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldW = Item.Width;
        var newW = Math.Max(40, oldW - e.HorizontalChange);
        Item.SetPropDirect("x",      Item.X + oldW - newW);
        Item.SetPropDirect("width",  newW);
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
    }

    private void OnSDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
    }

    private void OnSEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("width",  Math.Max(40, Item.Width  + e.HorizontalChange));
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
    }

    private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        if (Item == null || MainVm == null) return;
        var newX = Item.X; var newY = Item.Y;
        var newW = Item.Width; var newH = Item.Height;
        if (newX != _resizeStartX || newY != _resizeStartY ||
            newW != _resizeStartW || newH != _resizeStartH)
        {
            MainVm.RecordCanvasResize(Item,
                _resizeStartX, _resizeStartY, _resizeStartW, _resizeStartH,
                newX, newY, newW, newH);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Canvas? FindParentCanvas()
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(this);
        while (parent != null)
        {
            if (parent is Canvas c) return c;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
