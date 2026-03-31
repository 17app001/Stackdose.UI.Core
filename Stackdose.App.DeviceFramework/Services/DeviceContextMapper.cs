using Stackdose.App.DeviceFramework.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// �N MachineConfig �ഫ�� DeviceContext �X �q�άM�g�A���t�w�s�X�C
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
            ShowPlcEditor = config.ShowPlcEditor,
            ShowLiveLog = config.ShowLiveLog,
            LayoutMode = config.LayoutMode,
            RightColumnWidthStar = config.RightColumnWidthStar,
            LeftCommandWidthPx   = config.LeftCommandWidthPx > 0 ? config.LeftCommandWidthPx : 250,
            LiveDataTitle = config.LiveDataTitle,
            DeviceStatusTitle = config.DeviceStatusTitle,
            EnabledModules = [.. config.Modules],
            DataEvents = [.. config.DataEvents],
        };

        // �ʺA�R�O
        foreach (var (name, address) in config.Commands)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                context.Commands[name] = address;
            }
        }

        // 動態標籤 — 從 DetailLabels 字串字典寫入
        foreach (var (key, address) in config.DetailLabels)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                var info = new DeviceLabelInfo(address.Trim());
                if (config.DetailLabelStyles.TryGetValue(key, out var style))
                {
                    info.FrameShape      = style.FrameShape;
                    info.ValueColorTheme = style.ValueColorTheme;
                }
                context.Labels[key] = info;
            }
        }

        // DeviceStatus 標籤
        foreach (var (key, address) in config.DetailStatusLabels)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                var info = new DeviceLabelInfo(address.Trim());
                if (config.StatusLabelStyles.TryGetValue(key, out var style))
                {
                    info.FrameShape      = style.FrameShape;
                    info.ValueColorTheme = style.ValueColorTheme;
                }
                context.StatusLabels[key] = info;
            }
        }

        // 指令主題覆寫
        foreach (var (name, cmdStyle) in config.CommandStyles)
            context.CommandThemes[name] = cmdStyle.Theme;

        // �ʺA���� �X �q Tags (status + process) �۰ʶפJ�iŪ�� tag
        // �o�� App �ݤ��ݭn override RuntimeMapper �N�ள�� batchNo�BrecipeNo ��
        ImportTagsToLabels(context, config.Tags.Status);
        ImportTagsToLabels(context, config.Tags.Process);

        return context;
    }

    private static void ImportTagsToLabels(DeviceContext context, Dictionary<string, TagConfig> tags)
    {
        foreach (var (key, tag) in tags)
        {
            // ���L�w�s�b���]DetailLabels �u���^�M�D�iŪ��
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
