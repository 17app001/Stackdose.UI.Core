namespace Stackdose.Tools.MachinePageDesigner.Services;

/// <summary>
/// UndoRedo 機制：Command Pattern + Stack
/// </summary>
public interface IDesignCommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

public sealed class UndoRedoService
{
    private readonly Stack<IDesignCommand> _undoStack = new();
    private readonly Stack<IDesignCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event Action? StateChanged;

    public void Execute(IDesignCommand cmd)
    {
        cmd.Execute();
        _undoStack.Push(cmd);
        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.TryPop(out var cmd))
        {
            cmd.Undo();
            _redoStack.Push(cmd);
            StateChanged?.Invoke();
        }
    }

    public void Redo()
    {
        if (_redoStack.TryPop(out var cmd))
        {
            cmd.Execute();
            _undoStack.Push(cmd);
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 僅記錄 Command 至 Undo Stack，不重新執行（用於 UI 已直接套用變更的情況）
    /// </summary>
    public void Record(IDesignCommand cmd)
    {
        _undoStack.Push(cmd);
        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 防抖用：若 Undo Stack 頂端是相同 item+propKey 的 PropertyChangeCommand，
    /// 更新其 NewValue 而不新增記錄。
    /// </summary>
    public void UpdateTopPropertyCommand(object item, string propKey, object? newValue)
    {
        if (_undoStack.TryPeek(out var top) && top is PropertyChangeCommand pcc)
            pcc.UpdateNewValue(item, propKey, newValue);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}
