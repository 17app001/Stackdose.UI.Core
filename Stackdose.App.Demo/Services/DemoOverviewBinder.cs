using Stackdose.App.Demo.Models;
using Stackdose.UI.Templates.Pages;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.App.Demo.Services;

public static class DemoOverviewBinder
{
    public static void ApplyMeta(MachineOverviewPage page, DemoAppMeta meta)
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

    public static void Bind(MachineOverviewPage page, IReadOnlyList<DemoMachineConfig> configs)
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
        page.PlcMonitorAddresses = DemoMonitorAddressBuilder.Build(configs);
        page.MachineCards = [.. configs.Select(CreateCard)];
    }

    private static MachineOverviewCard CreateCard(DemoMachineConfig config)
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
            LeftBottomValue = "Normal",
            RightTopLabel = "Nozzle",
            RightTopValue = "--",
            RightBottomLabel = "Mode",
            RightBottomValue = "Manual"
        };
    }
}
