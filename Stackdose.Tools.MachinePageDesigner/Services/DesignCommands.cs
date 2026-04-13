using System.Collections.ObjectModel;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Services;

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
/// 自由畫布元件 Z 順序調整
/// </summary>
public sealed class ReorderCanvasItemCommand : IDesignCommand
{
    private readonly ObservableCollection<DesignerItemViewModel> _items;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    public ReorderCanvasItemCommand(ObservableCollection<DesignerItemViewModel> items, int fromIndex, int toIndex)
    { _items = items; _fromIndex = fromIndex; _toIndex = toIndex; }

    public string Description => $"Reorder item {_fromIndex} → {_toIndex}";
    public void Execute() => _items.Move(_fromIndex, _toIndex);
    public void Undo() => _items.Move(_toIndex, _fromIndex);
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
