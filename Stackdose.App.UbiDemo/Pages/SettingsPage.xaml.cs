using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.App.UbiDemo.ViewModels;

namespace Stackdose.App.UbiDemo.Pages;

public partial class SettingsPage : UserControl
{
    private readonly SettingsPageViewModel _viewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void SetMonitorAddresses(string monitorAddresses)
    {
        _viewModel.ApplyMonitorAddresses(monitorAddresses);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var status = PlcContext.GlobalStatus;
        if (status == null)
        {
            return;
        }

        BindLabelToStatus("LblPlcReady", status);
        BindLabelToStatus("LblRunning", status);
        BindLabelToStatus("LblAlarm", status);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnbindLabel("LblPlcReady");
        UnbindLabel("LblRunning");
        UnbindLabel("LblAlarm");
    }

    private void BindLabelToStatus(string labelName, PlcStatus status)
    {
        if (FindName(labelName) is PlcLabel label)
        {
            label.TargetStatus = status;
        }
    }

    private void UnbindLabel(string labelName)
    {
        if (FindName(labelName) is PlcLabel label)
        {
            label.TargetStatus = null;
        }
    }
}
