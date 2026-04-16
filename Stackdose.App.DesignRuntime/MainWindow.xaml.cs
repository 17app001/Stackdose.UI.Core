using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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
    private FileSystemWatcher? _fileWatcher;
    private string? _loadedFilePath;
    private DateTime _lastHotReload = DateTime.MinValue;
    private DesignDocument? _currentDocument;
    private int _currentPageIndex = 0;

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
                var isReconnect = !btnConnect.IsEnabled; // connect 已按過，表示重連
                ShowStatus(isReconnect
                    ? $"↺ PLC 重新連線成功：{ip}:{port}"
                    : $"PLC 已連線：{ip}:{port}");
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                // 重連後確保所有畫布 PLC 地址重新納入掃描清單
                _plcStatus?.RefreshMonitors();
            });
        };

        _plcStatus.ConnectionLost += () =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                ShowStatus("⚠ PLC 斷線，正在重新連線…", error: true);
                // 斷線期間停用 Disconnect 按鈕（避免操作衝突）
                btnDisconnect.IsEnabled = false;
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
            _loadedFilePath = path;
            SetupFileWatcher(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            ShowStatus($"載入失敗：{ex.Message}", error: true);
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
            NotifyFilter        = NotifyFilters.LastWrite | NotifyFilters.Size,
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

        // 延遲讀取：等待寫入完成
        Task.Delay(400).ContinueWith(_ =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var doc = DesignFileService.Load(e.FullPath);
                    RenderDocument(doc, e.FullPath);
                    lblFilePath.Text       = $"↺ {e.FullPath}";
                    lblFilePath.Foreground = System.Windows.Media.Brushes.Cyan;
                    ShowStatus($"↺ 偵測到檔案變更，已自動重新載入：{Path.GetFileName(e.FullPath)}");
                }
                catch { /* 靜默失敗，等下次更新觸發 */ }
            });
        });
    }

    private void RenderDocument(DesignDocument doc, string filePath)
    {
        // Hot-reload 時保留當前頁索引（若頁數足夠），否則回到第一頁
        var restorePage = (_currentDocument != null)
            ? Math.Min(_currentPageIndex, (doc.Pages?.Count ?? 1) - 1)
            : 0;

        _currentDocument = doc;
        _currentPageIndex = 0;

        var fileName = Path.GetFileName(filePath);
        Title = $"DesignRuntime — {fileName}";

        // 更新 Tags 狀態
        UpdateTagsStatus(doc);

        // 建立頁籤列
        BuildPageTabs(doc, fileName);

        // 渲染目標頁（hot-reload 保留頁，首次載入從第一頁開始）
        SwitchPage(restorePage);
    }

    /// <summary>
    /// 掃描文件中所有 PLC 地址，與 Tags 清單比對後更新狀態列右側資訊。
    /// 未定義在 Tags 中的地址會以橘色警告標示，方便工程師在上線前發現遺漏。
    /// </summary>
    private void UpdateTagsStatus(DesignDocument doc)
    {
        var tagCount = doc.Tags.Count;
        if (tagCount == 0)
        {
            lblItemCount.Text = "";
            return;
        }

        var allItems = doc.Pages?.SelectMany(p => p.CanvasItems) ?? [];
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in allItems)
        {
            var a = item.Props.GetString("address");
            var d = item.Props.GetString("displayAddress");
            var c = item.Props.GetString("commandAddress");
            if (!string.IsNullOrWhiteSpace(a)) used.Add(a.Trim());
            if (!string.IsNullOrWhiteSpace(d)) used.Add(d.Trim());
            if (!string.IsNullOrWhiteSpace(c)) used.Add(c.Trim());
        }

        var tagSet = new HashSet<string>(doc.Tags.Select(t => t.Address), StringComparer.OrdinalIgnoreCase);
        var undefinedCount = used.Count(a => !tagSet.Contains(a));

        if (undefinedCount > 0)
        {
            lblItemCount.Text = $"📌 Tags {tagCount}  ⚠ {undefinedCount} 未對應";
            lblItemCount.Foreground = System.Windows.Media.Brushes.Orange;
            lblItemCount.ToolTip = $"有 {undefinedCount} 個地址未定義在 Tags 清單中，建議在設計器補充定義";
        }
        else
        {
            lblItemCount.Text = $"📌 Tags {tagCount}  ✓ 全部對應";
            lblItemCount.Foreground = System.Windows.Media.Brushes.LightGreen;
            lblItemCount.ToolTip = $"所有 PLC 地址均已定義在 Tags 清單中";
        }
    }

    private void BuildPageTabs(DesignDocument doc, string? fileName = null)
    {
        pageTabs.Children.Clear();

        var pages = doc.Pages;
        if (pages == null || pages.Count <= 1)
        {
            // 單頁：隱藏頁籤列
            pageTabsBar.Visibility = Visibility.Collapsed;
            return;
        }

        pageTabsBar.Visibility = Visibility.Visible;

        for (int i = 0; i < pages.Count; i++)
        {
            var idx = i; // closure capture
            var page = pages[i];

            var btn = new Button
            {
                Content = page.Name,
                Tag = idx,
                Padding = new Thickness(14, 5, 14, 5),
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
            };
            SetTabStyle(btn, active: i == 0);
            btn.Click += (_, _) => SwitchPage(idx);
            pageTabs.Children.Add(btn);
        }
    }

    private void SwitchPage(int index)
    {
        var pages = _currentDocument?.Pages;
        if (pages == null || index < 0 || index >= pages.Count) return;

        _currentPageIndex = index;

        // 更新頁籤樣式
        for (int i = 0; i < pageTabs.Children.Count; i++)
        {
            if (pageTabs.Children[i] is Button btn)
                SetTabStyle(btn, active: i == index);
        }

        RenderPage(pages[index]);
    }

    private static void SetTabStyle(Button btn, bool active)
    {
        if (active)
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x6E, 0xBF));
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x6A, 0x8E, 0xDF));
            btn.FontWeight = FontWeights.SemiBold;
        }
        else
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x48));
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x6A));
            btn.FontWeight = FontWeights.Normal;
        }
    }

    private void RenderPage(DesignPage page)
    {
        // 清除舊有元件
        runtimeCanvas.Children.Clear();

        // 套用畫布尺寸
        runtimeCanvas.Width = page.CanvasWidth;
        runtimeCanvas.Height = page.CanvasHeight;
        canvasBorder.Width = page.CanvasWidth;
        canvasBorder.Height = page.CanvasHeight;

        int okCount = 0;
        int errorCount = 0;

        // 依 Z-order 建立控制項（canvasItems[0] = 最底層）
        foreach (var def in page.CanvasItems)
        {
            UIElement control;
            try
            {
                control = RuntimeControlFactory.Instance.Create(def);
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

        // 更新狀態列
        lblCanvasInfo.Text = $"畫布：{page.CanvasWidth:F0} × {page.CanvasHeight:F0} px";
        lblItemCount.Text = $"元件：{okCount} 個" + (errorCount > 0 ? $"（{errorCount} 個錯誤）" : "");

        var pageLabel = page.Name;
        var status = $"頁面：{pageLabel}  共 {page.CanvasItems.Count} 個元件";
        if (errorCount > 0) status += $"，{errorCount} 個建立失敗（橘框標示）";
        ShowStatus(status, error: errorCount > 0);

        // 等所有 PlcLabel 的 Loaded 事件執行完後，刷新 Monitor 讓新地址加入掃描清單
        Dispatcher.BeginInvoke(
            () => _plcStatus?.RefreshMonitors(),
            System.Windows.Threading.DispatcherPriority.Loaded);
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

    // ── 視窗關閉 ──────────────────────────────────────────────────────────

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _simCts?.Cancel();
        _fileWatcher?.Dispose();
        _fileWatcher = null;
        PlcContext.GlobalStatus = null;
        _plcStatus?.Dispose();
        ComplianceContext.Shutdown();
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
