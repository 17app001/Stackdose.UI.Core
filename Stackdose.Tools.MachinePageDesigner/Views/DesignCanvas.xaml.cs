using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class DesignCanvas : UserControl
{
    public DesignCanvas()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Tag is MainViewModel mainVm)
            mainVm.PropertyChanged += OnMainVmPropertyChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Tag is MainViewModel mainVm)
            mainVm.PropertyChanged -= OnMainVmPropertyChanged;
    }

    private void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.LayoutMode))
        {
            // 找到 LayoutModePanel 並強制重新排列
            var panel = FindDescendant<LayoutModePanel>(this);
            if (panel != null)
            {
                panel.InvalidateMeasure();
                panel.InvalidateArrange();
            }
        }
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var result = FindDescendant<T>(child);
            if (result != null) return result;
        }
        return null;
    }
}

/// <summary>
/// 根據 MainViewModel.LayoutMode 動態切換子項排列方式的面板。
/// - Standard：垂直堆疊（所有 Zone 上下排列）
/// - SplitRight：第一個 Zone 在左，其餘在右（水平分割）
/// - SplitBottom：第一個 Zone 在上，其餘在下（垂直分割，上下比例不同）
/// </summary>
public class LayoutModePanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var mode = GetLayoutMode();
        var children = InternalChildren;

        if (children.Count == 0) return new Size(0, 0);

        switch (mode)
        {
            case "SplitRight":
                return MeasureSplitRight(availableSize);
            case "SplitBottom":
                return MeasureSplitBottom(availableSize);
            default:
                return MeasureStandard(availableSize);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var mode = GetLayoutMode();
        var children = InternalChildren;

        if (children.Count == 0) return finalSize;

        switch (mode)
        {
            case "SplitRight":
                ArrangeSplitRight(finalSize);
                break;
            case "SplitBottom":
                ArrangeSplitBottom(finalSize);
                break;
            default:
                ArrangeStandard(finalSize);
                break;
        }

        return finalSize;
    }

    // ── Standard：垂直堆疊 ──

    private Size MeasureStandard(Size available)
    {
        double totalHeight = 0;
        double maxWidth = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(available);
            totalHeight += child.DesiredSize.Height;
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
        }
        return new Size(maxWidth, totalHeight);
    }

    private void ArrangeStandard(Size final)
    {
        double y = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Arrange(new Rect(0, y, final.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
    }

    // ── SplitRight：左右分割（第一個 Zone 在左） ──

    private Size MeasureSplitRight(Size available)
    {
        var children = InternalChildren;
        double leftWidth = available.Width * 0.4;
        double rightWidth = available.Width * 0.6;

        if (children.Count >= 1)
            children[0].Measure(new Size(leftWidth, available.Height));

        double rightHeight = 0;
        for (int i = 1; i < children.Count; i++)
        {
            children[i].Measure(new Size(rightWidth, available.Height));
            rightHeight += children[i].DesiredSize.Height;
        }

        double totalHeight = children.Count >= 1
            ? Math.Max(children[0].DesiredSize.Height, rightHeight)
            : 0;

        return new Size(available.Width, totalHeight);
    }

    private void ArrangeSplitRight(Size final)
    {
        var children = InternalChildren;
        double leftWidth = final.Width * 0.4;
        double rightWidth = final.Width * 0.6;

        if (children.Count >= 1)
            children[0].Arrange(new Rect(0, 0, leftWidth, final.Height));

        double y = 0;
        for (int i = 1; i < children.Count; i++)
        {
            children[i].Arrange(new Rect(leftWidth, y, rightWidth, children[i].DesiredSize.Height));
            y += children[i].DesiredSize.Height;
        }
    }

    // ── SplitBottom：上下分割（第一個 Zone 在上佔 60%） ──

    private Size MeasureSplitBottom(Size available)
    {
        var children = InternalChildren;
        double topHeight = available.Height * 0.6;
        double bottomHeight = available.Height * 0.4;

        if (children.Count >= 1)
            children[0].Measure(new Size(available.Width, topHeight));

        for (int i = 1; i < children.Count; i++)
            children[i].Measure(new Size(available.Width, bottomHeight));

        return new Size(available.Width, available.Height);
    }

    private void ArrangeSplitBottom(Size final)
    {
        var children = InternalChildren;
        double topHeight = final.Height * 0.6;

        if (children.Count >= 1)
            children[0].Arrange(new Rect(0, 0, final.Width, topHeight));

        double y = topHeight;
        double remainHeight = final.Height - topHeight;
        int bottomCount = Math.Max(1, children.Count - 1);
        double eachHeight = remainHeight / bottomCount;

        for (int i = 1; i < children.Count; i++)
        {
            children[i].Arrange(new Rect(0, y, final.Width, eachHeight));
            y += eachHeight;
        }
    }

    // ── Helper ──

    private string GetLayoutMode()
    {
        var parent = this as DependencyObject;
        while (parent != null)
        {
            if (parent is DesignCanvas canvas && canvas.Tag is MainViewModel mainVm)
                return mainVm.LayoutMode;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return "Standard";
    }
}