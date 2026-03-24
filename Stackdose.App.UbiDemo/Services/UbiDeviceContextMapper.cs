using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.UbiDemo.Models;

namespace Stackdose.App.UbiDemo.Services;

/// <summary>
/// 從框架的 DeviceContext 映射到 Ubi 本地的 UbiDeviceContext。
/// </summary>
internal static class UbiDeviceContextMapper
{
    /// <summary>
    /// 從框架的 DeviceContext 轉換成 UbiDeviceContext。
    /// 框架的 DeviceContext 使用 Labels 字典和 Commands 字典，
    /// UbiDeviceContext 使用硬編碼屬性。
    /// </summary>
    public static UbiDeviceContext FromFrameworkContext(DeviceContext context)
    {
        var ubiCtx = new UbiDeviceContext
        {
            MachineId = context.MachineId,
            MachineName = context.MachineName,
            RunningAddress = context.RunningAddress,
            CompletedAddress = context.CompletedAddress,
            AlarmAddress = context.AlarmAddress,
            AlarmConfigFile = context.AlarmConfigFile,
            SensorConfigFile = context.SensorConfigFile,
            PrintHead1ConfigFile = context.PrintHeadConfigFiles.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = context.PrintHeadConfigFiles.ElementAtOrDefault(1) ?? string.Empty,
        };

        // Commands
        ubiCtx.StartCommandAddress = context.Commands.TryGetValue("Start", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "--";
        ubiCtx.PauseCommandAddress = context.Commands.TryGetValue("Pause", out var p) && !string.IsNullOrWhiteSpace(p) ? p : "--";
        ubiCtx.StopCommandAddress = context.Commands.TryGetValue("Stop", out var st) && !string.IsNullOrWhiteSpace(st) ? st : "--";

        // Labels → Ubi 硬編碼屬性
        ubiCtx.BatchAddress = GetLabelAddress(context, "batchNo", "--");
        ubiCtx.RecipeAddress = GetLabelAddress(context, "recipeNo", "--");
        ubiCtx.NozzleAddress = GetLabelAddress(context, "nozzleTemp", "--");
        ubiCtx.TotalTrayAddress = GetLabelAddress(context, "totalTray", "D3400");
        ubiCtx.CurrentTrayAddress = GetLabelAddress(context, "currentTray", "D33");
        ubiCtx.TotalLayerAddress = GetLabelAddress(context, "totalLayer", "D3401");
        ubiCtx.CurrentLayerAddress = GetLabelAddress(context, "currentLayer", "D32");
        ubiCtx.SwitchGraphicLayerAddress = GetLabelAddress(context, "switchGraphicLayer", "D510");
        ubiCtx.SwitchAreaLayerAddress = GetLabelAddress(context, "switchAreaLayer", "D512");
        ubiCtx.MessageIdAddress = GetLabelAddress(context, "messageId", "D85");
        ubiCtx.BatteryAddress = GetLabelAddress(context, "battery", "D120");
        ubiCtx.ElapsedTimeAddress = GetLabelAddress(context, "elapsedTime", "D86");
        ubiCtx.PrintHeadCountAddress = GetLabelAddress(context, "printHeadCount", "D87");

        return ubiCtx;
    }

    private static string GetLabelAddress(DeviceContext context, string key, string fallback)
    {
        if (context.Labels.TryGetValue(key, out var info) && !string.IsNullOrWhiteSpace(info.Address))
        {
            return info.Address;
        }

        return fallback;
    }
}
