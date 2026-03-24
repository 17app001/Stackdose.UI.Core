using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// Overview 綁定 — 將 Machine Config 清單綁定到 MachineOverviewPage。
/// </summary>
public static class OverviewBindingMapper
{
    public static void BindOverview(MachineOverviewPage page, IReadOnlyList<MachineConfig> configs, string monitorAddresses)
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
        page.MachineCards = [.. configs.Select(MonitorAddressBuilder.CreateCard)];
    }
}
