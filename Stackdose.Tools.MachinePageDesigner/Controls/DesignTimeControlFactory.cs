using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Models;

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
            "PlcLabel"           => CreatePlcLabel(def),
            "PlcText"            => CreatePlcText(def),
            "PlcStatusIndicator" => CreatePlcStatusIndicator(def),
            "SecuredButton"      => CreateSecuredButton(def),
            "Spacer"             => CreateGroupBox(def),
            "LiveLog"            => CreateViewerPlaceholder("System Log",    "\u2637", Color.FromRgb(0x1A, 0x1A, 0x30), null),
            "AlarmViewer"        => CreateViewerPlaceholder("Alarm Viewer",  "\u26A0", Color.FromRgb(0x30, 0x18, 0x18),
                                        def.Props.GetString("configFile"),
                                        def.Props.GetObjectList("alarmItems").Count),
            "SensorViewer"       => CreateViewerPlaceholder("Sensor Viewer", "\u26A1", Color.FromRgb(0x18, 0x28, 0x30),
                                        def.Props.GetString("configFile"),
                                        def.Props.GetObjectList("sensorItems").Count),
            "StaticLabel"        => CreateStaticLabel(def),
            _ => new TextBlock
            {
                Text = $"未知類型: {def.Type}",
                Foreground = Brushes.OrangeRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
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
            Divisor = p.GetDouble("divisor", 1),
            StringFormat = p.GetString("stringFormat", "F0"),
            ShowAddress = false,
            IsHitTestVisible = false,  // 設計時不可互動
        };

        // FrameShape
        var shapeStr = p.GetString("frameShape", "Rectangle");
        if (Enum.TryParse<PlcLabelFrameShape>(shapeStr, true, out var shape))
            label.FrameShape = shape;

        // ValueColorTheme → ValueForeground
        var colorStr = p.GetString("valueColorTheme", "NeonBlue");
        if (Enum.TryParse<PlcLabelColorTheme>(colorStr, true, out var colorTheme))
            label.ValueForeground = colorTheme;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("frameBackground", "DarkBlue"), true, out var bg))
            label.FrameBackground = bg;

        if (p.GetDouble("labelFontSize", 0) is > 0 and var lfs)
            label.LabelFontSize = lfs;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("labelForeground", "Default"), true, out var labelFg))
            label.LabelForeground = labelFg;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("labelAlignment", "Left"), true, out var labelAlign))
            label.LabelAlignment = labelAlign;

        if (Enum.TryParse<HorizontalAlignment>(p.GetString("valueAlignment", "Right"), true, out var valueAlign))
            label.ValueAlignment = valueAlign;

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
        };
        if (Enum.TryParse<PlcTextMode>(p.GetString("plcTextMode", "Word"), true, out var textMode))
            plcText.Mode = textMode;
        return plcText;
    }

    private static UIElement CreatePlcStatusIndicator(DesignerItemDefinition def)
    {
        var p     = def.Props;
        var addr  = p.GetString("displayAddress", "M100");
        var label = p.GetString("label", addr);

        var border = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(4),
            Padding         = new Thickness(8),
            MinHeight       = 40,
        };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width  = 12, Height = 12,
            Fill   = Brushes.Gray,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text      = $"{label}  [{addr}]",
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)),
            VerticalAlignment = VerticalAlignment.Center
        });
        border.Child = stack;
        return border;
    }

    private static UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p = def.Props;
        var btn = new Button
        {
            Content = p.GetString("label", "Command"),
            IsHitTestVisible = false,
            MinWidth = 80,
            Height = 36,
            FontWeight = FontWeights.SemiBold,
        };

        var theme = p.GetString("theme", "Primary").ToLowerInvariant();
        btn.Background = theme switch
        {
            "danger" or "red" => new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
            "success" or "green" => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94)),
            "warning" or "orange" => new SolidColorBrush(Color.FromRgb(0xFF, 0xB7, 0x4D)),
            _ => new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)), // Primary
        };
        btn.Foreground = Brushes.White;
        btn.BorderThickness = new Thickness(0);

        return btn;
    }

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var (headerBgBrush, headerFgBrush) = GroupBoxThemeBrushes(def.Props.GetString("groupHeaderTheme", "Primary"));

        // 根容器（無背景 → 不攔截 hit-test，讓 Body 區點擊穿透到 Canvas）
        var root = new Grid();

        // ── 第一層：純視覺（邊框 + 半透明底）IsHitTestVisible=false ──────
        root.Children.Add(new Border
        {
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)),
            BorderThickness  = new Thickness(1.5),
            Background      = new SolidColorBrush(Color.FromArgb(0x18, 0x6C, 0x8E, 0xEF)),
            CornerRadius    = new CornerRadius(4),
            IsHitTestVisible = false,   // 只是外觀，不攔截滑鼠
        });

        // ── 第二層：互動（只有 Header 有背景 → 只有 Header 可 hit-test）─
        var headerBorder = new Border
        {
            Background   = headerBgBrush,
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Padding      = new Thickness(10, 4, 10, 4),
            // Background 非 null → 該區域 hit-testable，點 Header 可選取/拖曳 GroupBox
        };
        headerBorder.Child = new TextBlock
        {
            Text       = string.IsNullOrWhiteSpace(title) ? "Group" : title,
            Foreground = headerFgBrush,
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

    private static UIElement CreateStaticLabel(DesignerItemDefinition def)
    {
        var p = def.Props;
        var text      = p.GetString("staticText",       "Text");
        var fontSize  = p.GetDouble("staticFontSize",   16);
        var weightStr = p.GetString("staticFontWeight", "Normal");
        var alignStr  = p.GetString("staticTextAlign",  "Left");
        var fgStr     = p.GetString("staticForeground", "#E2E2F0");

        var weight = weightStr.Equals("Bold", StringComparison.OrdinalIgnoreCase)
            ? FontWeights.Bold : FontWeights.Normal;

        var align = alignStr switch
        {
            "Center" => TextAlignment.Center,
            "Right"  => TextAlignment.Right,
            _        => TextAlignment.Left,
        };

        Brush fg;
        try { fg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgStr)); }
        catch { fg = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)); }

        return new TextBlock
        {
            Text              = text,
            FontSize          = fontSize,
            FontWeight        = weight,
            TextAlignment     = align,
            Foreground        = fg,
            TextWrapping      = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible  = false,
        };
    }

    private static UIElement CreateViewerPlaceholder(string title, string icon, Color bgColor, string? configFile, int embeddedCount = 0)
    {
        var border = new Border
        {
            Background      = new SolidColorBrush(bgColor),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(6),
        };
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Margin              = new Thickness(12),
        };
        stack.Children.Add(new TextBlock
        {
            Text      = icon,
            FontSize  = 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xDD)),
            Margin    = new Thickness(0, 0, 0, 6),
            VerticalAlignment = VerticalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text       = title,
            FontSize   = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)),
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (!string.IsNullOrWhiteSpace(configFile))
        {
            stack.Children.Add(new TextBlock
            {
                Text       = System.IO.Path.GetFileName(configFile),
                FontSize   = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                Margin     = new Thickness(0, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        else if (embeddedCount > 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text       = $"({embeddedCount} 筆內嵌定義)",
                FontSize   = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA)),
                Margin     = new Thickness(0, 4, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        border.Child = stack;
        return border;
    }

    internal static (Brush bg, Brush fg) GroupBoxThemeBrushes(string theme) =>
        theme.ToLowerInvariant() switch
        {
            "info"    => (new SolidColorBrush(Color.FromArgb(0xCC, 0x0D, 0x6E, 0xAA)), Brushes.White),
            "success" => (new SolidColorBrush(Color.FromArgb(0xCC, 0x1A, 0x6B, 0x3A)), Brushes.White),
            "warning" => (new SolidColorBrush(Color.FromArgb(0xCC, 0x7A, 0x4E, 0x00)), Brushes.White),
            "error"   => (new SolidColorBrush(Color.FromArgb(0xCC, 0x7A, 0x18, 0x18)), Brushes.White),
            "dark"    => (new SolidColorBrush(Color.FromArgb(0xCC, 0x12, 0x12, 0x20)), Brushes.White),
            _         => (new SolidColorBrush(Color.FromArgb(0xCC, 0x3A, 0x56, 0xA8)), Brushes.White), // Primary
        };
}
