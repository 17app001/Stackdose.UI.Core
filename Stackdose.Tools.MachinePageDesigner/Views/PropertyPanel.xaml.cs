using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
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
    private void OnAddAlarmItem(object sender, RoutedEventArgs e)
    {
        if (GetViewModel(sender) is { } vm)
            vm.AlarmItems.Add(new AlarmEditItem());
    }

    private void OnRemoveAlarmItem(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AlarmEditItem item && GetViewModel(sender) is { } vm)
            vm.AlarmItems.Remove(item);
    }

    // ── SensorViewer inline editor handlers ────────────────────────
    private void OnAddSensorItem(object sender, RoutedEventArgs e)
    {
        if (GetViewModel(sender) is { } vm)
            vm.SensorItems.Add(new SensorEditItem());
    }

    private void OnRemoveSensorItem(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SensorEditItem item && GetViewModel(sender) is { } vm)
            vm.SensorItems.Remove(item);
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
