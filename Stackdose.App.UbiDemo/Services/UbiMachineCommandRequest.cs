namespace Stackdose.App.UbiDemo.Services;

internal sealed record UbiMachineCommandRequest(
    string MachineId,
    string MachineName,
    string CommandKey,
    string Parameter);
