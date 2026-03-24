using Stackdose.App.DeviceFramework.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 將 MachineConfig 轉換為 DeviceContext — 通用邏輯，不含硬編碼。
/// </summary>
public static class DeviceContextMapper
{
    public static DeviceContext CreateDeviceContext(MachineConfig config, IRuntimeMappingAdapter adapter)
    {
        var runningAddress = !string.IsNullOrWhiteSpace(config.ProcessMonitor.IsRunning)
            ? config.ProcessMonitor.IsRunning
            : adapter.GetTagAddress(config, "status", "isRunning");

        var alarmAddress = !string.IsNullOrWhiteSpace(config.ProcessMonitor.IsAlarm)
            ? config.ProcessMonitor.IsAlarm
            : adapter.GetTagAddress(config, "status", "isAlarm");

        var completedAddress = !string.IsNullOrWhiteSpace(config.ProcessMonitor.IsCompleted)
            ? config.ProcessMonitor.IsCompleted
            : "--";

        var context = new DeviceContext
        {
            MachineId = config.Machine.Id,
            MachineName = config.Machine.Name,
            RunningAddress = runningAddress,
            CompletedAddress = completedAddress,
            AlarmAddress = alarmAddress,
            AlarmConfigFile = adapter.GetAlarmConfigFile(config),
            SensorConfigFile = adapter.GetSensorConfigFile(config),
            PrintHeadConfigFiles = [.. adapter.GetPrintHeadConfigFiles(config)],
            EnabledModules = [.. config.Modules]
        };

        // 動態命令
        foreach (var (name, address) in config.Commands)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                context.Commands[name] = address;
            }
        }

        // 動態標籤 — 從 DetailLabels 字典產生
        foreach (var (key, address) in config.DetailLabels)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                context.Labels[key] = new DeviceLabelInfo(address.Trim());
            }
        }

        return context;
    }
}
