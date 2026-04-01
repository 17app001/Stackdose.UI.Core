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

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}
