using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Controls;

namespace Stackdose.App.DesignRuntime;

public partial class MainWindow : Window
{
    private PlcStatus? _plcStatus;

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
            IpAddress    = ip,
            Port         = port,
            AutoConnect  = true,
            IsGlobal     = true,
            ScanInterval = scan,
            ShowBorder   = true,
        };

        _plcStatus.ConnectionEstablished += mgr =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                ShowStatus($"PLC 已連線：{ip}:{port}");
                btnConnect.IsEnabled    = false;
                btnDisconnect.IsEnabled = true;
            });
        };

        plcStatusHost.Child = _plcStatus;
        ShowStatus($"正在連線至 {ip}:{port} …");
    }

    private void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        _plcStatus?.Dispose();
        plcStatusHost.Child = null;
        _plcStatus = null;

        btnConnect.IsEnabled    = true;
        btnDisconnect.IsEnabled = false;
        ShowStatus("已斷線");
    }

    // ── 開啟 JSON ─────────────────────────────────────────────────────────

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "選擇 MachineDesign JSON 檔案",
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
        runtimeCanvas.Width  = doc.CanvasWidth;
        runtimeCanvas.Height = doc.CanvasHeight;
        canvasBorder.Width   = doc.CanvasWidth;
        canvasBorder.Height  = doc.CanvasHeight;

        int okCount    = 0;
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
                control    = MakeErrorPlaceholder(def, ex.Message);
                errorCount++;
            }

            // 設定尺寸
            if (control is FrameworkElement fe)
            {
                fe.Width  = def.Width;
                fe.Height = def.Height;
            }

            Canvas.SetLeft(control, def.X);
            Canvas.SetTop(control,  def.Y);
            runtimeCanvas.Children.Add(control);
        }

        // 更新 UI
        var fileName = Path.GetFileName(filePath);
        Title = $"DesignRuntime — {fileName}";
        lblCanvasInfo.Text = $"畫布：{doc.CanvasWidth:F0} × {doc.CanvasHeight:F0} px";
        lblItemCount.Text  = $"元件：{okCount} 個" + (errorCount > 0 ? $"（{errorCount} 個錯誤）" : "");

        var status = $"已載入：{fileName}  共 {doc.CanvasItems.Count} 個元件";
        if (errorCount > 0) status += $"，{errorCount} 個建立失敗（橘框標示）";
        ShowStatus(status, error: errorCount > 0);
    }

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string message)
    {
        return new Border
        {
            Width           = def.Width,
            Height          = def.Height,
            BorderBrush     = System.Windows.Media.Brushes.OrangeRed,
            BorderThickness  = new Thickness(1),
            Background      = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
            Child = new TextBlock
            {
                Text         = $"[{def.Type}] {message}",
                Foreground   = System.Windows.Media.Brushes.OrangeRed,
                FontSize     = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
    }

    private void ShowStatus(string msg, bool error = false)
    {
        lblStatus.Text       = msg;
        lblStatus.Foreground = error
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightGreen;
    }
}
