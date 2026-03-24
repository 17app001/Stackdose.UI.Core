namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// 製程狀態列舉 — 所有設備共用。
/// </summary>
public enum ProcessState
{
    Idle,
    Starting,
    Running,
    Completed,
    Stopped,
    Faulted
}

/// <summary>
/// 製程執行結果。
/// </summary>
public sealed record ProcessExecutionResult(
    bool Success,
    ProcessState State,
    string Message);
