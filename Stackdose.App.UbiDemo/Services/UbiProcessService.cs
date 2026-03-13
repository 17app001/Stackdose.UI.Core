using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiProcessService
{
    public async Task<UbiProcessExecutionResult> StartProcessAsync(UbiMachineCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StartCommandAddress) || request.StartCommandAddress == "--")
        {
            return new UbiProcessExecutionResult(
                false,
                UbiProcessState.Faulted,
                $"無法啟動製程：機台 {request.MachineName} 尚未設定 Start command address。");
        }

        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected)
        {
            return new UbiProcessExecutionResult(
                false,
                UbiProcessState.Faulted,
                $"無法啟動製程：PLC 尚未連線。\n\nMachine: {request.MachineName}\nStart Address: {request.StartCommandAddress}");
        }

        var writeSucceeded = await manager.WriteAsync($"{request.StartCommandAddress},1");
        if (!writeSucceeded)
        {
            return new UbiProcessExecutionResult(
                false,
                UbiProcessState.Faulted,
                $"Start 命令寫入失敗。\n\nMachine: {request.MachineName}\nStart Address: {request.StartCommandAddress}");
        }

        ComplianceContext.LogSystem(
            $"[UbiProcess] Start requested: {request.MachineName} ({request.MachineId}) -> {request.StartCommandAddress}");

        return new UbiProcessExecutionResult(
            true,
            UbiProcessState.Starting,
            $"已送出 Start 命令\n\nMachine: {request.MachineName}\nMachine ID: {request.MachineId}\nCommand Key: {request.CommandKey}\nParameter: {request.Parameter}\nStart Address: {request.StartCommandAddress}");
    }
}
