using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using System.Windows.Media;

namespace Stackdose.Tools.DesignViewer;

public partial class MainWindow : Window
{
    private DesignDocument? _currentDocument;
    private int _currentPageIndex = 0;

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
            _currentDocument = doc;
            _currentPageIndex = 0;

            var fileName = Path.GetFileName(path);
            lblFilePath.Text = path;
            lblFilePath.Foreground = System.Windows.Media.Brushes.LightGray;
            Title = $"DesignViewer — {fileName}";

            BuildPageTabs(doc);
            SwitchPage(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Text = $"載入失敗：{ex.Message}";
            lblStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
    }

    private void BuildPageTabs(DesignDocument doc)
    {
        pageTabs.Children.Clear();

        var pages = doc.Pages;
        if (pages == null || pages.Count <= 1)
        {
            pageTabsBar.Visibility = Visibility.Collapsed;
            return;
        }

        pageTabsBar.Visibility = Visibility.Visible;

        for (int i = 0; i < pages.Count; i++)
        {
            var idx = i;
            var btn = new Button
            {
                Content         = pages[i].Name,
                Padding         = new Thickness(14, 5, 14, 5),
                Margin          = new Thickness(0, 0, 2, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                FontSize        = 12,
                BorderThickness = new Thickness(1),
                Foreground      = System.Windows.Media.Brushes.White,
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
            btn.Background  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x6E, 0xBF));
            btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6A, 0x8E, 0xDF));
            btn.FontWeight  = FontWeights.SemiBold;
        }
        else
        {
            btn.Background  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x48));
            btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x4A, 0x6A));
            btn.FontWeight  = FontWeights.Normal;
        }
    }

    private void RenderPage(DesignPage page)
    {
        designCanvas.Children.Clear();
        designCanvas.Width  = page.CanvasWidth;
        designCanvas.Height = page.CanvasHeight;
        canvasBorder.Width  = page.CanvasWidth;
        canvasBorder.Height = page.CanvasHeight;

        int ok = 0, err = 0;
        foreach (var def in page.CanvasItems)
        {
            UIElement control;
            try   { control = DesignTimeControlFactory.Create(def); ok++; }
            catch (Exception ex) { control = MakeErrorPlaceholder(def, ex.Message); err++; }

            if (control is FrameworkElement fe)
            {
                fe.Width  = def.Width;
                fe.Height = def.Height;
            }

            Canvas.SetLeft(control, def.X);
            Canvas.SetTop(control,  def.Y);
            designCanvas.Children.Add(control);
        }

        lblCanvasSize.Text = $"畫布：{page.CanvasWidth:F0} × {page.CanvasHeight:F0} px";
        lblStatus.Text = $"頁面：{page.Name}　共 {ok} 個元件" + (err > 0 ? $"（{err} 個錯誤）" : "");
        lblStatus.Foreground = err > 0
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightGreen;
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
