using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 頂層 ViewModel，協調所有子模組
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    // ── Sub-ViewModels ───────────────────────────────────────────────
    public ToolboxViewModel Toolbox { get; } = new();

    // ── Pages ────────────────────────────────────────────────────────
    public ObservableCollection<PageTabViewModel> Pages { get; } = [];

    private PageTabViewModel? _currentPage;

    public PageTabViewModel? CurrentPage
    {
        get => _currentPage;
        set => SetCurrentPage(value);
    }

    /// <summary>目前頁面的畫布 ViewModel（供 FreeCanvas / PropertyPanel 綁定用）</summary>
    public DesignCanvasViewModel Canvas => _currentPage?.Canvas ?? _fallbackCanvas;

    /// <summary>目前頁面的 UndoRedo 服務</summary>
    public UndoRedoService UndoRedo => _currentPage?.UndoRedo ?? _fallbackUndoRedo;

    private static readonly DesignCanvasViewModel _fallbackCanvas   = new();
    private static readonly UndoRedoService       _fallbackUndoRedo = new();

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

    // ── Clipboard ────────────────────────────────────────────────────
    private List<DesignerItemDefinition> _clipboard = [];
    private int _pasteCount;

    // ── Services（UndoRedo 現為 CurrentPage 代理，保留空行以免誤刪其他成員）─

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
        _docTitle = _document.Meta.Title;
        _machineId = _document.Meta.MachineId;

        // 初始化 Commands
        NewCmd = new RelayCommand(_ => NewDocument());
        OpenCmd = new RelayCommand(_ => OpenDocument());
        SaveCmd = new RelayCommand(_ => SaveDocument());
        SaveAsCmd = new RelayCommand(_ => SaveDocumentAs());
        UndoCmd = new RelayCommand(_ => PerformUndo(), _ => UndoRedo?.CanUndo == true);
        RedoCmd = new RelayCommand(_ => PerformRedo(), _ => UndoRedo?.CanRedo == true);
        AddPageCmd    = new RelayCommand(_ => AddPage());
        DeletePageCmd = new RelayCommand(p => DeletePage(p as PageTabViewModel),
                                         p => Pages.Count > 1);
        SelectPageCmd = new RelayCommand(p => { if (p is PageTabViewModel vm) SetCurrentPage(vm); });
        RenamePageCmd = new RelayCommand(p => { if (p is PageTabViewModel vm) vm.IsEditing = true; });
        DeleteSelectedCmd  = new RelayCommand(_ => DeleteSelected(),  _ => Canvas.HasSelectedItem);
        BringToFrontCmd    = new RelayCommand(_ => BringToFront(),   _ => Canvas.HasSelectedItem);
        SendToBackCmd      = new RelayCommand(_ => SendToBack(),     _ => Canvas.HasSelectedItem);
        MoveUpCmd          = new RelayCommand(_ => MoveUp(),         _ => Canvas.HasSelectedItem);
        MoveDownCmd        = new RelayCommand(_ => MoveDown(),       _ => Canvas.HasSelectedItem);
        LockToggleCmd      = new RelayCommand(_ => ToggleLock(),     _ => Canvas.HasSelectedItem);
        CopyCmd            = new RelayCommand(_ => CopySelected(),   _ => Canvas.HasSelectedItem);
        PasteCmd           = new RelayCommand(_ => PasteClipboard(), _ => _clipboard.Count > 0);
        SelectAllCmd       = new RelayCommand(_ => SelectAll());

        AlignLeftCmd    = new RelayCommand(_ => AlignItems("left"),    _ => Canvas.HasSelectedItem);
        AlignRightCmd   = new RelayCommand(_ => AlignItems("right"),   _ => Canvas.HasSelectedItem);
        AlignTopCmd     = new RelayCommand(_ => AlignItems("top"),     _ => Canvas.HasSelectedItem);
        AlignBottomCmd  = new RelayCommand(_ => AlignItems("bottom"),  _ => Canvas.HasSelectedItem);
        AlignCenterHCmd = new RelayCommand(_ => AlignItems("centerH"), _ => Canvas.HasSelectedItem);
        AlignCenterVCmd = new RelayCommand(_ => AlignItems("centerV"), _ => Canvas.HasSelectedItem);

        DistributeHorizCmd = new RelayCommand(_ => DistributeItems(horizontal: true),  _ => Canvas.HasSelectedItem);
        DistributeVertCmd  = new RelayCommand(_ => DistributeItems(horizontal: false), _ => Canvas.HasSelectedItem);

        // 初始化時建立第一個頁面（LoadDocumentIntoUI 會設定 CurrentPage → 觸發事件訂閱）
        LoadDocumentIntoUI();
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
            var v = Math.Max(400, Math.Min(value, 4000));
            if (Canvas.CanvasWidth == v) return;
            Canvas.CanvasWidth = v;
            N();
            MarkDirty();
        }
    }

    public double CanvasHeight
    {
        get => Canvas.CanvasHeight;
        set
        {
            var v = Math.Max(300, Math.Min(value, 3000));
            if (Canvas.CanvasHeight == v) return;
            Canvas.CanvasHeight = v;
            N();
            MarkDirty();
        }
    }

    // ── Layout Properties ────────────────────────────────────────────

    public string LayoutMode
    {
        get => _layoutMode;
        set { if (Set(ref _layoutMode, value)) MarkDirty(); }
    }

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

    public string[] LayoutModes { get; } = ["SplitRight", "Standard", "SplitBottom"];

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

    // Page Management
    public ICommand AddPageCmd    { get; }
    public ICommand DeletePageCmd { get; }
    public ICommand SelectPageCmd { get; }
    public ICommand RenamePageCmd { get; }

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
        foreach (var p in Pages) p.UndoRedo.Clear();
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
            foreach (var p in Pages) p.UndoRedo.Clear();
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
        var items = Canvas.GetAllSelectedItems();
        if (items.Count == 0) return;
        _clipboard = items.Select(i => i.ToDefinition().Clone()).ToList();
        _pasteCount = 0;
        Application.Current?.Dispatcher.InvokeAsync(
            CommandManager.InvalidateRequerySuggested,
            DispatcherPriority.Background);
        StatusText = $"已複製 {_clipboard.Count} 個元件";
    }

    private void PasteClipboard()
    {
        if (_clipboard.Count == 0) return;
        _pasteCount++;
        double offset = _pasteCount * 20;

        var newVms = _clipboard
            .Select(def => new DesignerItemViewModel(def.Clone(offset, offset)))
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
        var item = Canvas.SelectedItem;
        if (item == null) return;
        var cmd = new CanvasRemoveItemCommand(Canvas.CanvasItems, item);
        UndoRedo.Execute(cmd);
        Canvas.ClearSelection();
        StatusText = $"已刪除：{item.DisplayName}";
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

    // ── Page Management ──────────────────────────────────────────────

    private void SetCurrentPage(PageTabViewModel? page)
    {
        // 解除舊頁面事件訂閱
        if (_currentPage != null)
        {
            _currentPage.Canvas.PropertyChanged -= OnCanvasPropertyChanged;
            _currentPage.UndoRedo.StateChanged  -= OnUndoRedoStateChanged;
            if (_subscribedPropItem != null)
                _subscribedPropItem.PropCommitted -= OnItemPropCommitted;
            _subscribedPropItem  = null;
            _lastPropSnapshot    = null;
        }

        // 更新所有頁籤 IsActive
        foreach (var p in Pages)
            p.IsActive = ReferenceEquals(p, page);

        _currentPage = page;
        N(nameof(CurrentPage));
        N(nameof(Canvas));
        N(nameof(UndoRedo));
        N(nameof(CanvasWidth));
        N(nameof(CanvasHeight));

        // 訂閱新頁面事件
        if (_currentPage != null)
        {
            _currentPage.Canvas.PropertyChanged += OnCanvasPropertyChanged;
            _currentPage.UndoRedo.StateChanged  += OnUndoRedoStateChanged;
        }

        UpdateStatusFromSelection();
        Application.Current?.Dispatcher.InvokeAsync(
            CommandManager.InvalidateRequerySuggested,
            DispatcherPriority.Background);
    }

    private void OnCanvasPropertyChanged(object? _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DesignCanvasViewModel.SelectedItem)) return;

        if (_subscribedPropItem != null)
            _subscribedPropItem.PropCommitted -= OnItemPropCommitted;

        _subscribedPropItem = Canvas.SelectedItem;

        if (_subscribedPropItem != null)
            _subscribedPropItem.PropCommitted += OnItemPropCommitted;

        UpdateStatusFromSelection();
    }

    private void OnUndoRedoStateChanged()
    {
        MarkDirty();
        Application.Current?.Dispatcher.InvokeAsync(
            CommandManager.InvalidateRequerySuggested,
            DispatcherPriority.Background);
    }

    private void AddPage()
    {
        var page = new DesignPage
        {
            PageId      = Guid.NewGuid().ToString("N")[..8],
            Name        = $"Page {Pages.Count + 1}",
            CanvasWidth  = Canvas.CanvasWidth,
            CanvasHeight = Canvas.CanvasHeight,
        };
        var vm = new PageTabViewModel(page);
        Pages.Add(vm);
        SetCurrentPage(vm);
        MarkDirty();
        StatusText = $"已新增頁面：{vm.Name}";
    }

    private void DeletePage(PageTabViewModel? page)
    {
        if (page == null || Pages.Count <= 1)
        {
            StatusText = "至少需要保留一個頁面";
            return;
        }
        var idx = Pages.IndexOf(page);
        Pages.Remove(page);
        if (ReferenceEquals(page, _currentPage))
            SetCurrentPage(Pages[Math.Max(0, Math.Min(idx, Pages.Count - 1))]);
        MarkDirty();
        StatusText = $"已刪除頁面：{page.Name}";
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
        _docTitle = _document.Meta.Title;
        _machineId = _document.Meta.MachineId;

        N(nameof(LayoutMode));
        N(nameof(LeftCommandWidthPx));
        N(nameof(RightColumnWidthStar));
        N(nameof(ShowLiveLog));
        N(nameof(ShowAlarmViewer));
        N(nameof(ShowSensorViewer));
        N(nameof(DocTitle));
        N(nameof(MachineId));

        // 建立頁面 ViewModels
        Pages.Clear();
        foreach (var page in _document.Pages ?? [])
            Pages.Add(new PageTabViewModel(page));

        // 確保至少有一頁
        if (Pages.Count == 0)
            Pages.Add(new PageTabViewModel(new DesignPage()));

        SetCurrentPage(Pages[0]);
    }

    private void SyncUIToDocument()
    {
        _document.Meta.Title = DocTitle;
        _document.Meta.MachineId = MachineId;

        _document.Layout.Mode = LayoutMode;
        _document.Layout.LeftCommandWidthPx = LeftCommandWidthPx;
        _document.Layout.RightColumnWidthStar = RightColumnWidthStar;
        _document.Layout.ShowLiveLog = ShowLiveLog;
        _document.Layout.ShowAlarmViewer = ShowAlarmViewer;
        _document.Layout.ShowSensorViewer = ShowSensorViewer;

        // 匯出所有頁面（DesignFileService.Save 會同步 Legacy 欄位）
        _document.Pages = Pages.Select(p => p.Export()).ToList();
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
