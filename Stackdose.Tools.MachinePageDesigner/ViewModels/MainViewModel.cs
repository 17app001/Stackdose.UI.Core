using System.IO;
using System.Windows;
using System.Windows.Input;
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
    public DesignCanvasViewModel Canvas { get; } = new();
    public ToolboxViewModel Toolbox { get; } = new();

    // ── Document state ───────────────────────────────────────────────
    private DesignDocument _document = new();
    private string? _currentFilePath;
    private bool _isDirty;
    private string _statusText = "就緒";

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
        DeleteSelectedCmd = new RelayCommand(_ => DeleteSelected(), _ => Canvas.HasSelectedItem);

        // UndoRedo 狀態變更時更新 dirty
        UndoRedo.StateChanged += () => MarkDirty();

        // 載入預設文件
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

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand NewCmd { get; }
    public ICommand OpenCmd { get; }
    public ICommand SaveCmd { get; }
    public ICommand SaveAsCmd { get; }
    public ICommand UndoCmd { get; }
    public ICommand RedoCmd { get; }
    public ICommand DeleteSelectedCmd { get; }

    // ── UndoRedo 整合方法（供 View 呼叫） ────────────────────────────

    /// <summary>
    /// 透過 UndoRedo 新增項目至 Zone
    /// </summary>
    public void ExecuteAddItem(ZoneViewModel zone, DesignerItemViewModel item, int index = -1)
    {
        var actualIndex = index >= 0 ? index : zone.Items.Count;
        var cmd = new AddItemCommand(zone, item, actualIndex);
        UndoRedo.Execute(cmd);
    }

    /// <summary>
    /// 透過 UndoRedo 移除項目
    /// </summary>
    public void ExecuteRemoveItem(ZoneViewModel zone, DesignerItemViewModel item)
    {
        var cmd = new RemoveItemCommand(zone, item);
        UndoRedo.Execute(cmd);
    }

    /// <summary>
    /// 透過 UndoRedo 移動項目
    /// </summary>
    public void ExecuteMoveItem(ZoneViewModel zone, int fromIndex, int toIndex)
    {
        var cmd = new MoveItemCommand(zone, fromIndex, toIndex);
        UndoRedo.Execute(cmd);
    }

    /// <summary>
    /// 透過 UndoRedo 變更屬性
    /// </summary>
    public void ExecutePropertyChange(DesignerItemViewModel item, string propKey, object? oldValue, object? newValue)
    {
        var cmd = new PropertyChangeCommand(item, propKey, oldValue, newValue);
        UndoRedo.Execute(cmd);
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
        var items = Canvas.GetAllSelectedItems();
        if (items.Count == 0) return;

        if (items.Count == 1)
        {
            // 單選刪除
            var item = items[0];
            var zone = Canvas.FindZoneOf(item);
            if (zone != null)
            {
                ExecuteRemoveItem(zone, item);
                Canvas.ClearSelection();
            }
        }
        else
        {
            // 多選批次刪除
            var entries = new List<(ZoneViewModel zone, DesignerItemViewModel item, int index)>();
            foreach (var item in items)
            {
                var zone = Canvas.FindZoneOf(item);
                if (zone != null)
                    entries.Add((zone, item, zone.Items.IndexOf(item)));
            }
            if (entries.Count > 0)
            {
                var cmd = new BatchDeleteCommand(entries);
                UndoRedo.Execute(cmd);
                Canvas.ClearSelection();
            }
        }
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

        // Raise all property changed
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

        _document.Zones = Canvas.ExportZones();
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