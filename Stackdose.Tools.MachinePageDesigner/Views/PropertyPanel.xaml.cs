using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
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
    public DataTemplate? StaticLabelTemplate { get; set; }
    public DataTemplate? SpacerTemplate { get; set; }
    public DataTemplate? AlarmViewerTemplate { get; set; }
    public DataTemplate? SensorViewerTemplate { get; set; }
    public DataTemplate? ProcessStatusIndicatorTemplate { get; set; }
    public DataTemplate? PlcDeviceEditorTemplate { get; set; }
    public DataTemplate? EmptyTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not DesignerItemViewModel vm) return EmptyTemplate;

        return vm.ItemType switch
        {
            "PlcLabel" => PlcLabelTemplate,
            "PlcText" => PlcTextTemplate,
            "PlcStatusIndicator" => PlcStatusIndicatorTemplate,
            "StaticLabel" => StaticLabelTemplate,
            "SecuredButton" => SecuredButtonTemplate,
            "Spacer" => SpacerTemplate,
            "AlarmViewer" => AlarmViewerTemplate,
            "SensorViewer" => SensorViewerTemplate,
            "ProcessStatusIndicator" => ProcessStatusIndicatorTemplate,
            "PlcDeviceEditor" => PlcDeviceEditorTemplate,
            _ => EmptyTemplate,
        };
    }

}
