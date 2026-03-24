using Stackdose.App.DeviceFramework.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 將 MachineConfig 轉換為 DeviceContext — 通用映射，不含硬編碼。
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

        // 動態標籤 — 從 DetailLabels 字典匯入
        foreach (var (key, address) in config.DetailLabels)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                context.Labels[key] = new DeviceLabelInfo(address.Trim());
            }
        }

        // 動態標籤 — 從 Tags (status + process) 自動匯入可讀的 tag
        // 這樣 App 端不需要 override RuntimeMapper 就能拿到 batchNo、recipeNo 等
        ImportTagsToLabels(context, config.Tags.Status);
        ImportTagsToLabels(context, config.Tags.Process);

        return context;
    }

    private static void ImportTagsToLabels(DeviceContext context, Dictionary<string, TagConfig> tags)
    {
        foreach (var (key, tag) in tags)
        {
            // 跳過已存在的（DetailLabels 優先）和非可讀的
            if (context.Labels.ContainsKey(key))
                continue;

            if (!string.IsNullOrWhiteSpace(tag.Access)
                && !tag.Access.Equals("read", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(tag.Address))
                continue;

            context.Labels[key] = new DeviceLabelInfo(
                address: tag.Address,
                dataType: string.IsNullOrWhiteSpace(tag.Type) ? "Word" : tag.Type);
        }
    }
}
