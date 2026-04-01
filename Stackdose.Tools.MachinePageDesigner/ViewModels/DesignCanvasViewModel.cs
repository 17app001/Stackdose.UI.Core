using System.Collections.ObjectModel;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 畫布狀態 ViewModel（選取、Zone 管理）
/// </summary>
public sealed class DesignCanvasViewModel : ObservableObject
{
    private DesignerItemViewModel? _selectedItem;
    private ZoneViewModel? _selectedZone;

    public ObservableCollection<ZoneViewModel> Zones { get; } = [];

    /// <summary>多選清單</summary>
    public ObservableCollection<DesignerItemViewModel> SelectedItems { get; } = [];

    public DesignerItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            // 清除舊選取
            if (_selectedItem != null)
                _selectedItem.IsSelected = false;

            Set(ref _selectedItem, value);

            // 設定新選取
            if (_selectedItem != null)
                _selectedItem.IsSelected = true;

            N(nameof(HasSelectedItem));
        }
    }

    public bool HasSelectedItem => _selectedItem != null || SelectedItems.Count > 0;

    public ZoneViewModel? SelectedZone
    {
        get => _selectedZone;
        set => Set(ref _selectedZone, value);
    }

    /// <summary>
    /// 單選模式（清除多選）
    /// </summary>
    public void SelectSingle(DesignerItemViewModel item)
    {
        ClearMultiSelection();
        SelectedItem = item;
    }

    /// <summary>
    /// 多選模式（Shift+Click）
    /// </summary>
    public void ToggleMultiSelect(DesignerItemViewModel item)
    {
        if (SelectedItems.Contains(item))
        {
            SelectedItems.Remove(item);
            item.IsSelected = false;
        }
        else
        {
            SelectedItems.Add(item);
            item.IsSelected = true;
        }

        // 同步 SelectedItem 為最後選取的
        if (SelectedItems.Count > 0)
        {
            _selectedItem = SelectedItems[^1];
            N(nameof(SelectedItem));
        }
        else
        {
            SelectedItem = null;
        }
        N(nameof(HasSelectedItem));
    }

    /// <summary>
    /// 清除多選狀態
    /// </summary>
    private void ClearMultiSelection()
    {
        foreach (var item in SelectedItems)
            item.IsSelected = false;
        SelectedItems.Clear();
    }

    /// <summary>
    /// 取得所有已選取項目（合併單選+多選）
    /// </summary>
    public List<DesignerItemViewModel> GetAllSelectedItems()
    {
        if (SelectedItems.Count > 0)
            return [.. SelectedItems];
        if (_selectedItem != null)
            return [_selectedItem];
        return [];
    }

    /// <summary>
    /// 從 DesignDocument 載入 Zones
    /// </summary>
    public void LoadFromDocument(DesignDocument doc)
    {
        Zones.Clear();
        SelectedItem = null;
        ClearMultiSelection();
        foreach (var (key, def) in doc.Zones)
            Zones.Add(new ZoneViewModel(key, def));
    }

    /// <summary>
    /// 匯出回 DesignDocument 的 Zones
    /// </summary>
    public Dictionary<string, ZoneDefinition> ExportZones()
        => Zones.ToDictionary(z => z.ZoneKey, z => z.ToDefinition());

    /// <summary>
    /// 刪除目前選取的項目（單選）
    /// </summary>
    public void DeleteSelectedItem()
    {
        if (_selectedItem == null) return;

        var zone = Zones.FirstOrDefault(z => z.Items.Contains(_selectedItem));
        if (zone != null)
        {
            zone.RemoveItem(_selectedItem);
            SelectedItem = null;
        }
    }

    /// <summary>
    /// 尋找項目所屬的 Zone
    /// </summary>
    public ZoneViewModel? FindZoneOf(DesignerItemViewModel item)
        => Zones.FirstOrDefault(z => z.Items.Contains(item));

    /// <summary>
    /// 清除選取
    /// </summary>
    public void ClearSelection()
    {
        ClearMultiSelection();
        SelectedItem = null;
    }
}