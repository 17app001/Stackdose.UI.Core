using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Controls;

namespace Stackdose.Tools.MachinePageDesigner.Controls;

/// <summary>
/// 根據 DesignerItemDefinition 建立對應的真實 WPF 控制項實例（設計時版本）。
/// 設計時 PlcLabel 不連線 PLC，顯示 DefaultValue。
/// </summary>
public static class DesignTimeControlFactory
{
    public static UIElement Create(DesignerItemDefinition def)
    {
        return def.Type switch
        {
            "PlcLabel"               => CreatePlcLabel(def),
            "PlcText"                => CreatePlcText(def),
            "StaticLabel"            => CreateStaticLabel(def),
            "PlcStatusIndicator"     => CreatePlcStatusIndicator(def),
            "SecuredButton"          => CreateSecuredButton(def),
            "Spacer"                 => CreateGroupBox(def),
            "ProcessStatusIndicator" => CreateProcessStatusIndicator(def),
            "SystemClock"            => new SystemClock { IsHitTestVisible = false },
            "PlcDeviceEditor"        => CreatePlcDeviceEditor(def),
            "PrintHeadStatus"        => CreatePrintHeadStatus(def),
            "PrintHeadController"    => new PrintHeadController { IsHitTestVisible = false },
            "TabPanel"               => CreateTabPanelPlaceholder(def),
            "LiveLog"                => CreateViewerPlaceholder("System Log",    "\u2637", Color.FromRgb(0x1A, 0x1A, 0x30)),
            "AlarmViewer"            => CreateViewerPlaceholder("Alarm Viewer",  "\u26A0", Color.FromRgb(0x30, 0x18, 0x18)),
            "SensorViewer"           => CreateViewerPlaceholder("Sensor Viewer", "\u26A1", Color.FromRgb(0x18, 0x28, 0x30)),
            _ => new TextBlock
            {
                Text = $"未知類型: {def.Type}",
                Foreground = Brushes.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static UIElement CreateProcessStatusIndicator(DesignerItemDefinition def)
    {
        var p = def.Props;
        var indicator = new ProcessStatusIndicator
        {
            BatchNumber = p.GetString("label", ""),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        if (Enum.TryParse<ProcessState>(p.GetString("processState", "Running"), true, out var state))
            indicator.ProcessState = state;

        return indicator;
    }

    private static UIElement CreatePlcDeviceEditor(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcDeviceEditor
        {
            Address = p.GetString("address", "D100"),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    private static UIElement CreatePrintHeadStatus(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PrintHeadStatus
        {
            ConfigFilePath = p.GetString("configFile", "Config/feiyang_head1.json"),
            HeadName = p.GetString("headName", "PrintHead 1"),
            HeadIndex = (int)p.GetDouble("headIndex", 0),
            AutoConnect = p.GetBool("autoConnect", false),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    private static UIElement CreatePlcLabel(DesignerItemDefinition def)
    {
        var p = def.Props;

        var label = new PlcLabel
        {
            Label = p.GetString("label", "Label"),
            Address = p.GetString("address", "D100"),
            DefaultValue = p.GetString("defaultValue", "0"),
            ValueFontSize = p.GetDouble("valueFontSize", 20),
            LabelFontSize = p.GetDouble("labelFontSize", 12),
            Divisor = p.GetDouble("divisor", 1),
            StringFormat = p.GetString("stringFormat", "F0"),
            ShowAddress = false,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        if (Enum.TryParse<PlcLabelFrameShape>(p.GetString("frameShape", "Rectangle"), true, out var shape))
            label.FrameShape = shape;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("valueColorTheme", "NeonBlue"), true, out var valueFg))
            label.ValueForeground = valueFg;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("labelForeground", "Default"), true, out var labelFg))
            label.LabelForeground = labelFg;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("frameBackground", "DarkBlue"), true, out var frameBg))
            label.FrameBackground = frameBg;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("labelAlignment", "Left"), true, out var labelAlign))
            label.LabelAlignment = labelAlign;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("valueAlignment", "Right"), true, out var valueAlign))
            label.ValueAlignment = valueAlign;

        if (Enum.TryParse<PlcDataType>(p.GetString("dataType", "Word"), true, out var dataType))
            label.DataType = dataType;

        return label;
    }

    private static UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        var plcText = new PlcText
        {
            Label = p.GetString("label", "Parameter"),
            Address = p.GetString("address", "D100"),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        return plcText;
    }

    private static UIElement CreateStaticLabel(DesignerItemDefinition def)
    {
        var p = def.Props;
        var text = p.GetString("staticText", "Label");
        var fontSize = p.GetDouble("staticFontSize", 16);
        var fontWeightStr = p.GetString("staticFontWeight", "Normal");
        var textAlignStr = p.GetString("staticTextAlign", "Left");
        var foregroundStr = p.GetString("staticForeground", "#E2E2F0");

        var fontWeight = fontWeightStr.Equals("Bold", StringComparison.OrdinalIgnoreCase)
            ? FontWeights.Bold : FontWeights.Normal;

        var textAlign = textAlignStr.ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right"  => TextAlignment.Right,
            _        => TextAlignment.Left,
        };

        Brush foreground;
        try { foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foregroundStr)); }
        catch { foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)); }

        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            TextAlignment = textAlign,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
    }

    private static UIElement CreatePlcStatusIndicator(DesignerItemDefinition def)
    {
        // 用簡化的設計時預覽代替真實控件
        var p = def.Props;
        var addr = p.GetString("displayAddress", "M100");

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        var stack = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stack.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = Brushes.Gray,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"StatusIndicator [{addr}]",
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)),
            VerticalAlignment = VerticalAlignment.Center
        });
        border.Child = stack;
        return border;
    }

    private static UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p = def.Props;
        var label = p.GetString("label", "Command");
        var theme = p.GetString("theme", "Primary").ToLowerInvariant();

        // 顏色與 SecuredButton.UpdateThemeColors() 的 ButtonTheme enum 完全一致
        var bgColor = theme switch
        {
            "primary"          => Color.FromRgb(0x21, 0x96, 0xF3), // #2196F3
            "success"          => Color.FromRgb(0x4C, 0xAF, 0x50), // #4CAF50
            "warning"          => Color.FromRgb(0xFF, 0x98, 0x00), // #FF9800
            "error" or "danger"=> Color.FromRgb(0xF4, 0x43, 0x36), // #F44336
            "info"             => Color.FromRgb(0x00, 0xBC, 0xD4), // #00BCD4
            _                  => Color.FromRgb(0x60, 0x7D, 0x8B), // #607D8B Normal
        };

        var border = new Border
        {
            Background = new SolidColorBrush(bgColor),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };

        border.Child = new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };

        return border;
    }

    private static Color GroupBoxHeaderColor(string colorName) => colorName.ToLowerInvariant() switch
    {
        "normal"   => Color.FromArgb(0xCC, 0x37, 0x47, 0x4F),
        "success"  => Color.FromArgb(0xCC, 0x2E, 0x7D, 0x32),
        "warning"  => Color.FromArgb(0xCC, 0xE6, 0x51, 0x00),
        "error"    => Color.FromArgb(0xCC, 0xC6, 0x28, 0x28),
        "info"     => Color.FromArgb(0xCC, 0x00, 0x83, 0x8F),
        _          => Color.FromArgb(0xCC, 0x15, 0x65, 0xC0), // primary (default)
    };

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title       = def.Props.GetString("title", "Group");
        var headerColor = GroupBoxHeaderColor(def.Props.GetString("headerColor", "Primary"));

        // 根容器（無背景 → 不攔截 hit-test，讓 Body 區點擊穿透到 Canvas）
        var root = new Grid();

        // ── 第一層：純視覺（邊框 + 半透明底）IsHitTestVisible=false ──────
        root.Children.Add(new Border
        {
            BorderBrush     = new SolidColorBrush(Color.FromRgb(headerColor.R, headerColor.G, headerColor.B)),
            BorderThickness  = new Thickness(1.5),
            Background      = new SolidColorBrush(Color.FromArgb(0x18, headerColor.R, headerColor.G, headerColor.B)),
            CornerRadius    = new CornerRadius(4),
            IsHitTestVisible = false,   // 只是外觀，不攔截滑鼠
        });

        // ── 第二層：互動（只有 Header 有背景 → 只有 Header 可 hit-test）─
        var headerBorder = new Border
        {
            Background   = new SolidColorBrush(headerColor),
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Padding      = new Thickness(10, 4, 10, 4),
            // Background 非 null → 該區域 hit-testable，點 Header 可選取/拖曳 GroupBox
        };
        headerBorder.Child = new TextBlock
        {
            Text       = string.IsNullOrWhiteSpace(title) ? "Group" : title,
            Foreground = Brushes.White,
            FontSize   = 12,
            FontWeight = FontWeights.SemiBold,
        };

        var dock = new DockPanel { LastChildFill = true, Background = null };
        DockPanel.SetDock(headerBorder, Dock.Top);
        dock.Children.Add(headerBorder);
        // Body：Background=null → 不 hit-testable → 點擊穿透到 designCanvas → 框選可啟動
        dock.Children.Add(new Border { Background = null });

        root.Children.Add(dock);
        return root;
    }

    private static UIElement CreateTabPanelPlaceholder(DesignerItemDefinition def)
    {
        // Extract (title, itemCount) pairs from props
        var tabs = new List<(string Title, int Count)>();
        if (def.Props.TryGetValue("tabs", out var raw))
        {
            try
            {
                var je = raw is System.Text.Json.JsonElement j
                    ? j
                    : System.Text.Json.JsonSerializer.SerializeToElement(raw);
                foreach (var tab in je.EnumerateArray())
                {
                    var title = tab.TryGetProperty("title", out var t) ? t.GetString() ?? "Tab" : "Tab";
                    var count = tab.TryGetProperty("items", out var items) && items.ValueKind == System.Text.Json.JsonValueKind.Array
                        ? items.GetArrayLength() : 0;
                    tabs.Add((title, count));
                }
            }
            catch { }
        }
        if (tabs.Count == 0) { tabs.Add(("Tab 1", 0)); tabs.Add(("Tab 2", 0)); }

        var root = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x2A)) };

        // Header strip
        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x14, 0x20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        DockPanel.SetDock(header, Dock.Top);

        var tabStrip = new StackPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < tabs.Count; i++)
        {
            var isFirst = i == 0;
            var label = tabs[i].Count > 0 ? $"{tabs[i].Title}  ({tabs[i].Count})" : tabs[i].Title;
            tabStrip.Children.Add(new Border
            {
                Background = isFirst
                    ? new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x2A))
                    : Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(16, 6, 16, 6),
                Child = new TextBlock
                {
                    Text = label,
                    Foreground = isFirst
                        ? new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0))
                        : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                    FontWeight = isFirst ? FontWeights.SemiBold : FontWeights.Normal,
                    FontSize = 13,
                }
            });
        }
        header.Child = tabStrip;
        root.Children.Add(header);

        // Content area — "double-click to edit" hint
        var hint = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        hint.Children.Add(new TextBlock
        {
            Text = "✎",
            FontSize = 22,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x66, 0x99)),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        hint.Children.Add(new TextBlock
        {
            Text = "雙擊開啟 Tab 編輯器",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0xAA)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        });

        var content = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x38, 0x55)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(8),
            Child = hint,
        };
        root.Children.Add(content);

        return root;
    }

    private static UIElement CreateViewerPlaceholder(string title, string icon, Color bgColor)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(bgColor),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
        };
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12),
        };
        stack.Children.Add(new TextBlock
        {
            Text = icon,
            FontSize = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xDD)),
            Margin = new Thickness(0, 0, 0, 6),
            VerticalAlignment = VerticalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)),
            VerticalAlignment = VerticalAlignment.Center,
        });
        border.Child = stack;
        return border;
    }
}
