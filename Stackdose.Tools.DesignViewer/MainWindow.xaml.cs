using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;

namespace Stackdose.Tools.DesignViewer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ── 開啟按鈕 ──────────────────────────────────────────────────────────

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "選擇 MachineDesign JSON 檔案",
            Filter = "Machine Design (*.machinedesign.json)|*.machinedesign.json|JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;
        LoadFile(dlg.FileName);
    }

    // ── 拖曳放入 ──────────────────────────────────────────────────────────

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
        if (json != null)
            LoadFile(json);
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
            RenderDocument(doc);

            var fileName = Path.GetFileName(path);
            lblFilePath.Text = path;
            lblFilePath.Foreground = System.Windows.Media.Brushes.LightGray;
            lblCanvasSize.Text = $"畫布：{doc.CanvasWidth:F0} × {doc.CanvasHeight:F0} px";
            lblStatus.Text = $"已載入：{fileName}　共 {doc.CanvasItems.Count} 個元件";
            lblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            Title = $"DesignViewer — {fileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Text = $"載入失敗：{ex.Message}";
            lblStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
    }

    private void RenderDocument(DesignDocument doc)
    {
        // 清除舊有元件
        designCanvas.Children.Clear();

        // 設定畫布尺寸
        designCanvas.Width  = doc.CanvasWidth;
        designCanvas.Height = doc.CanvasHeight;
        canvasBorder.Width  = doc.CanvasWidth;
        canvasBorder.Height = doc.CanvasHeight;

        // 依 Z-order（canvasItems 順序：index 0 = 最底層）逐一建立控制項
        foreach (var def in doc.CanvasItems)
        {
            UIElement control;
            try
            {
                control = DesignTimeControlFactory.Create(def);
            }
            catch (Exception ex)
            {
                // 若某元件建立失敗，以錯誤占位符替代，不中斷其他元件
                control = MakeErrorPlaceholder(def, ex.Message);
            }

            // 設定尺寸
            if (control is FrameworkElement fe)
            {
                fe.Width  = def.Width;
                fe.Height = def.Height;
            }

            // 定位
            Canvas.SetLeft(control, def.X);
            Canvas.SetTop(control,  def.Y);

            designCanvas.Children.Add(control);
        }
    }

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string message)
    {
        var border = new Border
        {
            Width  = def.Width,
            Height = def.Height,
            BorderBrush     = System.Windows.Media.Brushes.OrangeRed,
            BorderThickness = new Thickness(1),
            Background      = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
        };
        border.Child = new TextBlock
        {
            Text       = $"[{def.Type}] {message}",
            Foreground = System.Windows.Media.Brushes.OrangeRed,
            FontSize   = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin     = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
        };
        return border;
    }
}
