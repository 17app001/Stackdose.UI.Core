using Stackdose.App.DesignPlayer.Models;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.DesignPlayer.Pages;

public partial class MonitorPage : UserControl
{
    private readonly PlayerAppConfig _config;
    private PlcStatus? _plcStatus;
    private FileSystemWatcher? _fileWatcher;
    private string? _loadedDesignPath;
    private DateTime _lastHotReload = DateTime.MinValue;

    public MonitorPage(PlayerAppConfig config)
    {
        _config = config;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ── 生命週期 ──────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupPlcStatus();
        LoadDesign();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // PlcStatus 的 Dispose 由 MainWindow.Closing 統一處理
    }

    // ── PLC 連線 ──────────────────────────────────────────────────────────

    private void SetupPlcStatus()
    {
        if (_plcStatus != null) return;

        _plcStatus = new PlcStatus
        {
            IpAddress    = _config.Plc.Ip,
            Port         = _config.Plc.Port,
            ScanInterval = _config.Plc.PollIntervalMs,
            AutoConnect  = _config.Plc.AutoConnect,
            IsGlobal     = true,
            ShowBorder   = true,
        };

        _plcStatus.ConnectionEstablished += _ =>
            Dispatcher.BeginInvoke(RefreshMonitors);

        PlcStatusHost.Child = _plcStatus;
    }

    /// <summary>
    /// 重新載入設計稿後呼叫，確保新 PlcLabel 地址加入掃描清單。
    /// </summary>
    public void RefreshMonitors() => _plcStatus?.RefreshMonitors();

    /// <summary>
    /// 應用程式關閉前呼叫，釋放 PLC 資源與檔案監聽器。
    /// </summary>
    public void DisposePlc()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = null;

        PlcContext.GlobalStatus = null;
        _plcStatus?.Dispose();
        PlcStatusHost.Child = null;
        _plcStatus = null;
    }

    // ── 設計稿載入 ────────────────────────────────────────────────────────

    public void LoadDesign()
    {
        var path = ResolvePath(_config.DesignFile);
        _loadedDesignPath = path;

        lblDesignFile.Text = Path.GetFileName(path);

        if (!File.Exists(path))
        {
            EmptyHint.Visibility = Visibility.Visible;
            EmptyHintSub.Text = $"找不到 {path}";
            return;
        }

        try
        {
            var doc = DesignFileService.Load(path);
            RenderDocument(doc);
            EmptyHint.Visibility = Visibility.Collapsed;
            SetupFileWatcher(path);
        }
        catch (Exception ex)
        {
            EmptyHint.Visibility = Visibility.Visible;
            EmptyHintSub.Text = $"載入失敗：{ex.Message}";
        }
    }

    // ── Hot-Reload ────────────────────────────────────────────────────────

    private void SetupFileWatcher(string path)
    {
        _fileWatcher?.Dispose();

        var dir  = Path.GetDirectoryName(path);
        var file = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;

        _fileWatcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter       = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _fileWatcher.Changed += OnDesignFileChanged;
    }

    private void OnDesignFileChanged(object sender, FileSystemEventArgs e)
    {
        // 防抖：800ms 內同一路徑只觸發一次
        var now = DateTime.UtcNow;
        if ((now - _lastHotReload).TotalMilliseconds < 800) return;
        _lastHotReload = now;

        // 延遲讀取：等待寫入完成（設計器存檔時可能多次觸發 Changed）
        Task.Delay(400).ContinueWith(_ =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var doc = DesignFileService.Load(e.FullPath);
                    RenderDocument(doc);
                    EmptyHint.Visibility = Visibility.Collapsed;
                    lblDesignFile.Text   = $"↺ {Path.GetFileName(e.FullPath)}";
                }
                catch { /* 靜默失敗，等下次更新觸發 */ }
            });
        });
    }

    private void RenderDocument(DesignDocument doc)
    {
        MonitorCanvas.Children.Clear();
        MonitorCanvas.Width  = doc.CanvasWidth;
        MonitorCanvas.Height = doc.CanvasHeight;
        CanvasBorder.Width   = doc.CanvasWidth;
        CanvasBorder.Height  = doc.CanvasHeight;

        int ok = 0, err = 0;
        foreach (var def in doc.CanvasItems)
        {
            UIElement control;
            try   { control = RuntimeControlFactory.Create(def); ok++;  }
            catch (Exception ex) { control = MakeErrorPlaceholder(def, ex.Message); err++; }

            if (control is FrameworkElement fe)
            {
                fe.Width  = def.Width;
                fe.Height = def.Height;
            }

            Canvas.SetLeft(control, def.X);
            Canvas.SetTop(control,  def.Y);
            MonitorCanvas.Children.Add(control);
        }

        lblCanvasInfo.Text = $"{doc.CanvasWidth:F0} × {doc.CanvasHeight:F0} px　{ok} 個元件"
                           + (err > 0 ? $"（{err} 個錯誤）" : "");

        // 所有 PlcLabel Loaded 後刷新 Monitor 讓新地址加入掃描清單
        Dispatcher.BeginInvoke(
            RefreshMonitors,
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string msg)
    {
        return new Border
        {
            Width = def.Width, Height = def.Height,
            BorderBrush     = System.Windows.Media.Brushes.OrangeRed,
            BorderThickness  = new Thickness(1),
            Background      = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
            Child = new TextBlock
            {
                Text         = $"[{def.Type}] {msg}",
                Foreground   = System.Windows.Media.Brushes.OrangeRed,
                FontSize     = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
    }

    // ── 工具 ──────────────────────────────────────────────────────────────

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
