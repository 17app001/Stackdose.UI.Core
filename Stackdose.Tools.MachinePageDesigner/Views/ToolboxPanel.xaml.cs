using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class ToolboxPanel : UserControl
{
    public ToolboxPanel()
    {
        InitializeComponent();
    }

    private void ToolboxItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement fe || fe.Tag is not ToolboxItemDescriptor desc) return;

        var data = new DataObject("ToolboxItem", desc);
        DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
    }

    private void OnTabChanged(object sender, RoutedEventArgs e)
    {
        if (controlsPanel == null || templatePanel == null) return;

        if (tabControls.IsChecked == true)
        {
            controlsPanel.Visibility = Visibility.Visible;
            templatePanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            controlsPanel.Visibility = Visibility.Collapsed;
            templatePanel.Visibility = Visibility.Visible;
            templatePanel.RefreshTemplates();
        }
    }

    /// <summary>供外部呼叫：重新整理模板列表</summary>
    public void RefreshTemplateList() => templatePanel?.RefreshTemplates();
}
