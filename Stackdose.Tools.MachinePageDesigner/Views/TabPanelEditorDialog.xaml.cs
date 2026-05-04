using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>
/// 雙擊 TabPanel 開啟的子項目設計器 Dialog。
/// 每個 Tab 有獨立畫布，可從工具箱拖入控件。
/// 儲存後將結果寫回 DesignerItemViewModel.props["tabs"]。
/// </summary>
public partial class TabPanelEditorDialog : Window
{
    private readonly DesignerItemViewModel _tabPanelItem;
    private readonly MainViewModel _editorVm;
    private readonly List<TabData> _tabs = [];
    private int _activeTabIndex = 0;

    // ── Init ─────────────────────────────────────────────────────────────

    public TabPanelEditorDialog(DesignerItemViewModel tabPanelItem)
    {
        _tabPanelItem = tabPanelItem;

        // Parse existing tabs from props
        _tabs = ParseTabs(tabPanelItem.Props);
        if (_tabs.Count == 0)
        {
            _tabs.Add(new TabData("Tab 1", []));
            _tabs.Add(new TabData("Tab 2", []));
        }

        // Create a fresh MainViewModel for this dialog's canvas
        _editorVm = new MainViewModel();
        _editorVm.Canvas.CanvasWidth  = Math.Max(600, tabPanelItem.Width);
        _editorVm.Canvas.CanvasHeight = Math.Max(300, tabPanelItem.Height - 36); // subtract tab header

        InitializeComponent();

        // Wire FreeCanvas to editorVm
        EditorCanvas.DataContext = _editorVm.Canvas;
        EditorCanvas.Tag         = _editorVm;

        // Wire ToolboxPanel
        ToolboxPanelCtrl.DataContext = _editorVm.Toolbox;

        // Build tab header buttons
        BuildTabHeaders();

        // Load first tab
        LoadTab(0);
    }

    // ── Tab switching ─────────────────────────────────────────────────────

    private void BuildTabHeaders()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        for (int i = 0; i < _tabs.Count; i++)
        {
            int idx = i; // capture for lambda
            var btn = new Button
            {
                Content       = _tabs[i].Title,
                Tag           = i,
                MinWidth      = 100,
                Height        = 28,
                Padding       = new Thickness(16, 0, 16, 0),
                Margin        = new Thickness(0, 0, 2, 0),
                BorderThickness = new Thickness(0),
                Cursor        = System.Windows.Input.Cursors.Hand,
            };
            btn.Click += (_, _) => SwitchTab(idx);
            UpdateTabButtonStyle(btn, idx == 0);
            panel.Children.Add(btn);
        }

        TabHeadersControl.Content = panel;
    }

    private void SwitchTab(int index)
    {
        if (index == _activeTabIndex) return;
        SaveCurrentTabToEntry();
        _activeTabIndex = index;
        LoadTab(index);
        RefreshTabButtonStyles();
    }

    private void RefreshTabButtonStyles()
    {
        if (TabHeadersControl.Content is not StackPanel panel) return;
        int i = 0;
        foreach (var child in panel.Children)
        {
            if (child is Button btn)
                UpdateTabButtonStyle(btn, i++ == _activeTabIndex);
        }
    }

    private static void UpdateTabButtonStyle(Button btn, bool isActive)
    {
        btn.Background = isActive
            ? new SolidColorBrush(Color.FromRgb(0x3A, 0x56, 0x90))
            : new SolidColorBrush(Color.FromRgb(0x28, 0x2C, 0x3E));
        btn.Foreground = isActive
            ? System.Windows.Media.Brushes.White
            : new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xCC));
        btn.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
    }

    // ── Load / Save tab data ──────────────────────────────────────────────

    private void LoadTab(int index)
    {
        _editorVm.Canvas.ClearSelection();
        _editorVm.Canvas.CanvasItems.Clear();

        foreach (var def in _tabs[index].Items)
            _editorVm.Canvas.CanvasItems.Add(new DesignerItemViewModel(def.Clone()));
    }

    private void SaveCurrentTabToEntry()
    {
        _tabs[_activeTabIndex].Items = _editorVm.Canvas.CanvasItems
            .Select(vm => vm.ToDefinition())
            .ToList();
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private void OnSave(object sender, RoutedEventArgs e)
    {
        SaveCurrentTabToEntry();

        // Serialize all tabs back to props["tabs"]
        var tabsArray = _tabs.Select(t => new RawTabEntry
        {
            title = t.Title,
            items = t.Items.ToArray(),
        }).ToArray();

        var element = JsonSerializer.SerializeToElement(tabsArray);
        _tabPanelItem.SetProp("tabs", element);

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Parsing ───────────────────────────────────────────────────────────

    private static List<TabData> ParseTabs(Dictionary<string, object?> props)
    {
        if (!props.TryGetValue("tabs", out var raw)) return [];
        try
        {
            var je = raw is JsonElement j ? j : JsonSerializer.SerializeToElement(raw);
            var result = new List<TabData>();
            foreach (var tab in je.EnumerateArray())
            {
                var title = tab.TryGetProperty("title", out var t) ? t.GetString() ?? "Tab" : "Tab";
                var items = new List<DesignerItemDefinition>();
                if (tab.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                {
                    var defs = JsonSerializer.Deserialize<DesignerItemDefinition[]>(
                        itemsEl.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (defs != null) items.AddRange(defs);
                }
                result.Add(new TabData(title, items));
            }
            return result;
        }
        catch { return []; }
    }

    // ── Inner types ───────────────────────────────────────────────────────

    private sealed class TabData(string title, List<DesignerItemDefinition> items)
    {
        public string Title { get; } = title;
        public List<DesignerItemDefinition> Items { get; set; } = items;
    }

    // Matches the JSON schema used by RuntimeControlFactory
    private sealed class RawTabEntry
    {
        public string? title { get; set; }
        public DesignerItemDefinition[]? items { get; set; }
    }
}
