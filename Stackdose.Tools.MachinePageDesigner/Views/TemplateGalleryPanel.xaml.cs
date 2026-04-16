using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class TemplateGalleryPanel : UserControl
{
    private List<TemplateDescriptor> _allTemplates = [];

    public TemplateGalleryPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshTemplates();
    }

    public void RefreshTemplates()
    {
        _allTemplates = TemplateLibraryService.GetAllTemplates();
        ApplyFilter();
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
        => ApplyFilter();

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void ApplyFilter()
    {
        var category = (categoryFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        var search = searchBox.Text?.Trim() ?? "";

        IEnumerable<TemplateDescriptor> filtered = _allTemplates;

        if (category != "All")
            filtered = filtered.Where(t => t.Category == category);

        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(t =>
                t.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase));

        templateList.ItemsSource = filtered.ToList();
    }

    private void TemplateCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement fe || fe.Tag is not TemplateDescriptor desc) return;

        var data = new DataObject("Template", desc);
        DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
    }
}
