using Stackdose.UI.Core.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.App.Monitor.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public event EventHandler? SecuredSampleButtonClicked;

    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(
        string machineName,
        string machineId,
        string plcIp,
        int plcPort,
        int scanIntervalMs,
        bool autoConnect,
        string monitorAddress,
        string sensorConfigPath,
        string alarmConfigPath)
    {
        MachineSummaryText.Text = $"Machine: {machineName} ({machineId})";
        TopPlcStatus.IpAddress = plcIp;
        TopPlcStatus.Port = plcPort;
        TopPlcStatus.ScanInterval = scanIntervalMs;
        TopPlcStatus.AutoConnect = autoConnect;
        TopPlcStatus.MonitorAddress = monitorAddress;

        BindViewerConfigs(sensorConfigPath, alarmConfigPath);
    }

    private void BindViewerConfigs(string sensorConfigPath, string alarmConfigPath)
    {
        foreach (var viewer in FindVisualChildren<SensorViewer>(this))
        {
            if (string.IsNullOrWhiteSpace(viewer.ConfigFile))
            {
                viewer.ConfigFile = sensorConfigPath;
            }
        }

        foreach (var viewer in FindVisualChildren<AlarmViewer>(this))
        {
            if (string.IsNullOrWhiteSpace(viewer.ConfigFile))
            {
                viewer.ConfigFile = alarmConfigPath;
            }

            if (viewer.TargetStatus == null)
            {
                viewer.TargetStatus = TopPlcStatus;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
        {
            yield break;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void OnSecuredSampleButtonClick(object sender, RoutedEventArgs e)
    {
        SecuredSampleButtonClicked?.Invoke(this, EventArgs.Empty);
    }
}
