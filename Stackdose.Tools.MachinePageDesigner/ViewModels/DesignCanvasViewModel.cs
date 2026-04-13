using System.Collections.ObjectModel;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 自由畫布狀態 ViewModel（選取、元件管理）
/// </summary>
public sealed class DesignCanvasViewModel : ObservableObject
{
    private DesignerItemViewModel? _selectedItem;

    /// <summary>多選清單</summary>
    public ObservableCollection<DesignerItemViewModel> SelectedItems { get; } = [];

    public DesignerItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != null)
                _selectedItem.IsSelected = false;

            Set(ref _selectedItem, value);

            if (_selectedItem != null)
                _selectedItem.IsSelected = true;

            N(nameof(HasSelectedItem));
        }
    }

    public bool HasSelectedItem => _selectedItem != null || SelectedItems.Count > 0;

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
    /// 清除選取
    /// </summary>
    public void ClearSelection()
    {
        ClearMultiSelection();
        SelectedItem = null;
    }

    // ── 自由畫布 ──────────────────────────────────────────────────────────

    public ObservableCollection<DesignerItemViewModel> CanvasItems { get; } = [];

    private double _canvasWidth = 1200;
    private double _canvasHeight = 750;

    public double CanvasWidth
    {
        get => _canvasWidth;
        set => Set(ref _canvasWidth, value);
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        set => Set(ref _canvasHeight, value);
    }

    /// <summary>
    /// 從 DesignDocument 載入畫布元件
    /// </summary>
    public void LoadFromDocument(DesignDocument doc)
    {
        CanvasItems.Clear();
        ClearSelection();
        CanvasWidth = doc.CanvasWidth;
        CanvasHeight = doc.CanvasHeight;
        foreach (var def in doc.CanvasItems)
            CanvasItems.Add(new DesignerItemViewModel(def));
    }

    /// <summary>
    /// 匯出畫布元件定義
    /// </summary>
    public List<DesignerItemDefinition> ExportCanvasItems()
        => CanvasItems.Select(vm => vm.ToDefinition()).ToList();
}
