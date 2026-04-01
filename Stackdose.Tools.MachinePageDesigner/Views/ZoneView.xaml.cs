using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class ZoneView : UserControl
{
    public ZoneView()
    {
        InitializeComponent();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ToolboxItem") || e.Data.GetDataPresent("DesignerItem"))
        {
            e.Effects = e.Data.GetDataPresent("DesignerItem") ? DragDropEffects.Move : DragDropEffects.Copy;
            DropTarget.Background = new SolidColorBrush(Color.FromArgb(0x22, 0x6C, 0x8E, 0xEF));
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        DropTarget.Background = Brushes.Transparent;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        DropTarget.Background = Brushes.Transparent;

        if (DataContext is not ZoneViewModel zone) return;
        var mainVm = GetMainViewModel();

        // Zone 內拖拉換序
        if (e.Data.GetDataPresent("DesignerItem") && e.Data.GetData("DesignerItem") is DesignerItemViewModel draggedItem)
        {
            var fromIndex = zone.Items.IndexOf(draggedItem);
            var toIndex = GetDropIndex(e);

            if (fromIndex >= 0 && toIndex >= 0 && fromIndex != toIndex)
            {
                if (mainVm != null)
                    mainVm.ExecuteMoveItem(zone, fromIndex, toIndex);
                else
                    zone.MoveItem(fromIndex, toIndex);
            }
            e.Handled = true;
            return;
        }

        // 從工具箱拖入
        if (e.Data.GetDataPresent("ToolboxItem") && e.Data.GetData("ToolboxItem") is ToolboxItemDescriptor desc)
        {
            var def = desc.CreateDefinition();
            var itemVm = new DesignerItemViewModel(def);
            var insertIndex = GetDropIndex(e);

            if (mainVm != null)
                mainVm.ExecuteAddItem(zone, itemVm, insertIndex);
            else
                zone.AddItem(itemVm, insertIndex);

            e.Handled = true;
        }
    }

    private int GetDropIndex(DragEventArgs e)
    {
        if (DataContext is not ZoneViewModel zone) return -1;
        // 預設放到末尾
        return zone.Items.Count;
    }

    private MainViewModel? GetMainViewModel()
    {
        var canvas = this.FindAncestor<DesignCanvas>();
        return canvas?.Tag as MainViewModel;
    }
}

/// <summary>
/// 數值為 0 時顯示，否則隱藏
/// </summary>
public class ZeroToVisibleConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count == 0)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }
}

/// <summary>
/// Bool 反轉 Converter
/// </summary>
public class InverseBoolConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

/// <summary>
/// Bool 轉 Visibility
/// </summary>
public class BoolToVisibilityConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
}

/// <summary>
/// String equality 轉 Bool (用於 RadioButton SelectedValue 綁定)
/// </summary>
public class StringEqualityConverter : MarkupExtensionConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? parameter?.ToString() ?? "" : Binding.DoNothing;
}

/// <summary>
/// MarkupExtension + IValueConverter 二合一基類
/// </summary>
public abstract class MarkupExtensionConverter : System.Windows.Markup.MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
    public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Visual tree 搜尋擴充方法
/// </summary>
public static class VisualTreeExtensions
{
    public static T? FindAncestor<T>(this DependencyObject obj) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(obj);
        while (current != null)
        {
            if (current is T found) return found;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}