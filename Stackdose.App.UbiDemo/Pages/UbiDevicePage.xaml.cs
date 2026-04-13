using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.ViewModels;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.UbiDemo.Pages;

public partial class UbiDevicePage : UserControl
{
    private readonly UbiDevicePageViewModel _viewModel = new();

    public UbiDevicePage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += UbiDevicePage_Loaded;
        Unloaded += UbiDevicePage_Unloaded;
    }

    public void SetDeviceContext(UbiDeviceContext context)
    {
        _viewModel.ApplyDeviceContext(context);
    }

    private void UbiDevicePage_Loaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
        PlcEventContext.EventTriggered += OnPlcEventTriggered;
    }

    private void UbiDevicePage_Unloaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
    }

    private void OnPlcEventTriggered(object? sender, PlcEventTriggeredEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnPlcEventTriggered(sender, e));
            return;
        }

        switch (e.EventName)
        {
            case ProcessEventNames.Running:
                _viewModel.MarkProcessRunning();
                break;

            case ProcessEventNames.Completed:
                _viewModel.MarkProcessCompleted();
                CyberMessageBox.Show(
                    $"製程完成\n\nMachine: {_viewModel.MachineName}\nAddress: {e.Address}",
                    "Process Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;

            case ProcessEventNames.Alarm:
                _viewModel.MarkProcessFaulted();
                CyberMessageBox.Show(
                    $"製程警報\n\nMachine: {_viewModel.MachineName}\nAddress: {e.Address}",
                    "Process Alarm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;
        }
    }
}
