using Stackdose.App.UbiDemo.Models;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiDeviceContextMapper
{
    public static DeviceContext CreateDeviceContext(UbiMachineConfig config, IUbiRuntimeMappingAdapter adapter)
    {
        var printHeadConfigs = adapter.GetPrintHeadConfigFiles(config);

        return new DeviceContext
        {
            MachineId = config.Machine.Id,
            MachineName = config.Machine.Name,
            BatchAddress = adapter.GetTagAddress(config, "process", "batchNo"),
            RecipeAddress = adapter.GetTagAddress(config, "process", "recipeNo"),
            NozzleAddress = adapter.GetTagAddress(config, "process", "nozzleTemp"),
            RunningAddress = adapter.GetTagAddress(config, "status", "isRunning"),
            AlarmAddress = adapter.GetTagAddress(config, "status", "isAlarm"),
            AlarmConfigFile = adapter.GetAlarmConfigFile(config),
            SensorConfigFile = adapter.GetSensorConfigFile(config),
            PrintHead1ConfigFile = printHeadConfigs.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = printHeadConfigs.ElementAtOrDefault(1) ?? string.Empty,
            TotalTrayAddress = adapter.GetDetailLabelAddress(config, "totalTray", "D3400"),
            CurrentTrayAddress = adapter.GetDetailLabelAddress(config, "currentTray", "D33"),
            TotalLayerAddress = adapter.GetDetailLabelAddress(config, "totalLayer", "D3401"),
            CurrentLayerAddress = adapter.GetDetailLabelAddress(config, "currentLayer", "D32"),
            SwitchGraphicLayerAddress = adapter.GetDetailLabelAddress(config, "switchGraphicLayer", "D510"),
            SwitchAreaLayerAddress = adapter.GetDetailLabelAddress(config, "switchAreaLayer", "D512"),
            MessageIdAddress = adapter.GetDetailLabelAddress(config, "messageId", "D85"),
            BatteryAddress = adapter.GetDetailLabelAddress(config, "battery", "D120"),
            ElapsedTimeAddress = adapter.GetDetailLabelAddress(config, "elapsedTime", "D86"),
            PrintHeadCountAddress = adapter.GetDetailLabelAddress(config, "printHeadCount", "D87")
        };
    }
}
