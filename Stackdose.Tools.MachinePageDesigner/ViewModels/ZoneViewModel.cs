using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 單一 Zone 的項目清單、欄數、標題
/// </summary>
public sealed class ZoneViewModel : ObservableObject
{
    private readonly string _zoneKey;
    private string _title;
    private int _columns;
    private int _rows;

    public ZoneViewModel(string zoneKey, ZoneDefinition def)
    {
        _zoneKey = zoneKey;
        _title = def.Title;
        _columns = def.Columns;
        _rows = def.Rows;

        foreach (var itemDef in def.Items.OrderBy(i => i.Order))
            Items.Add(new DesignerItemViewModel(itemDef));

        Items.CollectionChanged += OnItemsChanged;
    }

    // ── Properties ───────────────────────────────────────────────
    public string ZoneKey => _zoneKey;

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public int Columns
    {
        get => _columns;
        set { if (value >= 1 && value <= 6) Set(ref _columns, value); }
    }

    public int Rows
    {
        get => _rows;
        set { if (value >= 0 && value <= 10) Set(ref _rows, value); }
    }

    public ObservableCollection<DesignerItemViewModel> Items { get; } = [];

    /// <summary>欄數選項（UI ComboBox 綁定用）</summary>
    public int[] ColumnOptions { get; } = [1, 2, 3, 4, 5, 6];

    /// <summary>列數選項（0=自動）</summary>
    public int[] RowOptions { get; } = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    // ── Methods ──────────────────────────────────────────────────

    public void AddItem(DesignerItemViewModel item, int index = -1)
    {
        item.Order = index >= 0 ? index : Items.Count;
        if (index >= 0 && index <= Items.Count)
            Items.Insert(index, item);
        else
            Items.Add(item);
        ReorderItems();
    }

    public void RemoveItem(DesignerItemViewModel item)
    {
        Items.Remove(item);
        ReorderItems();
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count) return;
        if (toIndex < 0 || toIndex >= Items.Count) return;
        Items.Move(fromIndex, toIndex);
        ReorderItems();
    }

    // ── Export ────────────────────────────────────────────────────

    public ZoneDefinition ToDefinition() => new()
    {
        Title = Title,
        Columns = Columns,
        Rows = Rows,
        Items = Items.Select(i => i.ToDefinition()).ToList()
    };

    // ── Private ──────────────────────────────────────────────────

    private void ReorderItems()
    {
        for (int i = 0; i < Items.Count; i++)
            Items[i].Order = i;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => N(nameof(Items));
}
