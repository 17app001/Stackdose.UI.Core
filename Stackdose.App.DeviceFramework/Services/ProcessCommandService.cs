using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// �q�λs�{�R�O�A�� �X �ھکR�O�W�٩M��}�g�J PLC�C
/// </summary>
public class ProcessCommandService
{
    public virtual async Task<ProcessExecutionResult> ExecuteCommandAsync(
        string machineId,
        string machineName,
        string commandName,
        string commandAddress)
        => await ExecuteCommandAsync(machineId, machineName, commandName, commandAddress, "1");

    public virtual async Task<ProcessExecutionResult> ExecuteCommandAsync(
        string machineId,
        string machineName,
        string commandName,
        string commandAddress,
        string writeValue)
    {
        if (string.IsNullOrWhiteSpace(commandAddress) || commandAddress == "--")
        {
            return new ProcessExecutionResult(
                false,
                ProcessState.Faulted,
                $"Cannot execute '{commandName}': machine '{machineName}' has no address configured for this command.");
        }

        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected)
        {
            return new ProcessExecutionResult(
                false,
                ProcessState.Faulted,
                $"Cannot execute '{commandName}': PLC not connected.\n\nMachine: {machineName}\nAddress: {commandAddress}");
        }

        var writeSucceeded = await manager.WriteAsync($"{commandAddress},{writeValue}");
        if (!writeSucceeded)
        {
            return new ProcessExecutionResult(
                false,
                ProcessState.Faulted,
                $"'{commandName}' write failed.\n\nMachine: {machineName}\nAddress: {commandAddress}\nValue: {writeValue}");
        }

        ComplianceContext.LogSystem(
            $"[Process] {commandName} requested: {machineName} ({machineId}) -> {commandAddress}={writeValue}",
            machineId: machineId);

        return new ProcessExecutionResult(
            true,
            ProcessState.Starting,
            $"Command '{commandName}' sent.\n\nMachine: {machineName}\nMachine ID: {machineId}\nAddress: {commandAddress}\nValue: {writeValue}");
    }
}
