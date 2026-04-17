using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>
/// 比較字串是否等於 ConverterParameter，用於 RadioButton IsChecked 雙向綁定
/// </summary>
[ValueConversion(typeof(string), typeof(bool))]
public sealed class StringEqualityConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter?.ToString() : Binding.DoNothing;

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}

/// <summary>SensorEditItem.Mode 下拉選項</summary>
public static class SensorModes
{
    public static string[] All { get; } = ["AND", "OR", "COMPARE"];
}

public partial class PropertyPanel : UserControl
{
    // ── PLC Tags DependencyProperty ──────────────────────────────────────
    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(
            nameof(Tags),
            typeof(ObservableCollection<PlcTag>),
            typeof(PropertyPanel),
            new PropertyMetadata(null));

    /// <summary>
    /// 來自 MainViewModel 的 PLC 標籤清單，供 PropertyPanel 地址欄下拉選取。
    /// 由 MainWindow.xaml 的 PropertyPanel.Tags 繫結傳入。
    /// </summary>
    public ObservableCollection<PlcTag>? Tags
    {
        get => (ObservableCollection<PlcTag>?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public PropertyPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Enter 鍵立即提交 LostFocus 綁定；Escape 鍵放棄並還原顯示值
    /// </summary>
    private void OnNumericKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (e.Key == Key.Enter)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            e.Handled = true;
        }
    }

    // ── AlarmViewer inline editor handlers ─────────────────────────

    // ── 產生範本檔 handlers ────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private void OnGenerateAlarmTemplate(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel(sender);
        if (vm == null) return;

        var dlg = new SaveFileDialog
        {
            Title      = "儲存 Alarm 範本",
            FileName   = "alarms.json",
            DefaultExt = ".json",
            Filter     = "JSON 檔案|*.json",
        };
        if (dlg.ShowDialog() != true) return;

        var template = new
        {
            Alarms = new[]
            {
                new { Group = "Safety",      Device = "M100", Bit = 0, OperationDescription = "緊急停止觸發 (Emergency Stop)" },
                new { Group = "Safety",      Device = "M100", Bit = 1, OperationDescription = "安全門開啟 (Door Open)"        },
                new { Group = "Pressure",    Device = "M101", Bit = 0, OperationDescription = "CDA 壓力不足 (CDA Low)"        },
                new { Group = "Temperature", Device = "M102", Bit = 0, OperationDescription = "加熱器過熱 (Heater Overheat)"  },
            }
        };

        File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(template, _jsonOpts));
        vm.ConfigFile = dlg.FileName;
    }

    private void OnGenerateSensorTemplate(object sender, RoutedEventArgs e)
    {
        var vm = GetViewModel(sender);
        if (vm == null) return;

        var dlg = new SaveFileDialog
        {
            Title      = "儲存 Sensor 範本",
            FileName   = "sensors.json",
            DefaultExt = ".json",
            Filter     = "JSON 檔案|*.json",
        };
        if (dlg.ShowDialog() != true) return;

        var template = new object[]
        {
            new { Group = "Temperature", Device = "D90",  Bit = "",  Value = ">75",  Mode = "COMPARE", OperationDescription = "加熱器溫度過高 (Heater High)"  },
            new { Group = "Temperature", Device = "D91",  Bit = "",  Value = "<10",  Mode = "COMPARE", OperationDescription = "冷卻溫度過低 (Cooling Low)"    },
            new { Group = "Safety",      Device = "M100", Bit = "0", Value = "1",    Mode = "AND",     OperationDescription = "緊急停止啟動 (E-Stop Active)"  },
            new { Group = "Motion",      Device = "M101", Bit = "2", Value = "1",    Mode = "OR",      OperationDescription = "軸鎖定中 (Axis Interlock)"     },
        };

        File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(template, _jsonOpts));
        vm.ConfigFile = dlg.FileName;
    }

    private static DesignerItemViewModel? GetViewModel(object sender)
    {
        if (sender is FrameworkElement fe)
        {
            // Walk up to find the DesignerItemViewModel DataContext
            var dp = fe;
            while (dp != null)
            {
                if (dp.DataContext is DesignerItemViewModel vm) return vm;
                dp = dp.Parent as FrameworkElement;
            }
        }
        return null;
    }
}

/// <summary>
/// 根據選取項目類型切換屬性面板 DataTemplate
/// </summary>
public class PropertyPanelTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PlcLabelTemplate { get; set; }
    public DataTemplate? PlcTextTemplate { get; set; }
    public DataTemplate? PlcStatusIndicatorTemplate { get; set; }
    public DataTemplate? SecuredButtonTemplate { get; set; }
    public DataTemplate? SpacerTemplate { get; set; }
    public DataTemplate? LiveLogTemplate { get; set; }
    public DataTemplate? AlarmViewerTemplate { get; set; }
    public DataTemplate? SensorViewerTemplate { get; set; }
    public DataTemplate? StaticLabelTemplate { get; set; }
    public DataTemplate? EmptyTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not DesignerItemViewModel vm)
            return EmptyTemplate;

        return vm.ItemType switch
        {
            "PlcLabel"           => PlcLabelTemplate,
            "PlcText"            => PlcTextTemplate,
            "PlcStatusIndicator" => PlcStatusIndicatorTemplate,
            "SecuredButton"      => SecuredButtonTemplate,
            "Spacer"             => SpacerTemplate,
            "LiveLog"            => LiveLogTemplate,
            "AlarmViewer"        => AlarmViewerTemplate,
            "SensorViewer"       => SensorViewerTemplate,
            "StaticLabel"        => StaticLabelTemplate,
            _ => EmptyTemplate,
        };
    }
}
