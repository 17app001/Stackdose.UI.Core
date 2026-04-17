using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.Tools.MachinePageDesigner.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.Tools.MachinePageDesigner;

/// <summary>
/// Runtime 控制項工廠基底類別。
/// DesignPlayer 與 DesignRuntime 的 RuntimeControlFactory 繼承此類，
/// 僅需覆寫 <see cref="ContextName"/> 和可選的 <see cref="CreateLiveLog"/>。
/// </summary>
public abstract class BaseRuntimeControlFactory
{
    private readonly ProcessCommandService _cmdSvc = new();

    // ── 共享 PLC 輪詢 Timer ──────────────────────────────────────────────
    // 所有 BitIndicator 共享一個 DispatcherTimer（500ms），避免 N 個控制項 = N 個 timer
    private static DispatcherTimer? _sharedTimer;
    private static readonly List<Action> _pollActions = new();
    private static readonly object _pollLock = new();
    private static bool _polling;

    private static void EnsureSharedTimer()
    {
        if (_sharedTimer != null) return;
        _sharedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _sharedTimer.Tick += (_, _) =>
        {
            if (_polling) return; // 重入保護
            _polling = true;
            try
            {
                Action[] snapshot;
                lock (_pollLock) { snapshot = _pollActions.ToArray(); }
                foreach (var action in snapshot)
                {
                    try { action(); } catch { /* 個別失敗不影響其他 */ }
                }
            }
            finally { _polling = false; }
        };
        _sharedTimer.Start();
    }

    private static void RegisterPollAction(Action action)
    {
        EnsureSharedTimer();
        lock (_pollLock) { _pollActions.Add(action); }
    }

    private static void UnregisterPollAction(Action action)
    {
        lock (_pollLock) { _pollActions.Remove(action); }
    }

    /// <summary>
    /// 用於日誌記錄的上下文名稱（如 "Player" / "Runtime"）。
    /// </summary>
    protected abstract string ContextName { get; }

    /// <summary>
    /// 根據 DesignerItemDefinition 建立有真實 PLC 連線的 Runtime 控制項。
    /// </summary>
    public UIElement Create(DesignerItemDefinition def)
    {
        return def.Type switch
        {
            "PlcLabel"           => CreatePlcLabel(def),
            "PlcText"            => CreatePlcText(def),
            "PlcStatusIndicator" => CreateBitIndicator(def),
            "SecuredButton"      => CreateSecuredButton(def),
            "Spacer"             => CreateGroupBox(def),
            "LiveLog"            => CreateLiveLog(),
            "AlarmViewer"        => CreateAlarmViewer(def),
            "SensorViewer"       => CreateSensorViewer(def),
            "StaticLabel"        => CreateStaticLabel(def),
            _                    => CreateUnknownPlaceholder(def.Type),
        };
    }

    // ── PlcLabel ──────────────────────────────────────────────────────────

    protected virtual UIElement CreatePlcLabel(DesignerItemDefinition def)
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

    // ── PlcText ───────────────────────────────────────────────────────────

    protected virtual UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        var plcText = new PlcText
        {
            Label   = p.GetString("label",   "Parameter"),
            Address = p.GetString("address", "D100"),
        };
        if (Enum.TryParse<PlcTextMode>(p.GetString("plcTextMode", "Word"), true, out var textMode))
            plcText.Mode = textMode;
        return plcText;
    }

    // ── Bit Indicator ─────────────────────────────────────────────────────

    protected virtual UIElement CreateBitIndicator(DesignerItemDefinition def)
    {
        var p       = def.Props;
        var address = p.GetString("displayAddress", "M100");
        var label   = p.GetString("label", null);
        var bgHex   = p.GetString("cardBackground", "");
        var fgHex   = p.GetString("labelForeground", "#9090B0");

        var ctrl = new PlcStatusIndicator
        {
            DisplayAddress = address,
        };

        if (!string.IsNullOrEmpty(label))
            ctrl.Label = label;

        if (!string.IsNullOrWhiteSpace(bgHex))
        {
            try { ctrl.CardBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)); }
            catch { /* ignore invalid hex */ }
        }

        try { ctrl.LabelForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)); }
        catch { /* ignore */ }

        return ctrl;
    }

    // ── SecuredButton ─────────────────────────────────────────────────────

    protected virtual UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p           = def.Props;
        var label       = p.GetString("label",          "Command");
        var address     = p.GetString("commandAddress",  p.GetString("address", ""));
        var writeValue  = p.GetString("writeValue",      "1");
        var commandType = p.GetString("commandType",     "write");
        var pulseMs     = (int)p.GetDouble("pulseMs",    300);

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

        var hasAction = !string.IsNullOrWhiteSpace(address) || commandType.Equals("sequence", StringComparison.OrdinalIgnoreCase);
        if (hasAction)
        {
            btn.Click += async (_, _) =>
            {
                switch (commandType.ToLowerInvariant())
                {
                    case "sequence":
                    {
                        var seqJson = p.GetString("sequenceDefinition", "");
                        if (string.IsNullOrWhiteSpace(seqJson))
                        { MessageBox.Show("未定義序列步驟", "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning); break; }
                        var seqDef = SequenceStepSerializer.Deserialize(seqJson);
                        if (seqDef == null)
                        { MessageBox.Show("序列定義解析失敗", "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning); break; }
                        btn.IsEnabled = false;
                        try
                        {
                            var seqResult = await _cmdSvc.ExecuteSequenceAsync(ContextName, label, label, seqDef);
                            if (!seqResult.Success) MessageBox.Show(seqResult.Message, "序列執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        finally { btn.IsEnabled = true; }
                        break;
                    }
                    case "pulse":
                    {
                        var r1 = await _cmdSvc.ExecuteCommandAsync(ContextName, label, label, address, writeValue);
                        if (!r1.Success) { MessageBox.Show(r1.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning); break; }
                        await Task.Delay(Math.Max(50, pulseMs));
                        var r2 = await _cmdSvc.ExecuteCommandAsync(ContextName, label, $"{label}(reset)", address, "0");
                        if (!r2.Success) MessageBox.Show(r2.Message, "歸零失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                    case "toggle":
                    {
                        var mgr = PlcContext.GlobalStatus?.CurrentManager;
                        if (mgr == null || !mgr.IsConnected)
                        { MessageBox.Show("PLC 未連線", "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning); break; }
                        int? current = address.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                            ? (mgr.ReadBit(address) == true ? 1 : 0)
                            : mgr.ReadWord(address);
                        var newVal = (current is > 0) ? "0" : writeValue;
                        var result = await _cmdSvc.ExecuteCommandAsync(ContextName, label, label, address, newVal);
                        if (!result.Success) MessageBox.Show(result.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                    default: // "write"
                    {
                        var result = await _cmdSvc.ExecuteCommandAsync(ContextName, label, label, address, writeValue);
                        if (!result.Success) MessageBox.Show(result.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }
                }
            };
        }

        return btn;
    }

    // ── GroupBox（Spacer）──────────────────────────────────────────────────

    protected virtual UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var (headerBgBrush, headerFgBrush) = DesignTimeControlFactory.GroupBoxThemeBrushes(
            def.Props.GetString("groupHeaderTheme", "Primary"));
        var root  = new Grid();

        root.Children.Add(new Border
        {
            BorderBrush      = new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)),
            BorderThickness  = new Thickness(1.5),
            Background       = new SolidColorBrush(Color.FromArgb(0x18, 0x6C, 0x8E, 0xEF)),
            CornerRadius     = new CornerRadius(4),
            IsHitTestVisible = false,
        });

        var header = new Border
        {
            Background   = headerBgBrush,
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Padding      = new Thickness(10, 4, 10, 4),
        };
        header.Child = new TextBlock
        {
            Text       = string.IsNullOrWhiteSpace(title) ? "Group" : title,
            Foreground = headerFgBrush,
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

    protected virtual UIElement CreateLiveLog() => new LiveLogViewer();

    // ── AlarmViewer ───────────────────────────────────────────────────────

    protected virtual UIElement CreateAlarmViewer(DesignerItemDefinition def)
    {
        var viewer = new AlarmViewer();
        var configFile = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(configFile))
            viewer.ConfigFile = configFile;
        return viewer;
    }

    // ── SensorViewer ──────────────────────────────────────────────────────

    protected virtual UIElement CreateSensorViewer(DesignerItemDefinition def)
    {
        var viewer = new SensorViewer();
        var configFile = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(configFile))
            viewer.ConfigFile = configFile;
        return viewer;
    }

    // ── StaticLabel ───────────────────────────────────────────────────────

    protected virtual UIElement CreateStaticLabel(DesignerItemDefinition def)
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
        };
    }

    // ── 未知類型佔位符 ────────────────────────────────────────────────────

    protected virtual UIElement CreateUnknownPlaceholder(string type)
    {
        return new Border
        {
            BorderBrush      = Brushes.OrangeRed,
            BorderThickness  = new Thickness(1),
            Background       = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
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
