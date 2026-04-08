using System.Collections.ObjectModel;
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
    private object? _newValue;

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

    /// <summary>
    /// 防抖用：若為相同 item + propKey，更新 NewValue（UI 已套用，Undo 時還原至 _oldValue）
    /// </summary>
    public void UpdateNewValue(object item, string propKey, object? newValue)
    {
        if (ReferenceEquals(_item, item) && _propKey == propKey)
            _newValue = newValue;
    }
}

/// <summary>
/// Move item across Zones (cross-zone drag)
/// </summary>
public sealed class CrossZoneMoveCommand : IDesignCommand
{
    private readonly ZoneViewModel _sourceZone;
    private readonly ZoneViewModel _targetZone;
    private readonly DesignerItemViewModel _item;
    private int _sourceIndex;
    private readonly int _targetIndex;

    public CrossZoneMoveCommand(ZoneViewModel sourceZone, ZoneViewModel targetZone, DesignerItemViewModel item, int targetIndex)
    {
        _sourceZone = sourceZone;
        _targetZone = targetZone;
        _item = item;
        _sourceIndex = sourceZone.Items.IndexOf(item);
        _targetIndex = targetIndex;
    }

    public string Description => $"Move {_item.ItemType} from {_sourceZone.Title} to {_targetZone.Title}";

    public void Execute()
    {
        _sourceIndex = _sourceZone.Items.IndexOf(_item);
        _sourceZone.RemoveItem(_item);
        _targetZone.AddItem(_item, _targetIndex);
    }

    public void Undo()
    {
        _targetZone.RemoveItem(_item);
        _sourceZone.AddItem(_item, _sourceIndex);
    }
}

/// <summary>
/// Reorder Zones by moving fromIndex to toIndex
/// </summary>
public sealed class MoveZoneCommand(ObservableCollection<ZoneViewModel> zones, int fromIndex, int toIndex) : IDesignCommand
{
    public string Description => $"Move Zone {fromIndex} → {toIndex}";
    public void Execute() => zones.Move(fromIndex, toIndex);
    public void Undo()    => zones.Move(toIndex, fromIndex);
}

/// <summary>
/// Add a Zone to the canvas
/// </summary>
public sealed class AddZoneCommand(ObservableCollection<ZoneViewModel> zones, ZoneViewModel zone, int index) : IDesignCommand
{
    public string Description => $"Add Zone '{zone.Title}'";
    public void Execute() => zones.Insert(Math.Clamp(index, 0, zones.Count), zone);
    public void Undo() => zones.Remove(zone);
}

/// <summary>
/// Remove a Zone from the canvas
/// </summary>
public sealed class RemoveZoneCommand : IDesignCommand
{
    private readonly ObservableCollection<ZoneViewModel> _zones;
    private readonly ZoneViewModel _zone;
    private int _index;

    public RemoveZoneCommand(ObservableCollection<ZoneViewModel> zones, ZoneViewModel zone)
    {
        _zones = zones;
        _zone = zone;
        _index = zones.IndexOf(zone);
    }

    public string Description => $"Remove Zone '{_zone.Title}'";

    public void Execute()
    {
        _index = _zones.IndexOf(_zone);
        _zones.Remove(_zone);
    }

    public void Undo() => _zones.Insert(Math.Clamp(_index, 0, _zones.Count), _zone);
}

/// <summary>
/// 在自由畫布新增元件
/// </summary>
public sealed class CanvasAddItemCommand : IDesignCommand
{
    private readonly ObservableCollection<DesignerItemViewModel> _items;
    private readonly DesignerItemViewModel _item;

    public CanvasAddItemCommand(ObservableCollection<DesignerItemViewModel> items, DesignerItemViewModel item)
    { _items = items; _item = item; }

    public string Description => $"Add {_item.ItemType} to canvas";
    public void Execute() => _items.Add(_item);
    public void Undo() => _items.Remove(_item);
}

/// <summary>
/// 從自由畫布移除元件
/// </summary>
public sealed class CanvasRemoveItemCommand : IDesignCommand
{
    private readonly ObservableCollection<DesignerItemViewModel> _items;
    private readonly DesignerItemViewModel _item;
    private int _index;

    public CanvasRemoveItemCommand(ObservableCollection<DesignerItemViewModel> items, DesignerItemViewModel item)
    { _items = items; _item = item; }

    public string Description => $"Remove {_item.ItemType} from canvas";
    public void Execute() { _index = _items.IndexOf(_item); _items.Remove(_item); }
    public void Undo() => _items.Insert(Math.Clamp(_index, 0, _items.Count), _item);
}

/// <summary>
/// 自由畫布元件移動
/// </summary>
public sealed class CanvasMoveItemCommand : IDesignCommand
{
    private readonly DesignerItemViewModel _item;
    private readonly double _oldX, _oldY;
    private double _newX, _newY;

    public CanvasMoveItemCommand(DesignerItemViewModel item, double oldX, double oldY, double newX, double newY)
    { _item = item; _oldX = oldX; _oldY = oldY; _newX = newX; _newY = newY; }

    public string Description => $"Move {_item.ItemType}";
    public void Execute() { _item.X = _newX; _item.Y = _newY; }
    public void Undo() { _item.X = _oldX; _item.Y = _oldY; }
    public void UpdateNewPosition(double x, double y) { _newX = x; _newY = y; }
}

/// <summary>
/// 自由畫布元件大小調整
/// </summary>
public sealed class CanvasResizeItemCommand : IDesignCommand
{
    private readonly DesignerItemViewModel _item;
    private readonly double _oldX, _oldY, _oldW, _oldH;
    private double _newX, _newY, _newW, _newH;

    public CanvasResizeItemCommand(DesignerItemViewModel item,
        double oldX, double oldY, double oldW, double oldH,
        double newX, double newY, double newW, double newH)
    {
        _item = item;
        _oldX = oldX; _oldY = oldY; _oldW = oldW; _oldH = oldH;
        _newX = newX; _newY = newY; _newW = newW; _newH = newH;
    }

    public string Description => $"Resize {_item.ItemType}";
    public void Execute() { _item.X = _newX; _item.Y = _newY; _item.Width = _newW; _item.Height = _newH; }
    public void Undo() { _item.X = _oldX; _item.Y = _oldY; _item.Width = _oldW; _item.Height = _oldH; }
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