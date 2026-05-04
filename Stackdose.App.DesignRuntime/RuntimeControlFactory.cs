using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.ShellShared.Behaviors;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Controls;

namespace Stackdose.App.DesignRuntime;

/// <summary>
/// 根據 DesignerItemDefinition 建立有真實 PLC 連線的 Runtime 控制項。
/// 控制項放入 Canvas 後，會自動透過 PlcContext.GlobalStatus 訂閱 PLC 數據。
/// </summary>
public static class RuntimeControlFactory
{
    private static readonly ProcessCommandService _cmdSvc = new();

    public static UIElement Create(DesignerItemDefinition def)
    {
        var control = def.Type switch
        {
            "PlcLabel"               => CreatePlcLabel(def),
            "PlcText"                => CreatePlcText(def),
            "PlcStatusIndicator"     => CreateBitIndicator(def),
            "SecuredButton"          => CreateSecuredButton(def),
            "Spacer"                 => CreateGroupBox(def),
            "LiveLog"                => CreateLiveLog(),
            "AlarmViewer"            => CreateAlarmViewer(def),
            "SensorViewer"           => CreateSensorViewer(def),
            "StaticLabel"            => CreateStaticLabel(def),
            "PrintHeadStatus"        => CreatePrintHeadStatus(def),
            "PrintHeadController"    => CreatePrintHeadController(def),
            "TabPanel"               => CreateTabPanel(def),
            "SystemClock"            => new SystemClock(),
            "ProcessStatusIndicator" => CreateProcessStatusIndicator(def),
            "PlcDeviceEditor"        => CreatePlcDeviceEditor(def),
            _ => MakeUnknownPlaceholder(def.Type),
        };

        // 附加 BehaviorTag — 讓 BehaviorEngine 能識別控制項並執行 SetProp
        AttachBehaviorTag(def, control);
        return control;
    }

    /// <summary>
    /// 將 ControlRuntimeTag 設定至控制項 Tag 屬性，並為 SecuredButton 設定 BehaviorId。
    /// </summary>
    private static void AttachBehaviorTag(DesignerItemDefinition def, UIElement control)
    {
        if (control is not FrameworkElement fe) return;

        var tag = new ControlRuntimeTag
        {
            Id          = def.Id,
            PropSetters = BuildPropSetters(fe),
        };
        fe.Tag = tag;

        if (fe is SecuredButton btn)
            btn.BehaviorId = def.Id;
    }

    /// <summary>
    /// 依控制項類型建立 prop 名稱 → setter 字典（閉包捕捉控制項實體）。
    /// </summary>
    private static Dictionary<string, Action<string>> BuildPropSetters(FrameworkElement fe)
    {
        var s = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);

        // 通用（Control 有 Background / Foreground）
        if (fe is System.Windows.Controls.Control ctrl)
        {
            s["background"] = v => { try { ctrl.Background = ParseBrush(v); } catch { } };
            s["foreground"] = v => { try { ctrl.Foreground = ParseBrush(v); } catch { } };
        }

        // 控制項特定
        switch (fe)
        {
            case PlcLabel lbl:
                s["label"] = v => lbl.Label = v;
                break;
            case SecuredButton btn:
                s["label"] = v => btn.Content = v;
                break;
            case TextBlock tb:
                s["text"]       = v => tb.Text = v;
                s["foreground"] = v => { try { tb.Foreground = ParseBrush(v); } catch { } };
                break;
        }

        return s;
    }

    private static SolidColorBrush ParseBrush(string colorStr)
        => new((Color)ColorConverter.ConvertFromString(colorStr));

    // ── PlcLabel ─────────────────────────────────────────────────────────

    private static UIElement CreatePlcLabel(DesignerItemDefinition def)
    {
        var p = def.Props;

        var label = new PlcLabel
        {
            Label        = p.GetString("label",        "Label"),
            Address      = p.GetString("address",      "D100"),
            DefaultValue = p.GetString("defaultValue", "0"),
            Divisor      = p.GetDouble("divisor",      1),
            StringFormat = p.GetString("stringFormat", "F0"),
            ShowAddress  = false,
        };

        if (p.GetDouble("valueFontSize", 0) is > 0 and var vfs)
            label.ValueFontSize = vfs;

        if (p.GetDouble("labelFontSize", 0) is > 0 and var lfs)
            label.LabelFontSize = lfs;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("valueAlignment", ""), true, out var vAlign))
            label.ValueAlignment = vAlign;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("labelAlignment", ""), true, out var lAlign))
            label.LabelAlignment = lAlign;

        if (Enum.TryParse<PlcLabelFrameShape>(p.GetString("frameShape", "Rectangle"), true, out var shape))
            label.FrameShape = shape;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("valueColorTheme", "NeonBlue"), true, out var vTheme))
            label.ValueForeground = vTheme;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("labelForeground", ""), true, out var lTheme))
            label.LabelForeground = lTheme;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("frameBackground", ""), true, out var bgTheme))
            label.FrameBackground = bgTheme;

        if (Enum.TryParse<PlcDataType>(p.GetString("dataType", "Word"), true, out var dataType))
            label.DataType = dataType;

        label.EnableLiveRecord = p.GetBool("enableLiveRecord", true);

        return label;
    }

    // ── PlcText ───────────────────────────────────────────────────────────

    private static UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcText
        {
            Label               = p.GetString("label",              "Parameter"),
            Address             = p.GetString("address",            "D100"),
            ShowSuccessMessage  = p.GetBool  ("showSuccessMessage", true),
            EnableAuditTrail    = p.GetBool  ("enableAuditTrail",   true),
        };
    }

    // ── Bit Indicator（對應設計器的 PlcStatusIndicator）─────────────────
    // 每 500ms 從 PlcContext.GlobalStatus 讀一次指定地址的 bit/word，
    // 亮綠 = 非零，暗灰 = 零或未連線

    private static UIElement CreateBitIndicator(DesignerItemDefinition def)
    {
        var p       = def.Props;
        var address = p.GetString("displayAddress", "M100");
        var label   = p.GetString("label",          address);

        var root  = new Border
        {
            Background    = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)),
            BorderBrush   = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius  = new CornerRadius(4),
            Padding       = new Thickness(8),
        };

        var dot   = new Ellipse { Width = 12, Height = 12, Fill = Brushes.Gray, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        var text  = new TextBlock { Text = $"{label}  [{address}]", Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)), VerticalAlignment = VerticalAlignment.Center };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(dot);
        stack.Children.Add(text);
        root.Child = stack;

        // 輪詢 PLC
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            try
            {
                var mgr = PlcContext.GlobalStatus?.CurrentManager;
                if (mgr == null || !mgr.IsConnected) { dot.Fill = Brushes.Gray; return; }

                // 優先嘗試讀 bit（M 開頭）；其他地址讀 Word
                int? val = address.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                    ? (mgr.ReadBit(address) == true ? 1 : 0)
                    : mgr.ReadWord(address);

                dot.Fill = val is > 0
                    ? new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94))   // 綠
                    : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88));   // 暗
            }
            catch { dot.Fill = Brushes.OrangeRed; }
        };

        // 控制項載入時啟動 timer，卸載時停止
        root.Loaded   += (_, _) => timer.Start();
        root.Unloaded += (_, _) => timer.Stop();

        return root;
    }

    // ── SecuredButton ─────────────────────────────────────────────────────

    private static UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p       = def.Props;
        var label   = p.GetString("label",   "Command");
        var address = p.GetString("address", "");

        var theme = p.GetString("theme", "Primary").ToLowerInvariant() switch
        {
            "danger"  or "red"    or "error"   => ButtonTheme.Error,
            "success" or "green"               => ButtonTheme.Success,
            "warning" or "orange"              => ButtonTheme.Warning,
            "info"    or "cyan"                => ButtonTheme.Info,
            "normal"  or "gray"                => ButtonTheme.Normal,
            _                                  => ButtonTheme.Primary,
        };

        var btn = new SecuredButton
        {
            Content       = label,
            Theme         = theme,
            OperationName = label,
            MinWidth      = 80,
        };

        if (!string.IsNullOrWhiteSpace(address))
        {
            btn.Click += async (_, _) =>
            {
                var result = await _cmdSvc.ExecuteCommandAsync("Runtime", label, label, address);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }

        return btn;
    }

    // ── GroupBox（Spacer）─────────────────────────────────────────────────

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var root  = new Grid();

        root.Children.Add(new Border
        {
            BorderBrush      = new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)),
            BorderThickness   = new Thickness(1.5),
            Background       = new SolidColorBrush(Color.FromArgb(0x18, 0x6C, 0x8E, 0xEF)),
            CornerRadius     = new CornerRadius(4),
            IsHitTestVisible  = false,
        });

        var header = new Border
        {
            Background   = new SolidColorBrush(Color.FromArgb(0xCC, 0x3A, 0x56, 0xA8)),
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Padding      = new Thickness(10, 4, 10, 4),
        };
        header.Child = new TextBlock
        {
            Text       = string.IsNullOrWhiteSpace(title) ? "Group" : title,
            Foreground = Brushes.White,
            FontSize   = 12,
            FontWeight = FontWeights.SemiBold,
        };

        var dock = new DockPanel { LastChildFill = true, Background = null };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(new Border { Background = null });
        root.Children.Add(dock);
        return root;
    }

    // ── LiveLog ───────────────────────────────────────────────────────────

    private static UIElement CreateLiveLog()
    {
        // LiveLogViewer 直接訂閱 ComplianceContext 日誌，無需額外設定
        return new LiveLogViewer();
    }

    // ── AlarmViewer ───────────────────────────────────────────────────────

    private static UIElement CreateAlarmViewer(DesignerItemDefinition def)
    {
        var p      = def.Props;
        var viewer = new AlarmViewer();
        var cf     = p.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(cf)) viewer.ConfigFile = cf;
        var title  = p.GetString("viewerTitle", "");
        if (!string.IsNullOrWhiteSpace(title)) viewer.Title = title;
        viewer.DefaultShowActiveOnly = p.GetBool("defaultShowActiveOnly", false);
        return viewer;
    }

    // ── SensorViewer ──────────────────────────────────────────────────────

    private static UIElement CreateSensorViewer(DesignerItemDefinition def)
    {
        var p      = def.Props;
        var viewer = new SensorViewer();
        var cf     = p.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(cf)) viewer.ConfigFile = cf;
        var title  = p.GetString("viewerTitle", "");
        if (!string.IsNullOrWhiteSpace(title)) viewer.Title = title;
        viewer.EnableGrouping        = p.GetBool("enableGrouping", false);
        viewer.DefaultShowActiveOnly = p.GetBool("defaultShowActiveOnly", false);
        return viewer;
    }

    // ── StaticLabel ───────────────────────────────────────────────────────

    private static UIElement CreateStaticLabel(DesignerItemDefinition def)
    {
        var p          = def.Props;
        var text       = p.GetString("staticText",       p.GetString("text", p.GetString("label", "")));
        var fontSize   = p.GetDouble("staticFontSize",   p.GetDouble("fontSize", 13));
        var fontWeight = p.GetString("staticFontWeight", "Normal");
        var textAlign  = p.GetString("staticTextAlign",  p.GetString("textAlign", "Left"));
        var color      = p.GetString("staticForeground", p.GetString("foreground", "#E2E2F0"));

        SolidColorBrush brush;
        try { brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
        catch { brush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)); }

        var weight = fontWeight.ToLowerInvariant() switch
        {
            "bold"      => FontWeights.Bold,
            "semibold"  => FontWeights.SemiBold,
            "light"     => FontWeights.Light,
            _           => FontWeights.Normal,
        };

        var align = textAlign.ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right"  => TextAlignment.Right,
            _        => TextAlignment.Left,
        };

        return new TextBlock
        {
            Text              = text,
            FontSize          = fontSize,
            FontWeight        = weight,
            TextAlignment     = align,
            Foreground        = brush,
            FontFamily        = new System.Windows.Media.FontFamily("Microsoft JhengHei"),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping      = TextWrapping.Wrap,
        };
    }

    // ── ProcessStatusIndicator ───────────────────────────────────────────

    private static UIElement CreateProcessStatusIndicator(DesignerItemDefinition def)
    {
        var p         = def.Props;
        var indicator = new ProcessStatusIndicator
        {
            BatchNumber         = p.GetString("label", ""),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        if (Enum.TryParse<ProcessState>(p.GetString("processState", "Running"), true, out var state))
            indicator.ProcessState = state;

        return indicator;
    }

    // ── PlcDeviceEditor ──────────────────────────────────────────────────

    private static UIElement CreatePlcDeviceEditor(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcDeviceEditor
        {
            Address             = p.GetString("address", "D100"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };
    }

    // ── PrintHeadController ──────────────────────────────────────────────

    private static UIElement CreatePrintHeadController(DesignerItemDefinition def)
    {
        var p = def.Props;
        var ctrl = new PrintHeadController();

        var plcReady = p.GetString("plcReadyDevice", "");
        if (!string.IsNullOrWhiteSpace(plcReady))
            ctrl.PlcReadyDevice = plcReady;

        var dirDevice = p.GetString("directionPlcDevice", "");
        if (!string.IsNullOrWhiteSpace(dirDevice))
            ctrl.DirectionPlcDevice = dirDevice;

        return ctrl;
    }

    // ── PrintHeadStatus ──────────────────────────────────────────────────

    private static UIElement CreatePrintHeadStatus(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PrintHeadStatus
        {
            ConfigFilePath = p.GetString("configFile", "Config/feiyang_head1.json"),
            HeadName       = p.GetString("headName",   "PrintHead 1"),
            HeadIndex      = (int)p.GetDouble("headIndex", 0),
            AutoConnect    = p.GetBool("autoConnect",  false),
        };
    }

    // ── TabPanel ──────────────────────────────────────────────────────────

    private static UIElement CreateTabPanel(DesignerItemDefinition def)
    {
        var panel = new TabPanel();

        if (!def.Props.TryGetValue("tabs", out var raw) || raw is not JsonElement je)
            return panel;

        TabEntry[]? tabs;
        try { tabs = JsonSerializer.Deserialize<TabEntry[]>(je.GetRawText()); }
        catch { return panel; }
        if (tabs == null) return panel;

        foreach (var tab in tabs)
        {
            var container = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
            };
            if (tab.Items != null)
            {
                foreach (var itemDef in tab.Items)
                    container.Children.Add(Create(itemDef));
            }
            panel.AddTab(tab.Title ?? "", container);
        }
        return panel;
    }

    private sealed class TabEntry
    {
        public string? Title { get; set; }
        public DesignerItemDefinition[]? Items { get; set; }
    }

    // ── 未知類型佔位符 ────────────────────────────────────────────────────

    private static UIElement MakeUnknownPlaceholder(string type)
    {
        return new Border
        {
            BorderBrush     = Brushes.OrangeRed,
            BorderThickness  = new Thickness(1),
            Background      = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
            Child = new TextBlock
            {
                Text         = $"未知類型：{type}",
                Foreground   = Brushes.OrangeRed,
                FontSize     = 11,
                Margin       = new Thickness(6),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
    }
}
