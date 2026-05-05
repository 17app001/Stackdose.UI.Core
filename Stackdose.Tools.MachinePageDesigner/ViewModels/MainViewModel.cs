using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using System.ComponentModel;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 頂層 ViewModel，協調所有子模組
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    // ── Sub-ViewModels ───────────────────────────────────────────────
    public DesignCanvasViewModel Canvas { get; } = new();
    public ToolboxViewModel Toolbox { get; } = new();

    // ── Document state ───────────────────────────────────────────────
    private DesignDocument _document = new();
    private string? _currentFilePath;
    private bool _isDirty;
    private string _statusText = "就緒";

    // ── Snap to Grid ─────────────────────────────────────────────────────────
    private bool _snapToGrid = true;

    // ── Layout settings ──────────────────────────────────────────────
    private string _layoutMode;
    private int _leftCommandWidthPx;
    private double _rightColumnWidthStar;
    private bool _showLiveLog;
    private bool _showAlarmViewer;
    private bool _showSensorViewer;

    // ── Meta ─────────────────────────────────────────────────────────
    private string _docTitle;
    private string _machineId;
    private string _plcIp;
    private int    _plcPort;
    private int    _scanInterval;

    // ── Clipboard ────────────────────────────────────────────────────
    private int _pasteCount;

    // ── Services ─────────────────────────────────────────────────────
    public UndoRedoService UndoRedo { get; } = new();

    private DesignerItemViewModel? _subscribedPropItem;

    // ── 屬性防抖：相同 item+key 在 300ms 內視為同一步 ──────────────
    private record PropSnapshot(DesignerItemViewModel Item, string PropKey, object? OldValue, DateTime At);
    private PropSnapshot? _lastPropSnapshot;

    public MainViewModel()
    {
        // 初始化 layout 欄位
        _layoutMode = _document.Layout.Mode;
        _leftCommandWidthPx = _document.Layout.LeftCommandWidthPx;
        _rightColumnWidthStar = _document.Layout.RightColumnWidthStar;
        _showLiveLog = _document.Layout.ShowLiveLog;
        _showAlarmViewer = _document.Layout.ShowAlarmViewer;
        _showSensorViewer = _document.Layout.ShowSensorViewer;
        _docTitle     = _document.Meta.Title;
        _machineId    = _document.Meta.MachineId;
        _plcIp        = _document.Meta.PlcIp;
        _plcPort      = _document.Meta.PlcPort;
        _scanInterval = _document.Meta.ScanInterval;

        // 初始化 Commands
        NewCmd = new RelayCommand(_ => NewDocument());
        OpenCmd = new RelayCommand(_ => OpenDocument());
        SaveCmd = new RelayCommand(_ => SaveDocument());
        SaveAsCmd = new RelayCommand(_ => SaveDocumentAs());
        UndoCmd = new RelayCommand(_ => PerformUndo(), _ => UndoRedo.CanUndo);
        RedoCmd = new RelayCommand(_ => PerformRedo(), _ => UndoRedo.CanRedo);
        DeleteSelectedCmd  = new RelayCommand(_ => DeleteSelected(),  _ => Canvas.HasSelectedItem);
        BringToFrontCmd    = new RelayCommand(_ => BringToFront(),   _ => Canvas.HasSelectedItem);
        SendToBackCmd      = new RelayCommand(_ => SendToBack(),     _ => Canvas.HasSelectedItem);
        MoveUpCmd          = new RelayCommand(_ => MoveUp(),         _ => Canvas.HasSelectedItem);
        MoveDownCmd        = new RelayCommand(_ => MoveDown(),       _ => Canvas.HasSelectedItem);
        LockToggleCmd      = new RelayCommand(_ => ToggleLock(),     _ => Canvas.HasSelectedItem);
        CopyCmd            = new RelayCommand(_ => CopySelected(),   _ => Canvas.HasSelectedItem);
        CutCmd             = new RelayCommand(_ => CutSelected(),    _ => Canvas.HasSelectedItem);
        PasteCmd           = new RelayCommand(p => PasteClipboard(p is string s && bool.TryParse(s, out var b) ? b : (p is bool b2 && b2)), _ => DesignClipboard.HasData);
        SelectAllCmd       = new RelayCommand(_ => SelectAll());

        AlignLeftCmd    = new RelayCommand(_ => AlignItems("left"),    _ => Canvas.HasSelectedItem);
        AlignRightCmd   = new RelayCommand(_ => AlignItems("right"),   _ => Canvas.HasSelectedItem);
        AlignTopCmd     = new RelayCommand(_ => AlignItems("top"),     _ => Canvas.HasSelectedItem);
        AlignBottomCmd  = new RelayCommand(_ => AlignItems("bottom"),  _ => Canvas.HasSelectedItem);
        AlignCenterHCmd = new RelayCommand(_ => AlignItems("centerH"), _ => Canvas.HasSelectedItem);
        AlignCenterVCmd = new RelayCommand(_ => AlignItems("centerV"), _ => Canvas.HasSelectedItem);

        DistributeHorizCmd = new RelayCommand(_ => DistributeItems(horizontal: true),  _ => Canvas.HasSelectedItem);
        DistributeVertCmd  = new RelayCommand(_ => DistributeItems(horizontal: false), _ => Canvas.HasSelectedItem);

        // UndoRedo 狀態變更時更新 dirty 並強制刷新 Command enable 狀態
        UndoRedo.StateChanged += () =>
        {
            MarkDirty();
            Application.Current?.Dispatcher.InvokeAsync(
                CommandManager.InvalidateRequerySuggested,
                DispatcherPriority.Background);
        };

        // 追蹤 SelectedItem 變更：訂閱 PropCommitted + 更新 StatusBar
        Canvas.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(DesignCanvasViewModel.SelectedItem)) return;

            if (_subscribedPropItem != null)
                _subscribedPropItem.PropCommitted -= OnItemPropCommitted;

            _subscribedPropItem = Canvas.SelectedItem;

            if (_subscribedPropItem != null)
                _subscribedPropItem.PropCommitted += OnItemPropCommitted;

            UpdateStatusFromSelection();
        };

        Canvas.LoadFromDocument(_document);
    }

    // ── Properties ───────────────────────────────────────────────────

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set { Set(ref _currentFilePath, value); N(nameof(WindowTitle)); }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set { Set(ref _isDirty, value); N(nameof(WindowTitle)); }
    }

    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    public string WindowTitle
    {
        get
        {
            var fileName = string.IsNullOrEmpty(CurrentFilePath) ? "未命名" : Path.GetFileName(CurrentFilePath);
            var dirty = IsDirty ? " *" : "";
            return $"{fileName}{dirty} — MachinePageDesigner";
        }
    }

    // ── Canvas Size（代理至 Canvas VM，並標記 Dirty）──────────────────

    public double CanvasWidth
    {
        get => Canvas.CanvasWidth;
        set
        {
            if (Canvas.CanvasWidth == value) return;
            Canvas.CanvasWidth = value;
            N();
            MarkDirty();
        }
    }

    public double CanvasHeight
    {
        get => Canvas.CanvasHeight;
        set
        {
            if (Canvas.CanvasHeight == value) return;
            Canvas.CanvasHeight = value;
            N();
            MarkDirty();
        }
    }

    // ── Layout Properties ────────────────────────────────────────────

    public string LayoutMode
    {
        get => _layoutMode;
        set { if (Set(ref _layoutMode, value)) { MarkDirty(); N(nameof(IsDashboardMode)); } }
    }

    public bool IsDashboardMode => _layoutMode == "Dashboard";

    public int LeftCommandWidthPx
    {
        get => _leftCommandWidthPx;
        set { if (Set(ref _leftCommandWidthPx, value)) MarkDirty(); }
    }

    public double RightColumnWidthStar
    {
        get => _rightColumnWidthStar;
        set { if (Set(ref _rightColumnWidthStar, value)) MarkDirty(); }
    }

    public bool ShowLiveLog
    {
        get => _showLiveLog;
        set { if (Set(ref _showLiveLog, value)) MarkDirty(); }
    }

    public bool ShowAlarmViewer
    {
        get => _showAlarmViewer;
        set { if (Set(ref _showAlarmViewer, value)) MarkDirty(); }
    }

    public bool ShowSensorViewer
    {
        get => _showSensorViewer;
        set { if (Set(ref _showSensorViewer, value)) MarkDirty(); }
    }

    public string DocTitle
    {
        get => _docTitle;
        set { if (Set(ref _docTitle, value)) MarkDirty(); }
    }

    public string MachineId
    {
        get => _machineId;
        set { if (Set(ref _machineId, value)) MarkDirty(); }
    }

    public string PlcIp
    {
        get => _plcIp;
        set { if (Set(ref _plcIp, value)) MarkDirty(); }
    }

    public int PlcPort
    {
        get => _plcPort;
        set { if (Set(ref _plcPort, value)) MarkDirty(); }
    }

    public int ScanInterval
    {
        get => _scanInterval;
        set { if (Set(ref _scanInterval, value)) MarkDirty(); }
    }

    public string[] LayoutModes { get; } = ["SplitRight", "Standard", "SplitBottom", "Dashboard"];

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => Set(ref _snapToGrid, value);
    }

    public double SnapGridSize => 10;

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand NewCmd { get; }
    public ICommand OpenCmd { get; }
    public ICommand SaveCmd { get; }
    public ICommand SaveAsCmd { get; }
    public ICommand UndoCmd { get; }
    public ICommand RedoCmd { get; }
    public ICommand DeleteSelectedCmd { get; }
    public ICommand BringToFrontCmd { get; }
    public ICommand SendToBackCmd { get; }
    public ICommand MoveUpCmd { get; }
    public ICommand MoveDownCmd { get; }
    public ICommand LockToggleCmd { get; }
    public ICommand CopyCmd { get; }
    public ICommand CutCmd { get; }
    public ICommand PasteCmd { get; }
    public ICommand SelectAllCmd { get; }

    // Align
    public ICommand AlignLeftCmd    { get; }
    public ICommand AlignRightCmd   { get; }
    public ICommand AlignTopCmd     { get; }
    public ICommand AlignBottomCmd  { get; }
    public ICommand AlignCenterHCmd { get; }
    public ICommand AlignCenterVCmd { get; }

    // Distribute
    public ICommand DistributeHorizCmd { get; }
    public ICommand DistributeVertCmd  { get; }

    // ── UndoRedo 整合方法（供 View 呼叫） ────────────────────────────

    /// <summary>
    /// 在自由畫布新增元件（透過 UndoRedo）。
    /// Spacer/GroupBox 自動插入 Z-order 0（最底層），確保其他元件可在其上方互動。
    /// </summary>
    public void ExecuteCanvasAddItem(DesignerItemViewModel vm)
    {
        IDesignCommand cmd = vm.ItemType == "Spacer"
            ? new CanvasInsertItemCommand(Canvas.CanvasItems, vm, 0)
            : new CanvasAddItemCommand(Canvas.CanvasItems, vm);

        UndoRedo.Execute(cmd);
        MarkDirty();
        StatusText = $"已新增：{vm.DisplayName}";
    }

    /// <summary>
    /// 記錄畫布移動（UI 已套用，僅記錄以供 Undo）
    /// </summary>
    public void RecordCanvasMove(DesignerItemViewModel item, double oldX, double oldY, double newX, double newY)
    {
        UndoRedo.Record(new CanvasMoveItemCommand(item, oldX, oldY, newX, newY));
        MarkDirty();
        StatusText = $"移動：{item.DisplayName} → ({newX:F0}, {newY:F0})";
    }

    /// <summary>
    /// 記錄多元件同步移動（UI 已套用，僅記錄以供 Undo）
    /// </summary>
    public void RecordCanvasMultiMove(
        List<(DesignerItemViewModel item, double oldX, double oldY, double newX, double newY)> moves)
    {
        UndoRedo.Record(new CanvasMoveMultipleItemsCommand(moves));
        MarkDirty();
        StatusText = $"移動 {moves.Count} 個元件";
    }

    /// <summary>
    /// 記錄多元件同步縮放（UI 已套用，僅記錄以供 Undo）
    /// </summary>
    public void RecordCanvasMultiResize(
        List<(DesignerItemViewModel item,
              double oldX, double oldY, double oldW, double oldH,
              double newX, double newY, double newW, double newH)> resizes)
    {
        UndoRedo.Record(new CanvasResizeMultipleItemsCommand(resizes));
        MarkDirty();
        StatusText = $"縮放 {resizes.Count} 個元件";
    }

    /// <summary>
    /// 記錄畫布縮放（UI 已套用，僅記錄以供 Undo）
    /// </summary>
    public void RecordCanvasResize(DesignerItemViewModel item,
        double oldX, double oldY, double oldW, double oldH,
        double newX, double newY, double newW, double newH)
    {
        UndoRedo.Record(new CanvasResizeItemCommand(item, oldX, oldY, oldW, oldH, newX, newY, newW, newH));
        MarkDirty();
        StatusText = $"縮放：{item.DisplayName} → {newW:F0}×{newH:F0}";
    }

    // ── Command Implementations ──────────────────────────────────────

    private void NewDocument()
    {
        if (IsDirty && !ConfirmDiscard()) return;

        _document = DesignFileService.CreateNew();
        LoadDocumentIntoUI();
        CurrentFilePath = null;
        IsDirty = false;
        UndoRedo.Clear();
        StatusText = "已建立新文件";
    }

    private void OpenDocument()
    {
        if (IsDirty && !ConfirmDiscard()) return;

        var dlg = new OpenFileDialog
        {
            Title = "開啟 Machine Design 檔案",
            Filter = "Machine Design (*.machinedesign.json)|*.machinedesign.json|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _document = DesignFileService.Load(dlg.FileName);
            LoadDocumentIntoUI();
            CurrentFilePath = dlg.FileName;
            IsDirty = false;
            UndoRedo.Clear();
            StatusText = $"已開啟：{Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveDocument()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            SaveDocumentAs();
            return;
        }
        DoSave(CurrentFilePath);
    }

    private void SaveDocumentAs()
    {
        var dlg = new SaveFileDialog
        {
            Title = "儲存 Machine Design 檔案",
            Filter = "Machine Design (*.machinedesign.json)|*.machinedesign.json",
            FileName = string.IsNullOrEmpty(CurrentFilePath)
                ? $"{MachineId}.machinedesign.json"
                : Path.GetFileName(CurrentFilePath),
        };
        if (dlg.ShowDialog() != true) return;
        DoSave(dlg.FileName);
    }

    private void DoSave(string path)
    {
        try
        {
            SyncUIToDocument();
            DesignFileService.Save(_document, path);
            CurrentFilePath = path;
            IsDirty = false;
            StatusText = $"已儲存：{Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopySelected()
    {
        var items = GetDeepSelectedItems();
        if (items.Count == 0) return;
        DesignClipboard.SetData(items.Select(i => i.ToDefinition().Clone()));
        _pasteCount = 0;
        Application.Current?.Dispatcher.InvokeAsync(
            CommandManager.InvalidateRequerySuggested,
            DispatcherPriority.Background);
        StatusText = $"已複製 {DesignClipboard.Count} 個元件";
    }

    private void CutSelected()
    {
        var items = GetDeepSelectedItems();
        if (items.Count == 0) return;

        // 先複製
        DesignClipboard.SetData(items.Select(i => i.ToDefinition().Clone()));
        _pasteCount = 0;

        // 再刪除
        var cmd = new CanvasRemoveMultipleItemsCommand(Canvas.CanvasItems, items);
        UndoRedo.Execute(cmd);

        Canvas.ClearSelection();
        MarkDirty();
        StatusText = $"已剪下 {items.Count} 個元件";
    }

    private void PasteClipboard(bool atTopLeft = false)
    {
        if (!DesignClipboard.HasData) return;
        _pasteCount++;

        var data = DesignClipboard.GetData();
        if (data.Count == 0) return;

        // 如果指定貼在左上角 (例如進入 TabPanel 時)，重設所有元件的基準點為 (10, 10)
        // 但維持多個元件之間的相對位置。
        double offsetX = 0;
        double offsetY = 0;

        if (atTopLeft)
        {
            double minX = data.Min(i => i.X);
            double minY = data.Min(i => i.Y);
            offsetX = 10 - minX;
            offsetY = 10 - minY;
        }
        else
        {
            offsetX = _pasteCount * 20;
            offsetY = _pasteCount * 20;
        }

        var newVms = data
            .Select(def => new DesignerItemViewModel(def.Clone(offsetX, offsetY)))
            .ToList();

        UndoRedo.Execute(new CanvasAddMultipleItemsCommand(Canvas.CanvasItems, newVms));

        Canvas.ClearSelection();
        foreach (var vm in newVms)
            Canvas.ToggleMultiSelect(vm);

        MarkDirty();
        StatusText = $"已貼上 {newVms.Count} 個元件";
    }

    private void SelectAll()
    {
        Canvas.ClearSelection();
        foreach (var item in Canvas.CanvasItems)
        {
            if (item.ItemType == "Spacer") continue; // GroupBox 不納入全選
            Canvas.ToggleMultiSelect(item);
        }
        StatusText = $"已全選 {Canvas.SelectedItems.Count} 個元件";
    }

    private void DeleteSelected()
    {
        var items = GetDeepSelectedItems();
        if (items.Count == 0) return;

        var cmd = new CanvasRemoveMultipleItemsCommand(Canvas.CanvasItems, items);
        UndoRedo.Execute(cmd);

        Canvas.ClearSelection();
        StatusText = $"已刪除：{items.Count} 個元件";
    }

    /// <summary>
    /// 取得「深層」選取元件：包含手動選取的元件，以及選取中的 Spacer 所包含的所有子元件。
    /// </summary>
    private List<DesignerItemViewModel> GetDeepSelectedItems()
    {
        var selected = Canvas.GetAllSelectedItems();
        if (selected.Count == 0) return [];

        var result = new HashSet<DesignerItemViewModel>(selected);
        var spacers = selected.Where(i => i.ItemType == "Spacer").ToList();

        foreach (var spacer in spacers)
        {
            var bounds = new Rect(spacer.X, spacer.Y, spacer.Width, spacer.Height);
            var children = Canvas.CanvasItems
                .Where(i => !ReferenceEquals(i, spacer))
                .Where(i =>
                {
                    var itemBounds = new Rect(i.X, i.Y, i.Width, i.Height);
                    return bounds.Contains(itemBounds);
                });

            foreach (var child in children)
                result.Add(child);
        }

        return [.. result];
    }

    private void BringToFront()
    {
        var item = Canvas.SelectedItem; if (item == null) return;
        var from = Canvas.CanvasItems.IndexOf(item);
        var to = Canvas.CanvasItems.Count - 1;
        if (from == to) return;
        UndoRedo.Execute(new ReorderCanvasItemCommand(Canvas.CanvasItems, from, to));
        MarkDirty(); StatusText = $"移至最前：{item.DisplayName}";
    }

    private void SendToBack()
    {
        var item = Canvas.SelectedItem; if (item == null) return;
        var from = Canvas.CanvasItems.IndexOf(item);
        if (from == 0) return;
        UndoRedo.Execute(new ReorderCanvasItemCommand(Canvas.CanvasItems, from, 0));
        MarkDirty(); StatusText = $"移至最後：{item.DisplayName}";
    }

    private void MoveUp()
    {
        var item = Canvas.SelectedItem; if (item == null) return;
        var from = Canvas.CanvasItems.IndexOf(item);
        var to = Math.Min(from + 1, Canvas.CanvasItems.Count - 1);
        if (from == to) return;
        UndoRedo.Execute(new ReorderCanvasItemCommand(Canvas.CanvasItems, from, to));
        MarkDirty();
    }

    private void MoveDown()
    {
        var item = Canvas.SelectedItem; if (item == null) return;
        var from = Canvas.CanvasItems.IndexOf(item);
        var to = Math.Max(from - 1, 0);
        if (from == to) return;
        UndoRedo.Execute(new ReorderCanvasItemCommand(Canvas.CanvasItems, from, to));
        MarkDirty();
    }

    private void ToggleLock()
    {
        var item = Canvas.SelectedItem; if (item == null) return;
        item.IsLocked = !item.IsLocked;
        MarkDirty();
        StatusText = item.IsLocked ? $"已鎖定：{item.DisplayName}" : $"已解鎖：{item.DisplayName}";
    }

    /// <summary>
    /// 對齊選取元件（靠左/右/上/下/水平置中/垂直置中）
    /// </summary>
    private void AlignItems(string mode)
    {
        var items = Canvas.GetAllSelectedItems()
            .Where(i => !i.IsLocked && i.ItemType != "Spacer")
            .ToList();
        if (items.Count == 0) return;

        double anchor = mode switch
        {
            "left"    => items.Min(i => i.X),
            "right"   => items.Max(i => i.X + i.Width),
            "top"     => items.Min(i => i.Y),
            "bottom"  => items.Max(i => i.Y + i.Height),
            "centerH" => items.Average(i => i.X + i.Width  / 2),
            "centerV" => items.Average(i => i.Y + i.Height / 2),
            _ => 0
        };

        var moves = new List<(DesignerItemViewModel, double, double, double, double)>();
        foreach (var item in items)
        {
            double newX = item.X, newY = item.Y;
            switch (mode)
            {
                case "left":    newX = anchor; break;
                case "right":   newX = anchor - item.Width; break;
                case "top":     newY = anchor; break;
                case "bottom":  newY = anchor - item.Height; break;
                case "centerH": newX = anchor - item.Width  / 2; break;
                case "centerV": newY = anchor - item.Height / 2; break;
            }
            if (Math.Abs(newX - item.X) < 0.01 && Math.Abs(newY - item.Y) < 0.01) continue;
            moves.Add((item, item.X, item.Y, newX, newY));
            item.X = newX;
            item.Y = newY;
        }

        if (moves.Count == 0) return;
        UndoRedo.Record(new CanvasMoveMultipleItemsCommand(moves));
        MarkDirty();
        string label = mode switch
        {
            "left"    => "靠左對齊",  "right"   => "靠右對齊",
            "top"     => "靠上對齊",  "bottom"  => "靠下對齊",
            "centerH" => "水平置中",  "centerV" => "垂直置中",
            _ => "對齊"
        };
        StatusText = $"{label} {items.Count} 個元件";
    }

    /// <summary>
    /// 平均分配間距（水平或垂直）
    /// 排序後：最左/最上元件位置固定，中間元件均等間距分配。
    /// </summary>
    private void DistributeItems(bool horizontal)
    {
        var items = Canvas.GetAllSelectedItems()
            .Where(i => !i.IsLocked && i.ItemType != "Spacer")
            .ToList();
        if (items.Count < 3) { StatusText = "平均分配需至少選取 3 個元件"; return; }

        // 排序
        if (horizontal)
            items = [.. items.OrderBy(i => i.X)];
        else
            items = [.. items.OrderBy(i => i.Y)];

        // 計算總跨度 與 各元件尺寸總和
        double totalSpan = horizontal
            ? (items[^1].X + items[^1].Width)  - items[0].X
            : (items[^1].Y + items[^1].Height) - items[0].Y;

        double totalSize = horizontal
            ? items.Sum(i => i.Width)
            : items.Sum(i => i.Height);

        double gap = (totalSpan - totalSize) / (items.Count - 1);

        var moves = new List<(DesignerItemViewModel, double, double, double, double)>();
        double cursor = horizontal ? items[0].X : items[0].Y;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double newX = horizontal ? cursor : item.X;
            double newY = horizontal ? item.Y  : cursor;

            if (Math.Abs(newX - item.X) > 0.01 || Math.Abs(newY - item.Y) > 0.01)
            {
                moves.Add((item, item.X, item.Y, newX, newY));
                item.X = newX;
                item.Y = newY;
            }

            cursor += (horizontal ? item.Width : item.Height) + gap;
        }

        if (moves.Count == 0) return;
        UndoRedo.Record(new CanvasMoveMultipleItemsCommand(moves));
        MarkDirty();
        StatusText = $"{(horizontal ? "水平" : "垂直")}均分 {items.Count} 個元件，間距 {gap:F1}px";
    }

    /// <summary>
    /// 將 GroupBox (Spacer) 內的子控件自動排列為水平列或垂直行，
    /// 子控件等寬/等高填滿可用區域，保留 padding 與間距。
    /// </summary>
    public void AutoArrangeGroupBox(DesignerItemViewModel spacer, bool horizontal)
    {
        const double Padding      = 8.0;
        const double Gap          = 8.0;
        const double HeaderHeight = 28.0;

        var groupBounds = new Rect(spacer.X, spacer.Y, spacer.Width, spacer.Height);
        var children = Canvas.CanvasItems
            .Where(i => !ReferenceEquals(i, spacer) && !i.IsLocked)
            .Where(i => groupBounds.Contains(new Point(i.X + i.Width / 2, i.Y + i.Height / 2)))
            .ToList();

        if (children.Count == 0) return;

        double innerX = spacer.X + Padding;
        double innerY = spacer.Y + HeaderHeight + Padding;
        double innerW = spacer.Width  - 2 * Padding;
        double innerH = spacer.Height - HeaderHeight - 2 * Padding;
        int n = children.Count;

        var resizes = new List<(DesignerItemViewModel item,
            double oldX, double oldY, double oldW, double oldH,
            double newX, double newY, double newW, double newH)>();

        for (int i = 0; i < n; i++)
        {
            var child = children[i];
            double oldX = child.X, oldY = child.Y, oldW = child.Width, oldH = child.Height;
            double newX, newY, newW, newH;

            if (horizontal)
            {
                newW = Math.Max(40, (innerW - (n - 1) * Gap) / n);
                newH = Math.Max(30, innerH);
                newX = innerX + i * (newW + Gap);
                newY = innerY;
            }
            else
            {
                newW = Math.Max(40, innerW);
                newH = Math.Max(30, (innerH - (n - 1) * Gap) / n);
                newX = innerX;
                newY = innerY + i * (newH + Gap);
            }

            child.SetPropDirect("x", newX);
            child.SetPropDirect("y", newY);
            child.SetPropDirect("width",  newW);
            child.SetPropDirect("height", newH);
            resizes.Add((child, oldX, oldY, oldW, oldH, newX, newY, newW, newH));
        }

        RecordCanvasMultiResize(resizes);
        StatusText = $"{(horizontal ? "水平" : "垂直")}自動排列 {n} 個元件";
    }

    /// <summary>
    /// 將 GroupBox 內子控件以指定欄數排列成格狀（N 欄 × M 列）。
    /// </summary>
    public void AutoArrangeGroupBoxGrid(DesignerItemViewModel spacer, int columns)
    {
        const double Padding      = 8.0;
        const double Gap          = 8.0;
        const double HeaderHeight = 28.0;

        var groupBounds = new Rect(spacer.X, spacer.Y, spacer.Width, spacer.Height);
        var children = Canvas.CanvasItems
            .Where(i => !ReferenceEquals(i, spacer) && !i.IsLocked)
            .Where(i => groupBounds.Contains(new Point(i.X + i.Width / 2, i.Y + i.Height / 2)))
            .ToList();

        if (children.Count == 0) return;

        int n    = children.Count;
        int cols = Math.Max(1, Math.Min(columns, n));
        int rows = (int)Math.Ceiling((double)n / cols);

        double innerX = spacer.X + Padding;
        double innerY = spacer.Y + HeaderHeight + Padding;
        double innerW = spacer.Width  - 2 * Padding;
        double innerH = spacer.Height - HeaderHeight - 2 * Padding;

        double cellW = Math.Max(40, (innerW - (cols - 1) * Gap) / cols);
        double cellH = Math.Max(30, (innerH - (rows - 1) * Gap) / rows);

        var resizes = new List<(DesignerItemViewModel item,
            double oldX, double oldY, double oldW, double oldH,
            double newX, double newY, double newW, double newH)>();

        for (int i = 0; i < n; i++)
        {
            var child = children[i];
            int col = i % cols;
            int row = i / cols;
            double newX = innerX + col * (cellW + Gap);
            double newY = innerY + row * (cellH + Gap);

            resizes.Add((child, child.X, child.Y, child.Width, child.Height,
                         newX, newY, cellW, cellH));

            child.SetPropDirect("x",      newX);
            child.SetPropDirect("y",      newY);
            child.SetPropDirect("width",  cellW);
            child.SetPropDirect("height", cellH);
        }

        RecordCanvasMultiResize(resizes);
        StatusText = $"格狀排列 {n} 個元件（{cols} 欄 × {rows} 列）";
    }

    private void PerformUndo()
    {
        UndoRedo.Undo();
        StatusText = "復原";
    }

    private void PerformRedo()
    {
        UndoRedo.Redo();
        StatusText = "重做";
    }

    // ── Helpers ──────────────────────────────────────────────────────

    public void MarkDirty()
    {
        IsDirty = true;
    }

    private void LoadDocumentIntoUI()
    {
        _layoutMode = _document.Layout.Mode;
        _leftCommandWidthPx = _document.Layout.LeftCommandWidthPx;
        _rightColumnWidthStar = _document.Layout.RightColumnWidthStar;
        _showLiveLog = _document.Layout.ShowLiveLog;
        _showAlarmViewer = _document.Layout.ShowAlarmViewer;
        _showSensorViewer = _document.Layout.ShowSensorViewer;
        _docTitle     = _document.Meta.Title;
        _machineId    = _document.Meta.MachineId;
        _plcIp        = _document.Meta.PlcIp;
        _plcPort      = _document.Meta.PlcPort;
        _scanInterval = _document.Meta.ScanInterval;

        N(nameof(LayoutMode));
        N(nameof(IsDashboardMode));
        N(nameof(LeftCommandWidthPx));
        N(nameof(RightColumnWidthStar));
        N(nameof(ShowLiveLog));
        N(nameof(ShowAlarmViewer));
        N(nameof(ShowSensorViewer));
        N(nameof(DocTitle));
        N(nameof(MachineId));
        N(nameof(PlcIp));
        N(nameof(PlcPort));
        N(nameof(ScanInterval));

        Canvas.LoadFromDocument(_document);
        // 通知代理屬性更新（Canvas.LoadFromDocument 直接設值，MainVM 需手動通知）
        N(nameof(CanvasWidth));
        N(nameof(CanvasHeight));
    }

    private void SyncUIToDocument()
    {
        _document.Meta.Title        = DocTitle;
        _document.Meta.MachineId    = MachineId;
        _document.Meta.PlcIp        = PlcIp;
        _document.Meta.PlcPort      = PlcPort;
        _document.Meta.ScanInterval = ScanInterval;

        _document.Layout.Mode = LayoutMode;
        _document.Layout.LeftCommandWidthPx = LeftCommandWidthPx;
        _document.Layout.RightColumnWidthStar = RightColumnWidthStar;
        _document.Layout.ShowLiveLog = ShowLiveLog;
        _document.Layout.ShowAlarmViewer = ShowAlarmViewer;
        _document.Layout.ShowSensorViewer = ShowSensorViewer;

        _document.CanvasItems = Canvas.ExportCanvasItems();
        _document.CanvasWidth = Canvas.CanvasWidth;
        _document.CanvasHeight = Canvas.CanvasHeight;
    }

    private static readonly TimeSpan _propDebounce = TimeSpan.FromMilliseconds(300);

    private void UpdateStatusFromSelection()
    {
        var item = Canvas.SelectedItem;
        if (item == null) { StatusText = "就緒"; return; }
        StatusText = $"已選取：{item.DisplayName} [{item.X:F0}, {item.Y:F0}]";
    }

    private void OnItemPropCommitted(string propKey, object? oldValue, object? newValue)
    {
        if (_subscribedPropItem == null) return;

        var now = DateTime.UtcNow;

        // ── 多選同步：X/Y/W/H 套用至所有已選元件 ──────────────────────
        bool isSpatialProp = propKey is "x" or "y" or "width" or "height";
        if (isSpatialProp)
        {
            var allSelected = Canvas.GetAllSelectedItems();
            if (allSelected.Count > 1)
            {
                double newDouble = Convert.ToDouble(newValue);
                double delta     = newDouble - Convert.ToDouble(oldValue);

                var changes = new List<(DesignerItemViewModel, string, object?, object?)>
                {
                    (_subscribedPropItem, propKey, oldValue, newValue)
                };

                foreach (var item in allSelected)
                {
                    if (ReferenceEquals(item, _subscribedPropItem)) continue;
                    double itemOld = propKey switch
                    {
                        "x"      => item.X,      "y"      => item.Y,
                        "width"  => item.Width,  "height" => item.Height,
                        _ => 0
                    };
                    // W/H：套用絕對值（全部同寬/高）；X/Y：套用相同位移
                    double itemNew = propKey is "width" or "height"
                        ? newDouble
                        : Math.Max(0, itemOld + delta);

                    item.SetPropDirect(propKey, itemNew);
                    changes.Add((item, propKey, (object)itemOld, (object)itemNew));
                }

                UndoRedo.Record(new MultiItemPropertyChangeCommand(changes));
                _lastPropSnapshot = null;
                UpdateStatusFromSelection();
                return;
            }
        }

        // ── 單選防抖邏輯（原有）─────────────────────────────────────────
        if (_lastPropSnapshot is { } snap &&
            ReferenceEquals(snap.Item, _subscribedPropItem) &&
            snap.PropKey == propKey &&
            (now - snap.At) < _propDebounce)
        {
            UndoRedo.UpdateTopPropertyCommand(_subscribedPropItem, propKey, newValue);
            _lastPropSnapshot = snap with { At = now };
            return;
        }

        UndoRedo.Record(new PropertyChangeCommand(_subscribedPropItem, propKey, oldValue, newValue));
        _lastPropSnapshot = new PropSnapshot(_subscribedPropItem, propKey, oldValue, now);
        UpdateStatusFromSelection();
    }

    private static bool ConfirmDiscard()
    {
        var result = MessageBox.Show(
            "目前文件尚未儲存，確定要捨棄變更嗎？",
            "未儲存的變更",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }
}
