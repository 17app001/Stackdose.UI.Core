using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Stackdose.App.ShellShared.Services;
using Stackdose.Hardware.Plc;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.App.DesignRuntime;

public partial class MainWindow : Window
{
    private PlcStatus? _plcStatus;
    private CancellationTokenSource? _simCts;

    public MainWindow()
    {
        InitializeComponent();
    }

    // ── PLC 連線 ──────────────────────────────────────────────────────────

    private void OnConnectClick(object sender, RoutedEventArgs e)
    {
        var ip = txtIp.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip)) { ShowStatus("請輸入 PLC IP 位址", error: true); return; }

        if (!int.TryParse(txtPort.Text.Trim(), out var port) || port <= 0)
            port = 3000;

        if (!int.TryParse(txtScan.Text.Trim(), out var scan) || scan < 50)
            scan = 200;

        // 移除舊的 PlcStatus
        if (_plcStatus != null)
        {
            _plcStatus.Dispose();
            plcStatusHost.Child = null;
        }

        // 建立新的 PlcStatus（Loaded 後自動連線）
        _plcStatus = new PlcStatus
        {
            IpAddress = ip,
            Port = port,
            AutoConnect = true,
            IsGlobal = true,
            ScanInterval = scan,
            ShowBorder = true,
        };

        _plcStatus.ConnectionEstablished += mgr =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                ShowStatus($"PLC 已連線：{ip}:{port}");
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
            });
        };

        plcStatusHost.Child = _plcStatus;
        ShowStatus($"正在連線至 {ip}:{port} …");
    }

    private void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        // 先停止亂數模擬（若正在執行）
        if (_simCts != null)
        {
            _simCts.Cancel();
            _simCts = null;
            btnRandomSim.Content = "🎲 亂數 D100~D102";
            btnRandomSim.Background = s_simIdleColor;
        }

        // 清除 GlobalStatus 讓 Dispose() 真的執行斷線
        PlcContext.GlobalStatus = null;
        _plcStatus?.Dispose();
        plcStatusHost.Child = null;
        _plcStatus = null;

        btnConnect.IsEnabled = true;
        btnDisconnect.IsEnabled = false;
        ShowStatus("已斷線");
    }

    // ── 開啟 JSON ─────────────────────────────────────────────────────────

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "選擇 MachineDesign JSON 檔案",
            Filter = "Machine Design (*.machinedesign.json)|*.machinedesign.json|JSON (*.json)|*.json|All (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;
        LoadFile(dlg.FileName);
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        var json = files.FirstOrDefault(f =>
            f.EndsWith(".machinedesign.json", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        if (json != null) LoadFile(json);
    }

    // ── 縮放 ──────────────────────────────────────────────────────────────

    private void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (canvasScale == null) return;
        canvasScale.ScaleX = e.NewValue;
        canvasScale.ScaleY = e.NewValue;
        lblZoom.Text = $"{e.NewValue * 100:F0}%";
    }

    // ── 載入並渲染 ────────────────────────────────────────────────────────

    private void LoadFile(string path)
    {
        try
        {
            var doc = DesignFileService.Load(path);
            RenderDocument(doc, path);
            lblFilePath.Text = path;
            lblFilePath.Foreground = System.Windows.Media.Brushes.LightGray;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            ShowStatus($"載入失敗：{ex.Message}", error: true);
        }
    }

    private void RenderDocument(DesignDocument doc, string filePath)
    {
        // 清除舊有元件
        runtimeCanvas.Children.Clear();

        // 套用畫布尺寸
        runtimeCanvas.Width = doc.CanvasWidth;
        runtimeCanvas.Height = doc.CanvasHeight;
        canvasBorder.Width = doc.CanvasWidth;
        canvasBorder.Height = doc.CanvasHeight;

        int okCount = 0;
        int errorCount = 0;

        // 依 Z-order 建立控制項（canvasItems[0] = 最底層）
        foreach (var def in doc.CanvasItems)
        {
            UIElement control;
            try
            {
                control = RuntimeControlFactory.Create(def);
                okCount++;
            }
            catch (Exception ex)
            {
                control = MakeErrorPlaceholder(def, ex.Message);
                errorCount++;
            }

            // 設定尺寸
            if (control is FrameworkElement fe)
            {
                fe.Width = def.Width;
                fe.Height = def.Height;
            }

            Canvas.SetLeft(control, def.X);
            Canvas.SetTop(control, def.Y);
            runtimeCanvas.Children.Add(control);
        }

        // 更新 UI
        var fileName = Path.GetFileName(filePath);
        Title = $"DesignRuntime — {fileName}";
        lblCanvasInfo.Text = $"畫布：{doc.CanvasWidth:F0} × {doc.CanvasHeight:F0} px";
        lblItemCount.Text = $"元件：{okCount} 個" + (errorCount > 0 ? $"（{errorCount} 個錯誤）" : "");

        var status = $"已載入：{fileName}  共 {doc.CanvasItems.Count} 個元件";
        if (errorCount > 0) status += $"，{errorCount} 個建立失敗（橘框標示）";
        ShowStatus(status, error: errorCount > 0);

        // 套用 Shell 策略（FreeCanvas / SinglePage / Standard）
        ApplyShellStrategy(doc);

        // 等所有 PlcLabel 的 Loaded 事件執行完後，刷新 Monitor 讓新地址加入掃描清單
        Dispatcher.BeginInvoke(
            () => _plcStatus?.RefreshMonitors(),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 根據 DesignDocument.ShellMode 選擇策略，切換 Row 2 的顯示模式。
    /// FreeCanvas：原始 ScrollViewer + Canvas，保留縮放功能。
    /// SinglePage / Standard：將 canvasHost 遷移進 Shell 容器後顯示。
    /// </summary>
    private void ApplyShellStrategy(DesignDocument doc)
    {
        var strategy = ShellStrategyFactory.Select(doc.ShellMode);
        lblShellMode.Text = $"Shell: {strategy.LayoutMode}";

        if (strategy is FreeCanvasShellStrategy)
        {
            // 確保 canvasHost 回到 scrollViewerCanvas（可能前次已被遷移）
            if (!ReferenceEquals(scrollViewerCanvas.Content, canvasHost))
                scrollViewerCanvas.Content = canvasHost;

            scrollViewerCanvas.Visibility = Visibility.Visible;
            shellPreviewHost.Content      = null;
            shellPreviewHost.Visibility   = Visibility.Collapsed;
        }
        else
        {
            // 將 canvasHost 從 scrollViewerCanvas 遷移出，放入新的 ScrollViewer
            scrollViewerCanvas.Content = null;
            var innerScroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x1E)),
                Padding    = new Thickness(40),
                Content    = canvasHost,
            };

            shellPreviewHost.Content    = strategy.Wrap(innerScroller, doc.Meta.Title, doc.Meta.MachineId);
            shellPreviewHost.Visibility = Visibility.Visible;
            scrollViewerCanvas.Visibility = Visibility.Collapsed;
        }
    }

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string message)
    {
        return new Border
        {
            Width = def.Width,
            Height = def.Height,
            BorderBrush = System.Windows.Media.Brushes.OrangeRed,
            BorderThickness = new Thickness(1),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
            Child = new TextBlock
            {
                Text = $"[{def.Type}] {message}",
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
    }

    // ── 模擬器模式 ────────────────────────────────────────────────────

    private void OnSimModeChanged(object sender, RoutedEventArgs e)
    {
        PlcClientFactory.UseSimulator = chkSimMode.IsChecked == true;
        ShowStatus(PlcClientFactory.UseSimulator
            ? "模擬器模式已啟用 — 按「連線」即可使用內建模擬器"
            : "已切換回真實 PLC 模式");
    }

    // ── 亂數注入 D100~D102 ───────────────────────────────────────────

    private static readonly SolidColorBrush s_simActiveColor =
        new(Color.FromRgb(0xFF, 0x80, 0x00));
    private static readonly SolidColorBrush s_simIdleColor =
        new(Color.FromRgb(0x5C, 0x6B, 0xC0));

    private async void OnRandomSimClick(object sender, RoutedEventArgs e)
    {
        // ── 停止 ──
        if (_simCts != null)
        {
            _simCts.Cancel();
            _simCts = null;
            btnRandomSim.Content = "🎲 亂數 D100~D102";
            btnRandomSim.Background = s_simIdleColor;
            ShowStatus("亂數模擬已停止");
            return;
        }

        // ── 啟動 ──
        var manager = _plcStatus?.CurrentManager;
        if (manager == null || !manager.IsConnected)
        {
            ShowStatus("請先連線 PLC（或勾選「模擬器模式」後按連線）", error: true);
            return;
        }

        // 確保 D100~D102 已納入 Monitor 掃描清單（連線後才載入 JSON 時可能未被註冊）
        manager.Monitor?.Register("D100", 3);

        _simCts = new CancellationTokenSource();
        btnRandomSim.Content = "⏹ 停止模擬";
        btnRandomSim.Background = s_simActiveColor;
        ShowStatus("亂數模擬進行中 — D100 / D101 / D102 每 100~300ms 更新");

        var token = _simCts.Token;
        await Task.Run(() => RunRandomSimAsync(manager, token), token).ContinueWith(_ =>
        {
            // 模擬迴圈結束後（可能因停止按鈕或斷線）復原按鈕狀態
            Dispatcher.BeginInvoke(() =>
            {
                if (_simCts == null) return; // 已由停止按鈕清除
                _simCts = null;
                btnRandomSim.Content = "🎲 亂數 D100~D102";
                btnRandomSim.Background = s_simIdleColor;
                ShowStatus("亂數模擬已結束（連線中斷？）", error: true);
            });
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private static async Task RunRandomSimAsync(
        Stackdose.Abstractions.Hardware.IPlcManager manager,
        CancellationToken token)
    {
        var rng = new Random();
        while (!token.IsCancellationRequested && manager.IsConnected)
        {
            int index = rng.Next(0, 3);
            try
            {
                await manager.WriteAsync($"D10{index},{rng.Next(0, 32768)}");
                int delay = index == 0 ? 20 : rng.Next(100, 501);

                await Task.Delay(delay, token);
            }
            catch (TaskCanceledException) { break; }
            catch { break; }   // 連線中斷等意外，跳出
        }
    }

    // ── ValueChanged 事件監視 ─────────────────────────────────────────────

    private void OnWatchEventsToggled(object sender, RoutedEventArgs e)
    {
        if (btnWatchEvents.IsChecked == true)
        {
            lblEventLog.Visibility = Visibility.Visible;
            PlcEventContext.ControlValueChanged += OnControlValueChanged;
            lblEventLog.Text = "等待值變化…";
        }
        else
        {
            PlcEventContext.ControlValueChanged -= OnControlValueChanged;
            lblEventLog.Visibility = Visibility.Collapsed;
        }
    }

    private void OnControlValueChanged(object? sender, PlcValueChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
            lblEventLog.Text = $"⚡ {e.Address} = {e.DisplayText}");
    }

    // ── 狀態列 ────────────────────────────────────────────────────────

    private void ShowStatus(string msg, bool error = false)
    {
        lblStatus.Text = msg;
        lblStatus.Foreground = error
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightGreen;
    }
}
