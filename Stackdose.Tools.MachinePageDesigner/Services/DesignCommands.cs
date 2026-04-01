using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Services;

/// <summary>
/// Add item to Zone
/// </summary>
public sealed class AddItemCommand(ZoneViewModel zone, DesignerItemViewModel item, int index) : IDesignCommand
{
    public string Description => $"Add {item.ItemType} to {zone.Title}";

    public void Execute() => zone.AddItem(item, index);
    public void Undo() => zone.RemoveItem(item);
}

/// <summary>
/// Remove item from Zone
/// </summary>
public sealed class RemoveItemCommand : IDesignCommand
{
    private readonly ZoneViewModel _zone;
    private readonly DesignerItemViewModel _item;
    private int _index;

    public RemoveItemCommand(ZoneViewModel zone, DesignerItemViewModel item)
    {
        _zone = zone;
        _item = item;
        _index = zone.Items.IndexOf(item);
    }

    public string Description => $"Remove {_item.ItemType} from {_zone.Title}";

    public void Execute()
    {
        _index = _zone.Items.IndexOf(_item);
        _zone.RemoveItem(_item);
    }

    public void Undo() => _zone.AddItem(_item, _index);
}

/// <summary>
/// Move item within Zone (reorder)
/// </summary>
public sealed class MoveItemCommand(ZoneViewModel zone, int fromIndex, int toIndex) : IDesignCommand
{
    public string Description => $"Move item {fromIndex} -> {toIndex} in {zone.Title}";

    public void Execute() => zone.MoveItem(fromIndex, toIndex);
    public void Undo() => zone.MoveItem(toIndex, fromIndex);
}

/// <summary>
/// Property change (supports Undo)
/// </summary>
public sealed class PropertyChangeCommand : IDesignCommand
{
    private readonly DesignerItemViewModel _item;
    private readonly string _propKey;
    private readonly object? _oldValue;
    private readonly object? _newValue;

    public PropertyChangeCommand(DesignerItemViewModel item, string propKey, object? oldValue, object? newValue)
    {
        _item = item;
        _propKey = propKey;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public string Description => $"Change {_item.ItemType}.{_propKey}";

    public void Execute() => _item.SetPropDirect(_propKey, _newValue);
    public void Undo() => _item.SetPropDirect(_propKey, _oldValue);
}

/// <summary>
/// Batch delete multiple items
/// </summary>
public sealed class BatchDeleteCommand : IDesignCommand
{
    private readonly List<(ZoneViewModel zone, DesignerItemViewModel item, int index)> _entries;

    public BatchDeleteCommand(List<(ZoneViewModel zone, DesignerItemViewModel item, int index)> entries)
    {
        _entries = entries;
    }

    public string Description => $"BatchDelete {_entries.Count} items";

    public void Execute()
    {
        foreach (var (zone, item, _) in _entries)
            zone.RemoveItem(item);
    }

    public void Undo()
    {
        foreach (var (zone, item, index) in _entries.AsEnumerable().Reverse())
            zone.AddItem(item, index);
    }
}