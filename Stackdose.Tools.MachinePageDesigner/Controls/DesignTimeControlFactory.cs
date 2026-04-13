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
            "Spacer"             => CreateSpacer(),
            "LiveLog"            => CreateViewerPlaceholder("System Log",    "\u2637", Color.FromRgb(0x1A, 0x1A, 0x30)),
            "AlarmViewer"        => CreateViewerPlaceholder("Alarm Viewer",  "\u26A0", Color.FromRgb(0x30, 0x18, 0x18)),
            "SensorViewer"       => CreateViewerPlaceholder("Sensor Viewer", "\u26A1", Color.FromRgb(0x18, 0x28, 0x30)),
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
        return plcText;
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
        };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
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

    private static UIElement CreateSpacer()
    {
        return new Border
        {
            Background = Brushes.Transparent,
            MinHeight = 40,
        };
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
