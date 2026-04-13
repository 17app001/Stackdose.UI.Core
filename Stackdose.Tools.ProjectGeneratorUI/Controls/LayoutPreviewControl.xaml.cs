using Stackdose.App.DeviceFramework.Controls;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Stackdose.Tools.ProjectGeneratorUI.Controls;

/// <summary>
/// ?單???蝷箸????寞? LayoutMode / 璅∠??? / 瘥?閮剖?嚗?/// ? DynamicDevicePage ?之?渡??ａ?蝵柴?/// LiveData ??DeviceStatus ?憛蝙?函?撖?PlcDataGridPanel ?辣嚗?/// ?湔蝬??Ｙ??其葉雿輻?身摰? LabelRow 鞈???/// ?舀????蝺?乩耨?寞?撖祆?靘?TwoWay binding嚗?/// </summary>
public partial class LayoutPreviewControl : UserControl
{
    private const double RefWindowWidth = 1280.0;

    /// <summary>????蝺?? DP 閫貊?遣嚗?儐??/summary>
    private bool _suppressRebuild;
    /// <summary>SplitRight 璅∪?銝??Star 甈? ColumnDefinitions 蝝Ｗ?嚗??-1</summary>
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

    public static readonly DependencyProperty HasDeviceStatusProperty =
        DependencyProperty.Register(nameof(HasDeviceStatus), typeof(bool), typeof(LayoutPreviewControl),
            new PropertyMetadata(false, Rebuild));
    public bool HasDeviceStatus
    {
        get => (bool)GetValue(HasDeviceStatusProperty);
        set => SetValue(HasDeviceStatusProperty, value);
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

    public static readonly DependencyProperty DeviceStatusTitleProperty =
        DependencyProperty.Register(nameof(DeviceStatusTitle), typeof(string), typeof(LayoutPreviewControl),
            new PropertyMetadata("Device Status", Rebuild));
    public string DeviceStatusTitle
    {
        get => (string)GetValue(DeviceStatusTitleProperty);
        set => SetValue(DeviceStatusTitleProperty, value);
    }

    public static readonly DependencyProperty LabelsSourceProperty =
        DependencyProperty.Register(nameof(LabelsSource), typeof(IEnumerable), typeof(LayoutPreviewControl),
            new PropertyMetadata(null, Rebuild));
    public IEnumerable? LabelsSource
    {
        get => (IEnumerable?)GetValue(LabelsSourceProperty);
        set => SetValue(LabelsSourceProperty, value);
    }

    public static readonly DependencyProperty StatusLabelsSourceProperty =
        DependencyProperty.Register(nameof(StatusLabelsSource), typeof(IEnumerable), typeof(LayoutPreviewControl),
            new PropertyMetadata(null, Rebuild));
    public IEnumerable? StatusLabelsSource
    {
        get => (IEnumerable?)GetValue(StatusLabelsSourceProperty);
        set => SetValue(StatusLabelsSourceProperty, value);
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

    // ?? 瘥?閮? ?????????????????????????????????????????????????????
    private bool HasAnyViewer => HasAlarm || HasSensor;
    private bool ShowBottomRow => HasLiveLog || HasDeviceStatus;

    private double LeftStar   => LeftCommandWidthPx;
    private double RemainStar => RefWindowWidth - LeftCommandWidthPx;
    private double RightStar  => HasAnyViewer ? RemainStar * RightColumnWidthStar / (1.0 + RightColumnWidthStar) : 0;
    private double CenterStar => RemainStar - (LayoutMode == "SplitRight" && HasAnyViewer ? RightStar : 0);

    // ?? ?亙 ?????????????????????????????????????????????????????????
    private void RebuildPreview()
    {
        _rightStarColIdx = -1;

        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        HeaderLabel.Text = LayoutMode switch
        {
            "SplitBottom" => "SplitBottom ??銝 Alarm / Sensor",
            "Standard"    => "Standard ????Viewer ?Ｘ",
            _             => "SplitRight ???喳 Alarm / Sensor",
        };

        switch (LayoutMode)
        {
            case "SplitBottom": BuildSplitBottom(); break;
            case "Standard":    BuildStandard();    break;
            default:            BuildSplitRight();  break;
        }
    }

    // ?? SplitRight ???????????????????????????????????????????????????
    private void BuildSplitRight()
    {
        // Cols: Left | gap | Center | (gap | Right)?
        AddCol(LeftStar,   star: true);   // 0
        AddCol(5);                         // 1 ??left splitter
        AddCol(CenterStar, star: true);   // 2
        if (HasAnyViewer)
        {
            AddCol(5);                     // 3 ??right splitter
            AddCol(RightStar, star: true); // 4
            _rightStarColIdx = 4;
        }

        // Rows: Main | (gap | BottomRow)?
        int bottomRow = -1;
        AddRow(1, star: true);
        if (ShowBottomRow) { AddRow(5); bottomRow = AddRow(160); }

        int totalRows = PreviewGrid.RowDefinitions.Count;

        // Splitters
        AddSplitter(col: 1, totalRows, isLeft: true);
        if (HasAnyViewer) AddSplitter(col: 3, totalRows, isLeft: false);

        // Left: Command Op / Actions
        Place(LeftCommandsBlock(), row: 0, col: 0);

        // Center: Live Data panel
        Place(LiveDataPanelControl(), row: 0, col: 2);

        // Right: Alarm / Sensor
        if (HasAnyViewer)
            Place(RightViewersBlock(), row: 0, col: 4);

        // Bottom row: LiveLog + DeviceStatus
        if (ShowBottomRow)
        {
            int span = HasAnyViewer ? 5 : 3;
            AppendBottomRow(bottomRow, startCol: 0, colSpan: span);
        }
    }

    // ?? SplitBottom ??????????????????????????????????????????????????
    private void BuildSplitBottom()
    {
        AddCol(LeftStar,    star: true); // 0
        AddCol(5);                        // 1 ??left splitter
        AddCol(RemainStar,  star: true); // 2

        int viewerRow = -1, bottomRow = -1;
        AddRow(1.5, star: true);
        if (HasAnyViewer) { AddRow(5); viewerRow = AddRow(1, star: true); }
        if (ShowBottomRow) { AddRow(5); bottomRow = AddRow(160); }

        int totalRows = PreviewGrid.RowDefinitions.Count;
        AddSplitter(col: 1, totalRows, isLeft: true);

        Place(LeftCommandsBlock(), row: 0, col: 0);
        Place(LiveDataPanelControl(), row: 0, col: 2);

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

        if (ShowBottomRow)
            AppendBottomRow(bottomRow, startCol: 0, colSpan: 3);
    }

    // ?? Standard ????????????????????????????????????????????????????
    private void BuildStandard()
    {
        AddCol(LeftStar,   star: true); // 0
        AddCol(5);                       // 1 ??left splitter
        AddCol(RemainStar, star: true); // 2

        int bottomRow = -1;
        AddRow(1, star: true);
        if (ShowBottomRow) { AddRow(5); bottomRow = AddRow(160); }

        int totalRows = PreviewGrid.RowDefinitions.Count;
        AddSplitter(col: 1, totalRows, isLeft: true);

        Place(LeftCommandsBlock(), row: 0, col: 0);
        Place(LiveDataPanelControl(), row: 0, col: 2);

        if (ShowBottomRow)
            AppendBottomRow(bottomRow, startCol: 0, colSpan: 3);
    }

    // ?? Bottom row嚗iveLog + DeviceStatus嚗??????????????????????????
    private void AppendBottomRow(int row, int startCol, int colSpan)
    {
        if (HasLiveLog && HasDeviceStatus)
        {
            // 撌?1.5 : ??1 ?
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var log    = Block("System Log", PanelColor.Log);
            var status = DeviceStatusPanelControl();
            Grid.SetColumn(log, 0);    g.Children.Add(log);
            Grid.SetColumn(status, 2); g.Children.Add(status);
            SetPos(g, row, startCol, colSpan: colSpan);
            PreviewGrid.Children.Add(g);
        }
        else if (HasLiveLog)
        {
            var log = Block("System Log", PanelColor.Log);
            SetPos(log, row, startCol, colSpan: colSpan);
            PreviewGrid.Children.Add(log);
        }
        else
        {
            var status = DeviceStatusPanelControl();
            SetPos(status, row, startCol, colSpan: colSpan);
            PreviewGrid.Children.Add(status);
        }
    }

    // ?? GridSplitter ?????????????????????????????????????????????????
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
            ToolTip             = isLeft ? "??隤踵?誘甈祝" : "??隤踵 Viewer 甈祝",
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

    // ?? 摮?隞???????????????????????????????????????????????????????
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

    private PlcDataGridPanel LiveDataPanelControl() => new()
    {
        Title          = LiveDataTitle,
        ItemsSource    = LabelsSource,
        LabelFontSize  = 11,
        ValueFontSize  = 18,
    };

    private PlcDataGridPanel DeviceStatusPanelControl() => new()
    {
        Title          = DeviceStatusTitle,
        ItemsSource    = StatusLabelsSource,
        LabelFontSize  = 10,
        ValueFontSize  = 16,
    };

    // ?? Grid 撌亙 ????????????????????????????????????????????????????
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

    // ?? ?脣?撌亙?嚗??冽 Command / Viewer / Log ?憛? ??????????????
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

    // ?? Panel ?脩 ???????????????????????????????????????????????????
    private static class PanelColor
    {
        public const string CmdOp  = "#1A237E";
        public const string CmdAct = "#0D47A1";
        public const string Alarm  = "#6A1B9A";
        public const string Sensor = "#1B5E20";
        public const string Log    = "#263238";
    }
}
