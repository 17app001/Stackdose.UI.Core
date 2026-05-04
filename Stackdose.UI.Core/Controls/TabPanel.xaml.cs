using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls;

/// <summary>
/// 可在 MachinePageDesigner / DesignRuntime 中使用的多 Tab 容器。
/// 每個 Tab 對應一個標題 + 一個 UIElement 內容。
/// </summary>
public partial class TabPanel : UserControl
{
    private readonly List<TabEntry> _tabs = [];
    private int _activeIndex = -1;

    public TabPanel()
    {
        InitializeComponent();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// 加入一個 Tab。第一個加入的 Tab 自動成為 active。
    /// </summary>
    public void AddTab(string title, UIElement content)
    {
        _tabs.Add(new TabEntry(title, content));
        TabHeaders.ItemsSource = null;
        TabHeaders.ItemsSource = _tabs;

        if (_activeIndex < 0)
            ActivateTab(0);
    }

    /// <summary>
    /// 以 0-based index 切換到指定 Tab。
    /// </summary>
    public void ActivateTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        _activeIndex = index;
        ContentArea.Child = _tabs[index].Content;
        RefreshHeaderStyles();
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private void OnTabHeaderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TabEntry entry)
            ActivateTab(_tabs.IndexOf(entry));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void RefreshHeaderStyles()
    {
        // Update visual state via attached Tag on each generated container
        if (TabHeaders.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            return;

        for (int i = 0; i < _tabs.Count; i++)
        {
            var container = TabHeaders.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
            if (container == null) continue;
            var btn = FindChild<Button>(container);
            if (btn == null) continue;

            bool isActive = i == _activeIndex;
            btn.Background = isActive
                ? (Brush)TryFindResource("Plc.Bg.Main") ?? Brushes.Transparent
                : Brushes.Transparent;
            btn.Foreground = isActive
                ? (Brush)TryFindResource("Plc.Text.Primary") ?? Brushes.White
                : (Brush)TryFindResource("Plc.Text.Muted") ?? Brushes.Gray;
            btn.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    // ── Inner type ────────────────────────────────────────────────────────

    internal sealed class TabEntry(string title, UIElement content)
    {
        public string Title { get; } = title;
        public UIElement Content { get; } = content;
    }
}
