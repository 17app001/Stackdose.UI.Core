using System.Windows.Controls;

namespace Stackdose.App.MySinglePage2.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(string machineName, string machineId, string plcIp, int plcPort, int scanIntervalMs, bool autoConnect, string monitorAddress)
    {
        MachineSummaryText.Text = $"Machine: {machineName} ({machineId})";
        TopPlcStatus.IpAddress = plcIp;
        TopPlcStatus.Port = plcPort;
        TopPlcStatus.ScanInterval = scanIntervalMs;
        TopPlcStatus.AutoConnect = autoConnect;
        TopPlcStatus.MonitorAddress = monitorAddress;
    }
}
