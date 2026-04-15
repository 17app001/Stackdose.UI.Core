using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.App.DesignPlayer;

/// <summary>
/// 根據 DesignerItemDefinition 建立有真實 PLC 連線的 Runtime 控制項。
/// 控制項放入 Canvas 後，會自動透過 PlcContext.GlobalStatus 訂閱 PLC 數據。
/// </summary>
public static class RuntimeControlFactory
{
    private static readonly ProcessCommandService _cmdSvc = new();

    public static UIElement Create(DesignerItemDefinition def)
    {
        return def.Type switch
        {
            "PlcLabel"           => CreatePlcLabel(def),
            "PlcText"            => CreatePlcText(def),
            "PlcStatusIndicator" => CreateBitIndicator(def),
            "SecuredButton"      => CreateSecuredButton(def),
            "Spacer"             => CreateGroupBox(def),
            "LiveLog"            => new LiveLogViewer(),
            "AlarmViewer"        => CreateAlarmViewer(def),
            "SensorViewer"       => CreateSensorViewer(def),
            _                    => MakeUnknownPlaceholder(def.Type),
        };
    }

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

        if (p.GetDouble("valueFontSize", 0) is > 0 and var fs)
            label.ValueFontSize = fs;

        if (Enum.TryParse<PlcLabelFrameShape>(p.GetString("frameShape", "Rectangle"), true, out var shape))
            label.FrameShape = shape;

        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("valueColorTheme", "NeonBlue"), true, out var theme))
            label.ValueForeground = theme;

        return label;
    }

    private static UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcText
        {
            Label   = p.GetString("label",   "Parameter"),
            Address = p.GetString("address", "D100"),
        };
    }

    private static UIElement CreateBitIndicator(DesignerItemDefinition def)
    {
        var p       = def.Props;
        var address = p.GetString("displayAddress", "M100");
        var label   = p.GetString("label", address);

        var root = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(4),
            Padding         = new Thickness(8),
        };

        var dot   = new Ellipse { Width = 12, Height = 12, Fill = Brushes.Gray, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        var text  = new TextBlock { Text = $"{label}  [{address}]", Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)), VerticalAlignment = VerticalAlignment.Center };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(dot);
        stack.Children.Add(text);
        root.Child = stack;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            try
            {
                var mgr = PlcContext.GlobalStatus?.CurrentManager;
                if (mgr == null || !mgr.IsConnected) { dot.Fill = Brushes.Gray; return; }

                int? val = address.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                    ? (mgr.ReadBit(address) == true ? 1 : 0)
                    : mgr.ReadWord(address);

                dot.Fill = val is > 0
                    ? new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94))
                    : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88));
            }
            catch { dot.Fill = Brushes.OrangeRed; }
        };

        root.Loaded   += (_, _) => timer.Start();
        root.Unloaded += (_, _) => timer.Stop();

        return root;
    }

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
                var result = await _cmdSvc.ExecuteCommandAsync("Player", label, label, address);
                if (!result.Success)
                    MessageBox.Show(result.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        return btn;
    }

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var root  = new Grid();

        root.Children.Add(new Border
        {
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)),
            BorderThickness  = new Thickness(1.5),
            Background      = new SolidColorBrush(Color.FromArgb(0x18, 0x6C, 0x8E, 0xEF)),
            CornerRadius    = new CornerRadius(4),
            IsHitTestVisible = false,
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

    private static UIElement CreateAlarmViewer(DesignerItemDefinition def)
    {
        var viewer = new AlarmViewer();
        var configFile = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(configFile))
            viewer.ConfigFile = configFile;
        return viewer;
    }

    private static UIElement CreateSensorViewer(DesignerItemDefinition def)
    {
        var viewer = new SensorViewer();
        var configFile = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(configFile))
            viewer.ConfigFile = configFile;
        return viewer;
    }

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
