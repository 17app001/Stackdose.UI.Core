using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Stackdose.Tools.ProjectGeneratorUI.Controls;

/// <summary>
/// 即時版型示意圖：根據 LayoutMode / 模組開關 / 比例設定，
/// 用色塊呈現 DynamicDevicePage 的大致版面配置。
/// 支援拖拉分隔線直接修改欄寬比例（TwoWay binding）。
/// </summary>
public partial class LayoutPreviewControl : UserControl
{
    private const double RefWindowWidth = 1280.0;

    /// <summary>拖拉分隔線時抑制 DP 觸發重建，避免循環</summary>
    private bool _suppressRebuild;
    /// <summary>SplitRight 模式下右側 Star 欄的 ColumnDefinitions 索引，否則 -1</summary>
    private int _rightStarColIdx = -1;

    #region DependencyProperties

    public static readonly DependencyProperty LayoutModeProperty =
        DependencyProperty.Register(nameof(LayoutMode), typeof(string), typeof(LayoutPreviewControl),
            new PropertyMetadata("SplitRight", Rebuild));
    public string LayoutMode
    {
        get => (string)GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    public static readonly DependencyProperty HasAlarmProperty =
        DependencyProperty.Register(nameof(HasAlarm), typeof(bool), typeof(LayoutPreviewControl),
            new PropertyMetadata(false, Rebuild));
    public bool HasAlarm
    {
        get => (bool)GetValue(HasAlarmProperty);
        set => SetValue(HasAlarmProperty, value);
    }

    public static readonly DependencyProperty HasSensorProperty =
        DependencyProperty.Register(nameof(HasSensor), typeof(bool), typeof(LayoutPreviewControl),
            new PropertyMetadata(false, Rebuild));
    public bool HasSensor
    {
        get => (bool)GetValue(HasSensorProperty);
        set => SetValue(HasSensorProperty, value);
    }

    public static readonly DependencyProperty HasLiveLogProperty =
        DependencyProperty.Register(nameof(HasLiveLog), typeof(bool), typeof(LayoutPreviewControl),
            new PropertyMetadata(false, Rebuild));
    public bool HasLiveLog
    {
        get => (bool)GetValue(HasLiveLogProperty);
        set => SetValue(HasLiveLogProperty, value);
    }

    public static readonly DependencyProperty RightColumnWidthStarProperty =
        DependencyProperty.Register(nameof(RightColumnWidthStar), typeof(double), typeof(LayoutPreviewControl),
            new FrameworkPropertyMetadata(0.85, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Rebuild));
    public double RightColumnWidthStar
    {
        get => (double)GetValue(RightColumnWidthStarProperty);
        set => SetValue(RightColumnWidthStarProperty, value);
    }

    public static readonly DependencyProperty LeftCommandWidthPxProperty =
        DependencyProperty.Register(nameof(LeftCommandWidthPx), typeof(int), typeof(LayoutPreviewControl),
            new FrameworkPropertyMetadata(250, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Rebuild));
    public int LeftCommandWidthPx
    {
        get => (int)GetValue(LeftCommandWidthPxProperty);
        set => SetValue(LeftCommandWidthPxProperty, value);
    }

    public static readonly DependencyProperty LiveDataTitleProperty =
        DependencyProperty.Register(nameof(LiveDataTitle), typeof(string), typeof(LayoutPreviewControl),
            new PropertyMetadata("Live Data", Rebuild));
    public string LiveDataTitle
    {
        get => (string)GetValue(LiveDataTitleProperty);
        set => SetValue(LiveDataTitleProperty, value);
    }

    #endregion

    private static void Rebuild(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LayoutPreviewControl)d;
        if (!ctrl._suppressRebuild)
            ctrl.RebuildPreview();
    }

    public LayoutPreviewControl()
    {
        InitializeComponent();
        Loaded += (_, _) => RebuildPreview();
    }

    // ── 比例計算 ─────────────────────────────────────────────────────
    private bool HasAnyViewer => HasAlarm || HasSensor;

    private double LeftStar   => LeftCommandWidthPx;
    private double RemainStar => RefWindowWidth - LeftCommandWidthPx;
    private double RightStar  => HasAnyViewer ? RemainStar * RightColumnWidthStar / (1.0 + RightColumnWidthStar) : 0;
    private double CenterStar => RemainStar - (LayoutMode == "SplitRight" && HasAnyViewer ? RightStar : 0);

    // ── 入口 ─────────────────────────────────────────────────────────
    private void RebuildPreview()
    {
        _rightStarColIdx = -1;

        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        HeaderLabel.Text = LayoutMode switch
        {
            "SplitBottom" => "SplitBottom — 下方 Alarm / Sensor",
            "Standard"    => "Standard — 無 Viewer 面板",
            _             => "SplitRight — 右側 Alarm / Sensor",
        };

        switch (LayoutMode)
        {
            case "SplitBottom": BuildSplitBottom(); break;
            case "Standard":    BuildStandard();    break;
            default:            BuildSplitRight();  break;
        }
    }

    // ── SplitRight ───────────────────────────────────────────────────
    private void BuildSplitRight()
    {
        // Cols: Left | gap | Center | (gap | Right)?
        AddCol(LeftStar,   star: true);   // 0
        AddCol(5);                         // 1 ← left splitter
        AddCol(CenterStar, star: true);   // 2
        if (HasAnyViewer)
        {
            AddCol(5);                     // 3 ← right splitter
            AddCol(RightStar, star: true); // 4
            _rightStarColIdx = 4;
        }

        // Rows: Main | (gap | Log)?
        int logRow = -1;
        AddRow(1, star: true);
        if (HasLiveLog) { AddRow(5); logRow = AddRow(24); }

        int totalRows = PreviewGrid.RowDefinitions.Count;

        // Splitters
        AddSplitter(col: 1, totalRows, isLeft: true);
        if (HasAnyViewer) AddSplitter(col: 3, totalRows, isLeft: false);

        // Left: Command Op / Actions
        Place(LeftCommandsBlock(), row: 0, col: 0);

        // Center: Live Data
        Place(Block(LiveDataTitle, PanelColor.Data), row: 0, col: 2);

        // Right: Alarm / Sensor
        if (HasAnyViewer)
            Place(RightViewersBlock(), row: 0, col: 4);

        // Log
        if (HasLiveLog)
        {
            int span = HasAnyViewer ? 5 : 3;
            var log = Block("System Log", PanelColor.Log);
            SetPos(log, logRow, 0, colSpan: span);
            PreviewGrid.Children.Add(log);
        }
    }

    // ── SplitBottom ──────────────────────────────────────────────────
    private void BuildSplitBottom()
    {
        AddCol(LeftStar,    star: true); // 0
        AddCol(5);                        // 1 ← left splitter
        AddCol(RemainStar,  star: true); // 2

        int viewerRow = -1, logRow = -1;
        AddRow(1.5, star: true);
        if (HasAnyViewer) { AddRow(5); viewerRow = AddRow(1, star: true); }
        if (HasLiveLog)   { AddRow(5); logRow    = AddRow(24); }

        int totalRows = PreviewGrid.RowDefinitions.Count;
        AddSplitter(col: 1, totalRows, isLeft: true);

        Place(LeftCommandsBlock(), row: 0, col: 0);
        Place(Block(LiveDataTitle, PanelColor.Data), row: 0, col: 2);

        if (HasAnyViewer)
        {
            if (HasAlarm && HasSensor)
            {
                var vg = new Grid();
                vg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                vg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
                vg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var a = Block("Alarm\nViewer", PanelColor.Alarm);  Grid.SetColumn(a, 0); vg.Children.Add(a);
                var s = Block("Sensor\nViewer", PanelColor.Sensor); Grid.SetColumn(s, 2); vg.Children.Add(s);
                SetPos(vg, viewerRow, 0, colSpan: 3);
                PreviewGrid.Children.Add(vg);
            }
            else
            {
                var single = Block(HasAlarm ? "Alarm Viewer" : "Sensor Viewer",
                                   HasAlarm ? PanelColor.Alarm : PanelColor.Sensor);
                SetPos(single, viewerRow, 0, colSpan: 3);
                PreviewGrid.Children.Add(single);
            }
        }

        if (HasLiveLog)
        {
            var log = Block("System Log", PanelColor.Log);
            SetPos(log, logRow, 0, colSpan: 3);
            PreviewGrid.Children.Add(log);
        }
    }

    // ── Standard ────────────────────────────────────────────────────
    private void BuildStandard()
    {
        AddCol(LeftStar,   star: true); // 0
        AddCol(5);                       // 1 ← left splitter
        AddCol(RemainStar, star: true); // 2

        int logRow = -1;
        AddRow(1, star: true);
        if (HasLiveLog) { AddRow(5); logRow = AddRow(24); }

        int totalRows = PreviewGrid.RowDefinitions.Count;
        AddSplitter(col: 1, totalRows, isLeft: true);

        Place(LeftCommandsBlock(), row: 0, col: 0);
        Place(Block(LiveDataTitle, PanelColor.Data), row: 0, col: 2);

        if (HasLiveLog)
        {
            var log = Block("System Log", PanelColor.Log);
            SetPos(log, logRow, 0, colSpan: 3);
            PreviewGrid.Children.Add(log);
        }
    }

    // ── GridSplitter ─────────────────────────────────────────────────
    private void AddSplitter(int col, int rowSpan, bool isLeft)
    {
        var s = new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
            Background          = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0xB0, 0xFF)),
            ResizeDirection     = GridResizeDirection.Columns,
            ResizeBehavior      = GridResizeBehavior.PreviousAndNext,
            Width               = 5,
            Cursor              = Cursors.SizeWE,
            ToolTip             = isLeft ? "拖拉調整指令欄寬" : "拖拉調整 Viewer 欄寬",
        };
        s.DragCompleted += isLeft ? OnLeftSplitterDragCompleted : OnRightSplitterDragCompleted;
        SetPos(s, 0, col, rowSpan: rowSpan);
        PreviewGrid.Children.Add(s);
    }

    private void OnLeftSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (PreviewGrid.ActualWidth <= 0) return;
        var leftActual = PreviewGrid.ColumnDefinitions[0].ActualWidth;
        var newPx = (int)Math.Round(leftActual / PreviewGrid.ActualWidth * RefWindowWidth);
        newPx = Math.Clamp(newPx, 100, 600);
        _suppressRebuild = true;
        LeftCommandWidthPx = newPx;
        _suppressRebuild = false;
        RebuildPreview();
    }

    private void OnRightSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (_rightStarColIdx < 0 || PreviewGrid.ColumnDefinitions.Count <= _rightStarColIdx) return;
        var centerActual = PreviewGrid.ColumnDefinitions[2].ActualWidth;
        var rightActual  = PreviewGrid.ColumnDefinitions[_rightStarColIdx].ActualWidth;
        if (centerActual <= 0) return;
        var star = Math.Round(rightActual / centerActual, 2);
        star = Math.Clamp(star, 0.30, 2.00);
        _suppressRebuild = true;
        RightColumnWidthStar = star;
        _suppressRebuild = false;
        RebuildPreview();
    }

    // ── 子元件 ──────────────────────────────────────────────────────
    private Grid LeftCommandsBlock()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.4, GridUnitType.Star) });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var op = Block("Command\nOperation", PanelColor.CmdOp);  Grid.SetRow(op, 0); g.Children.Add(op);
        var ac = Block("Command\nActions",   PanelColor.CmdAct); Grid.SetRow(ac, 2); g.Children.Add(ac);
        return g;
    }

    private Grid RightViewersBlock()
    {
        var g = new Grid();
        if (HasAlarm && HasSensor)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var a = Block("Alarm\nViewer",  PanelColor.Alarm);  Grid.SetRow(a, 0); g.Children.Add(a);
            var s = Block("Sensor\nViewer", PanelColor.Sensor); Grid.SetRow(s, 2); g.Children.Add(s);
        }
        else if (HasAlarm)
            g.Children.Add(Block("Alarm\nViewer", PanelColor.Alarm));
        else
            g.Children.Add(Block("Sensor\nViewer", PanelColor.Sensor));
        return g;
    }

    // ── Grid 工具 ────────────────────────────────────────────────────
    private int AddRow(double size, bool star = false)
    {
        int idx = PreviewGrid.RowDefinitions.Count;
        PreviewGrid.RowDefinitions.Add(new RowDefinition
        {
            Height = star ? new GridLength(size, GridUnitType.Star) : new GridLength(size)
        });
        return idx;
    }

    private void AddCol(double size, bool star = false)
    {
        PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = star ? new GridLength(size, GridUnitType.Star) : new GridLength(size)
        });
    }

    private void Place(UIElement el, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        SetPos(el, row, col, rowSpan, colSpan);
        PreviewGrid.Children.Add(el);
    }

    private static void SetPos(UIElement el, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        Grid.SetRow(el, row);
        Grid.SetColumn(el, col);
        if (rowSpan > 1) Grid.SetRowSpan(el, rowSpan);
        if (colSpan > 1) Grid.SetColumnSpan(el, colSpan);
    }

    // ── 色塊工廠 ────────────────────────────────────────────────────
    private static Border Block(string label, string hex)
    {
        return new Border
        {
            Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
            CornerRadius = new CornerRadius(4),
            Margin       = new Thickness(2),
            Child        = new TextBlock
            {
                Text                = label,
                Foreground          = new SolidColorBrush(Colors.White),
                FontSize            = 9,
                FontWeight          = FontWeights.SemiBold,
                TextAlignment       = TextAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping        = TextWrapping.Wrap,
                Opacity             = 0.88,
            }
        };
    }

    // ── Panel 色盤 ───────────────────────────────────────────────────
    private static class PanelColor
    {
        public const string CmdOp  = "#1A237E";
        public const string CmdAct = "#0D47A1";
        public const string Data   = "#004D40";
        public const string Alarm  = "#6A1B9A";
        public const string Sensor = "#1B5E20";
        public const string Log    = "#263238";
    }
}
