using Stackdose.App.ShellShared.Models;
using Stackdose.UI.Templates.Pages;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.App.ShellShared.Services;

public static class ShellOverviewBinder
{
    public static void ApplyMeta(MachineOverviewPage page, ShellAppMeta meta)
    {
        page.ShowMachineCards = meta.ShowMachineCards;
        page.ShowSoftwareInfo = meta.ShowSoftwareInfo;
        page.ShowLiveLog = meta.ShowLiveLog;
        page.BottomPanelHeight = new GridLength(meta.BottomPanelHeight);
        page.BottomLeftTitle = meta.BottomLeftTitle;
        page.BottomRightTitle = meta.BottomRightTitle;

        page.SoftwareInfoItems =
        [
            .. meta.SoftwareInfoItems.Select(item => new OverviewInfoItem(item.Label, item.Value))
        ];
    }

    public static void Bind(MachineOverviewPage page, IReadOnlyList<ShellMachineConfig> configs)
    {
        if (configs.Count == 0)
        {
            page.MachineCards = [];
            page.PlcMonitorAddresses = string.Empty;
            return;
        }

        var first = configs[0];
        page.PlcIpAddress = first.Plc.Ip;
        page.PlcPort = first.Plc.Port;
        page.PlcScanInterval = first.Plc.PollIntervalMs;
        page.PlcAutoConnect = first.Plc.AutoConnect;
        page.PlcMonitorAddresses = ShellMonitorAddressBuilder.Build(configs);
        page.MachineCards = [.. configs.Select(CreateCard)];
    }

    public static string GetTagAddress(ShellMachineConfig config, string section, string key)
    {
        Dictionary<string, ShellTagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status" => config.Tags.Status,
            "process" => config.Tags.Process,
            _ => null
        };

        if (tags is null || !tags.TryGetValue(key, out var tag) || string.IsNullOrWhiteSpace(tag.Address))
        {
            return "--";
        }

        return tag.Address;
    }

    private static MachineOverviewCard CreateCard(ShellMachineConfig config)
    {
        return new MachineOverviewCard
        {
            MachineId = config.Machine.Id,
            Title = config.Machine.Name,
            BatchValue = "--",
            RecipeText = "--",
            StatusText = "Idle",
            StatusBrush = Brushes.SlateGray,
            LeftTopLabel = "Heartbeat",
            LeftTopValue = "0",
            LeftBottomLabel = "Alarm",
            LeftBottomValue = "0",
            RightTopLabel = "Nozzle",
            RightTopValue = "--",
            RightBottomLabel = "Mode",
            RightBottomValue = "Manual"
        };
    }
}
