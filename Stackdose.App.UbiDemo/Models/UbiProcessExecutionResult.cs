namespace Stackdose.App.UbiDemo.Models;

public sealed record UbiProcessExecutionResult(
    bool Success,
    UbiProcessState State,
    string Message);
