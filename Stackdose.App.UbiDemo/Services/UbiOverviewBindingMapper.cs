using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiOverviewBindingMapper
{
    public static void BindOverview(MachineOverviewPage page, IReadOnlyList<UbiMachineConfig> configs, string monitorAddresses)
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
        page.PlcMonitorAddresses = monitorAddresses;
        page.MachineCards = [.. configs.Select(UbiMonitorAddressBuilder.CreateCard)];
    }
}
