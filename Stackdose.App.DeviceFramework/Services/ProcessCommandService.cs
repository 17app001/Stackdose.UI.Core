using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用製程命令服務 — 根據命令名稱和位址寫入 PLC。
/// </summary>
public class ProcessCommandService
{
    public virtual async Task<ProcessExecutionResult> ExecuteCommandAsync(
        string machineId,
        string machineName,
        string commandName,
        string commandAddress)
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

        var writeSucceeded = await manager.WriteAsync($"{commandAddress},1");
        if (!writeSucceeded)
        {
            return new ProcessExecutionResult(
                false,
                ProcessState.Faulted,
                $"'{commandName}' write failed.\n\nMachine: {machineName}\nAddress: {commandAddress}");
        }

        ComplianceContext.LogSystem(
            $"[Process] {commandName} requested: {machineName} ({machineId}) -> {commandAddress}");

        return new ProcessExecutionResult(
            true,
            ProcessState.Starting,
            $"Command '{commandName}' sent.\n\nMachine: {machineName}\nMachine ID: {machineId}\nAddress: {commandAddress}");
    }
}
