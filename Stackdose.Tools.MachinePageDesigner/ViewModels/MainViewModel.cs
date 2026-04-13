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
        _docTitle = _document.Meta.Title;
        _machineId = _document.Meta.MachineId;

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

    // ── UndoRedo 整合方法（供 View 呼叫） ────────────────────────────

    /// <summary>
    /// 在自由畫布新增元件（透過 UndoRedo）
    /// </summary>
    public void ExecuteCanvasAddItem(DesignerItemViewModel vm)
    {
        var cmd = new CanvasAddItemCommand(Canvas.CanvasItems, vm);
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

        Canvas.LoadFromDocument(_document);
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
