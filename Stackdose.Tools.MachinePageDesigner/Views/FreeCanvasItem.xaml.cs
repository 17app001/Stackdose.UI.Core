using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class FreeCanvasItem : UserControl
{
    private const double HalfThumb      = 4.0;
    private const double SnapThreshold  = 8.0;   // 智慧對齊磁吸距離（px）

    // ── Move state ───────────────────────────────────────────────────────
    private bool  _isDragging;
    private bool  _isMultiDrag;
    private Point _dragOrigin;
    private double _dragStartX, _dragStartY;
    private List<(DesignerItemViewModel Item, double StartX, double StartY)> _multiDragState = [];

    // ── Resize state ─────────────────────────────────────────────────────
    private double _resizeStartX, _resizeStartY, _resizeStartW, _resizeStartH;
    private List<(DesignerItemViewModel Item, double StartX, double StartY, double StartW, double StartH)> _multiResizeState = [];

    public FreeCanvasItem()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PositionThumbs();
    }

    private MainViewModel?         MainVm => Tag as MainViewModel;
    private DesignerItemViewModel? Item   => DataContext as DesignerItemViewModel;

    // ── Grid Snap ────────────────────────────────────────────────────────

    private double Snap(double value)
    {
        var vm = MainVm;
        if (vm == null || !vm.SnapToGrid) return value;
        return Math.Round(value / vm.SnapGridSize) * vm.SnapGridSize;
    }

    // ── Smart Snap（磁吸對齊其他元件邊緣/中心）──────────────────────────
    /// <summary>
    /// 傳入格線吸附後的原始 x/y，嘗試對齊畫布上其他元件的邊緣或中心。
    /// excluded 為不參與吸附比對的元件集合（如正在拖曳的那些）。
    /// </summary>
    private (double x, double y) SmartSnap(
        double rawX, double rawY,
        double w, double h,
        HashSet<DesignerItemViewModel> excluded)
    {
        var vm = MainVm;
        if (vm == null) return (rawX, rawY);

        // 被拖曳元件的三條 X 軸對齊線（左/中/右）
        double[] dragX = [rawX,         rawX + w / 2, rawX + w];
        double[] dragY = [rawY,         rawY + h / 2, rawY + h];

        double bestDX    = SnapThreshold + 1;
        double bestDY    = SnapThreshold + 1;
        double snapAdjX  = 0;
        double snapAdjY  = 0;

        foreach (var other in vm.Canvas.CanvasItems)
        {
            if (excluded.Contains(other)) continue;

            // 其他元件的三條 X/Y 軸對齊線
            double[] otherX = [other.X, other.X + other.Width  / 2, other.X + other.Width];
            double[] otherY = [other.Y, other.Y + other.Height / 2, other.Y + other.Height];

            foreach (var dx in dragX)
                foreach (var ox in otherX)
                {
                    double d = Math.Abs(dx - ox);
                    if (d < bestDX) { bestDX = d; snapAdjX = ox - dx; }
                }

            foreach (var dy in dragY)
                foreach (var oy in otherY)
                {
                    double d = Math.Abs(dy - oy);
                    if (d < bestDY) { bestDY = d; snapAdjY = oy - dy; }
                }
        }

        double finalX = bestDX <= SnapThreshold ? rawX + snapAdjX : rawX;
        double finalY = bestDY <= SnapThreshold ? rawY + snapAdjY : rawY;

        return (Math.Max(0, finalX), Math.Max(0, finalY));
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

        e.Handled = true;

        if (Item.IsLocked) { MainVm?.Canvas.SelectSingle(Item); return; }

        var parentCanvas = FindParentCanvas();
        if (parentCanvas == null) return;

        // ── Shift+Click：加入/移出多選，不啟動拖曳 ──────────────────────
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            MainVm?.Canvas.ToggleMultiSelect(Item);
            return;
        }

        var allSelected = MainVm?.Canvas.GetAllSelectedItems() ?? [];
        bool inExplicitMulti = allSelected.Count > 1 && allSelected.Contains(Item);

        if (inExplicitMulti)
        {
            // ── 多選模式：拖曳所有已選元件 ──────────────────────────────
            _isMultiDrag   = true;
            _multiDragState = allSelected
                .Where(i => !i.IsLocked)
                .Select(i => (i, i.X, i.Y))
                .ToList();
        }
        else if (Item.ItemType == "Spacer")
        {
            // ── GroupBox 模式：自動帶動中心點在框內的元件 ───────────────
            MainVm?.Canvas.SelectSingle(Item);

            var groupBounds = new Rect(Item.X, Item.Y, Item.Width, Item.Height);
            var contained = MainVm?.Canvas.CanvasItems
                .Where(i => !ReferenceEquals(i, Item) && !i.IsLocked)
                .Where(i => groupBounds.Contains(
                    new Point(i.X + i.Width / 2, i.Y + i.Height / 2)))
                .ToList() ?? [];

            if (contained.Count > 0)
            {
                _isMultiDrag    = true;
                _multiDragState = [(Item, Item.X, Item.Y),
                    ..contained.Select(i => (i, i.X, i.Y))];
            }
            else
            {
                _isMultiDrag = false;
                _multiDragState.Clear();
            }
        }
        else
        {
            // ── 單選模式 ─────────────────────────────────────────────────
            MainVm?.Canvas.SelectSingle(Item);
            _isMultiDrag = false;
            _multiDragState.Clear();
        }

        _isDragging  = true;
        _dragOrigin  = e.GetPosition(parentCanvas);
        _dragStartX  = Item.X;
        _dragStartY  = Item.Y;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || Item == null) return;
        if (e.LeftButton != MouseButtonState.Pressed) { StopDrag(cancelled: true); return; }

        var parentCanvas = FindParentCanvas();
        if (parentCanvas == null) return;

        var pos = e.GetPosition(parentCanvas);
        var dx  = pos.X - _dragOrigin.X;
        var dy  = pos.Y - _dragOrigin.Y;

        if (_isMultiDrag && _multiDragState.Count > 1)
        {
            // Smart snap 依主元件決定，其他元件套用相同位移量
            var excluded = _multiDragState.Select(t => t.Item).ToHashSet();
            var rawX = Math.Max(0, Snap(_dragStartX + dx));
            var rawY = Math.Max(0, Snap(_dragStartY + dy));
            var (finalX, finalY) = SmartSnap(rawX, rawY, Item.Width, Item.Height, excluded);

            double snapDx = finalX - _dragStartX;
            double snapDy = finalY - _dragStartY;

            foreach (var (item, startX, startY) in _multiDragState)
            {
                item.SetPropDirect("x", Math.Max(0, startX + snapDx));
                item.SetPropDirect("y", Math.Max(0, startY + snapDy));
            }
        }
        else
        {
            var rawX = Math.Max(0, Snap(_dragStartX + dx));
            var rawY = Math.Max(0, Snap(_dragStartY + dy));
            var (finalX, finalY) = SmartSnap(rawX, rawY, Item.Width, Item.Height,
                new HashSet<DesignerItemViewModel> { Item });
            Item.SetPropDirect("x", finalX);
            Item.SetPropDirect("y", finalY);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || Item == null) { StopDrag(cancelled: true); return; }

        if (_isMultiDrag && _multiDragState.Count > 1)
        {
            var moves = _multiDragState
                .Where(t => t.Item.X != t.StartX || t.Item.Y != t.StartY)
                .Select(t => (t.Item, t.StartX, t.StartY, t.Item.X, t.Item.Y))
                .ToList();
            StopDrag(cancelled: false);
            if (moves.Count > 0)
                MainVm?.RecordCanvasMultiMove(moves);
        }
        else
        {
            var endX = Item.X;
            var endY = Item.Y;
            StopDrag(cancelled: false);
            if (endX != _dragStartX || endY != _dragStartY)
                MainVm?.RecordCanvasMove(Item, _dragStartX, _dragStartY, endX, endY);
        }
    }

    private void StopDrag(bool cancelled)
    {
        if (cancelled && _isDragging)
        {
            if (_isMultiDrag && _multiDragState.Count > 1)
            {
                foreach (var (item, startX, startY) in _multiDragState)
                {
                    item.SetPropDirect("x", startX);
                    item.SetPropDirect("y", startY);
                }
            }
            else if (Item != null)
            {
                Item.SetPropDirect("x", _dragStartX);
                Item.SetPropDirect("y", _dragStartY);
            }
        }
        _isDragging = false;
        _isMultiDrag = false;
        _multiDragState.Clear();
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

        var allSelected = MainVm?.Canvas.GetAllSelectedItems() ?? [];
        if (allSelected.Count > 1 && allSelected.Contains(Item))
        {
            _multiResizeState = allSelected
                .Where(i => !i.IsLocked)
                .Select(i => (i, i.X, i.Y, i.Width, i.Height))
                .ToList();
        }
        else
        {
            _multiResizeState.Clear();
            MainVm?.Canvas.SelectSingle(Item);
        }
    }

    // 多選 resize helpers ──────────────────────────────────────────────────

    private void ApplyMultiNW(double dh, double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            var oldW = item.Width; var oldH = item.Height;
            var newW = Math.Max(40, oldW - dh);
            var newH = Math.Max(30, oldH - dv);
            item.SetPropDirect("x", item.X + oldW - newW);
            item.SetPropDirect("y", item.Y + oldH - newH);
            item.SetPropDirect("width",  newW);
            item.SetPropDirect("height", newH);
        }
    }

    private void ApplyMultiN(double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            var oldH = item.Height;
            var newH = Math.Max(30, oldH - dv);
            item.SetPropDirect("y",      item.Y + oldH - newH);
            item.SetPropDirect("height", newH);
        }
    }

    private void ApplyMultiNE(double dh, double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            var oldH = item.Height;
            var newH = Math.Max(30, oldH - dv);
            item.SetPropDirect("y",      item.Y + oldH - newH);
            item.SetPropDirect("width",  Math.Max(40, item.Width + dh));
            item.SetPropDirect("height", newH);
        }
    }

    private void ApplyMultiW(double dh)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            var oldW = item.Width;
            var newW = Math.Max(40, oldW - dh);
            item.SetPropDirect("x",     item.X + oldW - newW);
            item.SetPropDirect("width", newW);
        }
    }

    private void ApplyMultiE(double dh)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            item.SetPropDirect("width", Math.Max(40, item.Width + dh));
        }
    }

    private void ApplyMultiSW(double dh, double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            var oldW = item.Width;
            var newW = Math.Max(40, oldW - dh);
            item.SetPropDirect("x",      item.X + oldW - newW);
            item.SetPropDirect("width",  newW);
            item.SetPropDirect("height", Math.Max(30, item.Height + dv));
        }
    }

    private void ApplyMultiS(double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            item.SetPropDirect("height", Math.Max(30, item.Height + dv));
        }
    }

    private void ApplyMultiSE(double dh, double dv)
    {
        foreach (var (item, _, _, _, _) in _multiResizeState)
        {
            if (ReferenceEquals(item, Item)) continue;
            item.SetPropDirect("width",  Math.Max(40, item.Width  + dh));
            item.SetPropDirect("height", Math.Max(30, item.Height + dv));
        }
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
        if (_multiResizeState.Count > 1) ApplyMultiNW(e.HorizontalChange, e.VerticalChange);
    }

    private void OnNDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldH = Item.Height;
        var newH = Math.Max(30, oldH - e.VerticalChange);
        Item.SetPropDirect("y",      Item.Y + oldH - newH);
        Item.SetPropDirect("height", newH);
        if (_multiResizeState.Count > 1) ApplyMultiN(e.VerticalChange);
    }

    private void OnNEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldH = Item.Height;
        var newH = Math.Max(30, oldH - e.VerticalChange);
        Item.SetPropDirect("y",      Item.Y + oldH - newH);
        Item.SetPropDirect("width",  Math.Max(40, Item.Width + e.HorizontalChange));
        Item.SetPropDirect("height", newH);
        if (_multiResizeState.Count > 1) ApplyMultiNE(e.HorizontalChange, e.VerticalChange);
    }

    private void OnWDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldW = Item.Width;
        var newW = Math.Max(40, oldW - e.HorizontalChange);
        Item.SetPropDirect("x",     Item.X + oldW - newW);
        Item.SetPropDirect("width", newW);
        if (_multiResizeState.Count > 1) ApplyMultiW(e.HorizontalChange);
    }

    private void OnEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("width", Math.Max(40, Item.Width + e.HorizontalChange));
        if (_multiResizeState.Count > 1) ApplyMultiE(e.HorizontalChange);
    }

    private void OnSWDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        var oldW = Item.Width;
        var newW = Math.Max(40, oldW - e.HorizontalChange);
        Item.SetPropDirect("x",      Item.X + oldW - newW);
        Item.SetPropDirect("width",  newW);
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
        if (_multiResizeState.Count > 1) ApplyMultiSW(e.HorizontalChange, e.VerticalChange);
    }

    private void OnSDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
        if (_multiResizeState.Count > 1) ApplyMultiS(e.VerticalChange);
    }

    private void OnSEDrag(object sender, DragDeltaEventArgs e)
    {
        if (Item == null) return;
        Item.SetPropDirect("width",  Math.Max(40, Item.Width  + e.HorizontalChange));
        Item.SetPropDirect("height", Math.Max(30, Item.Height + e.VerticalChange));
        if (_multiResizeState.Count > 1) ApplyMultiSE(e.HorizontalChange, e.VerticalChange);
    }

    private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        if (Item == null || MainVm == null) return;

        if (_multiResizeState.Count > 1)
        {
            var resizes = _multiResizeState
                .Where(t => t.Item.X != t.StartX || t.Item.Y != t.StartY ||
                            t.Item.Width != t.StartW || t.Item.Height != t.StartH)
                .Select(t => (t.Item, t.StartX, t.StartY, t.StartW, t.StartH,
                              t.Item.X, t.Item.Y, t.Item.Width, t.Item.Height))
                .ToList();
            _multiResizeState.Clear();
            if (resizes.Count > 0)
                MainVm.RecordCanvasMultiResize(resizes);
        }
        else
        {
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
