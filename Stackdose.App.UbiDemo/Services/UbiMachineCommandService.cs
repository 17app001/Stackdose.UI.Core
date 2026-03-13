namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiMachineCommandService
{
    public string BuildStartClickMessage(UbiMachineCommandRequest request)
    {
        var machineName = string.IsNullOrWhiteSpace(request.MachineName)
            ? "Unknown Machine"
            : request.MachineName;

        var machineId = string.IsNullOrWhiteSpace(request.MachineId)
            ? "N/A"
            : request.MachineId;

        var commandKey = string.IsNullOrWhiteSpace(request.CommandKey)
            ? "Start"
            : request.CommandKey;

        var parameter = string.IsNullOrWhiteSpace(request.Parameter)
            ? "(empty)"
            : request.Parameter;

        return $"¤wÂIÀ» Start\n\nMachine: {machineName}\nMachine ID: {machineId}\nCommand Key: {commandKey}\nParameter: {parameter}";
    }
}
