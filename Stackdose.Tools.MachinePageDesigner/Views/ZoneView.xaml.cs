using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
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
            UpdateInsertIndicator(e);
        }
        else
        {
            e.Effects = DragDropEffects.None;
            HideInsertIndicator();
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        DropTarget.Background = Brushes.Transparent;
        HideInsertIndicator();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        DropTarget.Background = Brushes.Transparent;
        HideInsertIndicator();

        if (DataContext is not ZoneViewModel zone) return;
        var mainVm = GetMainViewModel();

        // DesignerItem 拖移（同 Zone 換序 或 跨 Zone 移動）
        if (e.Data.GetDataPresent("DesignerItem") && e.Data.GetData("DesignerItem") is DesignerItemViewModel draggedItem)
        {
            var fromIndex = zone.Items.IndexOf(draggedItem);
            var toIndex   = GetDropIndex(e);

            if (fromIndex >= 0)
            {
                // 同 Zone 換序
                if (fromIndex != toIndex && toIndex >= 0)
                {
                    if (mainVm != null)
                        mainVm.ExecuteMoveItem(zone, fromIndex, toIndex);
                    else
                        zone.MoveItem(fromIndex, toIndex);
                }
            }
            else
            {
                // 跨 Zone 移動：找出 sourceZone
                var canvasVm = this.FindAncestor<DesignCanvas>()?.DataContext as DesignCanvasViewModel;
                var sourceZone = canvasVm?.FindZoneOf(draggedItem);
                if (sourceZone != null && !ReferenceEquals(sourceZone, zone))
                {
                    if (mainVm != null)
                        mainVm.ExecuteCrossZoneMove(sourceZone, zone, draggedItem, toIndex >= 0 ? toIndex : zone.Items.Count);
                    else
                    {
                        sourceZone.RemoveItem(draggedItem);
                        zone.AddItem(draggedItem, toIndex >= 0 ? toIndex : zone.Items.Count);
                    }
                }
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

        // 嘗試從 UniformGrid 計算滑鼠所在的 cell index
        var uniformGrid = DropTarget.FindDescendant<UniformGrid>();
        if (uniformGrid == null || zone.Items.Count == 0)
            return zone.Items.Count;

        var pos = e.GetPosition(uniformGrid);
        int cols = Math.Max(1, zone.Columns);
        int itemCount = uniformGrid.Children.Count;
        int rows = (int)Math.Ceiling((double)itemCount / cols);

        if (rows == 0 || uniformGrid.ActualWidth <= 0 || uniformGrid.ActualHeight <= 0)
            return zone.Items.Count;

        double cellWidth = uniformGrid.ActualWidth / cols;
        double cellHeight = uniformGrid.ActualHeight / rows;

        int col = Math.Clamp((int)(pos.X / cellWidth), 0, cols - 1);
        int row = Math.Clamp((int)(pos.Y / cellHeight), 0, rows - 1);

        int index = row * cols + col;
        return Math.Clamp(index, 0, zone.Items.Count);
    }

    // ── Zone 標題列拖曳換序 ──────────────────────────────────────────

    private Point _zoneHeaderDragStart;
    private bool _zoneHeaderDragging;

    private void OnZoneHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        _zoneHeaderDragStart = e.GetPosition(this);
        _zoneHeaderDragging = false;
        e.Handled = false; // 讓 TextBox 仍可聚焦
    }

    private void OnZoneHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _zoneHeaderDragging) return;
        if (DataContext is not ZoneViewModel zone) return;

        var diff = e.GetPosition(this) - _zoneHeaderDragStart;
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _zoneHeaderDragging = true;
            var data = new DataObject("ZoneViewModel", zone);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            _zoneHeaderDragging = false;
        }
    }

    private void OnZoneHeaderDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ZoneViewModel"))
        {
            e.Effects = DragDropEffects.Move;
            ZoneHeader.Background = new SolidColorBrush(Color.FromArgb(0x44, 0x6C, 0x8E, 0xEF));
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnZoneHeaderDragLeave(object sender, DragEventArgs e)
    {
        ZoneHeader.Background = (SolidColorBrush)FindResource("PanelBrush");
    }

    private void OnZoneHeaderDrop(object sender, DragEventArgs e)
    {
        ZoneHeader.Background = (SolidColorBrush)FindResource("PanelBrush");

        if (DataContext is not ZoneViewModel targetZone) return;
        if (!e.Data.GetDataPresent("ZoneViewModel")) return;
        if (e.Data.GetData("ZoneViewModel") is not ZoneViewModel sourceZone) return;
        if (ReferenceEquals(sourceZone, targetZone)) return;

        var canvasVm = this.FindAncestor<DesignCanvas>()?.DataContext as DesignCanvasViewModel;
        var mainVm   = this.FindAncestor<DesignCanvas>()?.Tag as MainViewModel;
        if (canvasVm == null) return;

        var fromIndex = canvasVm.Zones.IndexOf(sourceZone);
        var toIndex   = canvasVm.Zones.IndexOf(targetZone);
        if (fromIndex < 0 || toIndex < 0) return;

        if (mainVm != null)
        {
            var cmd = new MoveZoneCommand(canvasVm.Zones, fromIndex, toIndex);
            mainVm.UndoRedo.Execute(cmd);
            mainVm.MarkDirty();
        }
        else
        {
            canvasVm.Zones.Move(fromIndex, toIndex);
        }

        e.Handled = true;
    }

    private void UpdateInsertIndicator(DragEventArgs e)
    {
        if (DataContext is not ZoneViewModel zone) return;

        var uniformGrid = DropTarget.FindDescendant<UniformGrid>();
        if (uniformGrid == null || uniformGrid.ActualWidth <= 0) { HideInsertIndicator(); return; }

        int cols = Math.Max(1, zone.Columns);
        int count = zone.Items.Count;
        int rows = Math.Max(1, (int)Math.Ceiling((double)count / cols));

        double cellWidth = uniformGrid.ActualWidth / cols;
        double cellHeight = count > 0 ? uniformGrid.ActualHeight / rows : 80;

        // 計算插入 index
        var pos = e.GetPosition(uniformGrid);
        int col = Math.Clamp((int)(pos.X / cellWidth), 0, cols - 1);
        int row = Math.Clamp((int)(pos.Y / cellHeight), 0, rows - 1);
        int insertIndex = Math.Clamp(row * cols + col, 0, count);

        // 指示線位置：在 insertIndex cell 的左邊緣
        var gridPos = uniformGrid.TranslatePoint(new Point(0, 0), InsertIndicatorCanvas);
        int indicatorRow = insertIndex / cols;
        int indicatorCol = insertIndex % cols;

        double x = gridPos.X + indicatorCol * cellWidth - 1.5;
        double y = gridPos.Y + indicatorRow * cellHeight;
        double height = cellHeight;

        Canvas.SetLeft(InsertIndicator, x);
        Canvas.SetTop(InsertIndicator, y);
        InsertIndicator.Height = height;
        InsertIndicator.Visibility = Visibility.Visible;
    }

    private void HideInsertIndicator()
    {
        InsertIndicator.Visibility = Visibility.Collapsed;
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

    public static T? FindDescendant<T>(this DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var result = child.FindDescendant<T>();
            if (result != null) return result;
        }
        return null;
    }
}