using Stackdose.App.SingleDetailLab.Models;
using Stackdose.App.SingleDetailLab.Services;
using System.Windows.Controls;

namespace Stackdose.App.SingleDetailLab.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(LabMachineConfig config)
    {
        MachineSummaryText.Text = $"Machine: {config.Machine.Name} ({config.Machine.Id})";
        TopPlcStatus.IpAddress = config.Plc.Ip;
        TopPlcStatus.Port = config.Plc.Port;
        TopPlcStatus.ScanInterval = config.Plc.PollIntervalMs;
        TopPlcStatus.AutoConnect = config.Plc.AutoConnect;
        TopPlcStatus.MonitorAddress = LabMonitorAddressBuilder.Build(config);
    }
}
