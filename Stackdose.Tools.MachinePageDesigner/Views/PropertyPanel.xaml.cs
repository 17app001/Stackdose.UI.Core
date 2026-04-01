using System.Windows;
using System.Windows.Controls;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class PropertyPanel : UserControl
{
    public PropertyPanel()
    {
        InitializeComponent();
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
    public DataTemplate? EmptyTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not DesignerItemViewModel vm)
            return EmptyTemplate;

        return vm.ItemType switch
        {
            "PlcLabel" => PlcLabelTemplate,
            "PlcText" => PlcTextTemplate,
            "PlcStatusIndicator" => PlcStatusIndicatorTemplate,
            "SecuredButton" => SecuredButtonTemplate,
            "Spacer" => SpacerTemplate,
            _ => EmptyTemplate,
        };
    }
}
