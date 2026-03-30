using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.DeviceFramework.ViewModels;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Pages;

/// <summary>
/// Generic dynamic device page driven entirely by DeviceContext Labels/Commands.
/// No app-side C# or XAML needed - just provide JSON Config.
/// </summary>
public partial class DynamicDevicePage : UserControl
{
    private readonly DevicePageViewModel _viewModel;
    private readonly ProcessCommandService _commandService = new();
    private readonly PlcDataEventMonitor _dataEventMonitor = new();

    /// <summary>
    /// 數據事件攔截器 — App 側可設定，當 PLC 數據事件觸發時呼叫。
    /// 參數：(eventName, address, oldVal, newVal)
    /// </summary>
    public Action<string, string, int, int>? DataEventInterceptor { get; set; }

    /// <summary>
    /// �R�O�d�I�� �X App �ݥi�]�w���e�����d�I�R�O����C
    /// �ѼơG(machineId, commandName, address)
    /// �^�� true  �� �~�����w�] PLC �g�J
    /// �^�� false �� ���L�w�] PLC �g�J�]�� App �ݦۦ�B�z�^
    /// </summary>
    public Func<string, string, string, bool>? CommandInterceptor { get; set; }

    public DynamicDevicePage()
    {
        _viewModel = new DevicePageViewModel();
        _viewModel.CommandExecuted += OnCommandExecuted;

        InitializeComponent();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Apply device context (called by AppController PageFactory).
    /// </summary>
    public void SetContext(DeviceContext context)
    {
        _viewModel.ApplyDeviceContext(context);
        ApplyLayout();

        _dataEventMonitor.Unsubscribe();
        if (context.DataEvents.Count > 0)
        {
            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            if (status != null)
                _dataEventMonitor.Subscribe(status, context.DataEvents);
        }
        _dataEventMonitor.EventTriggered = (name, addr, oldVal, newVal) =>
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(() => DataEventInterceptor?.Invoke(name, addr, oldVal, newVal));
            else
                DataEventInterceptor?.Invoke(name, addr, oldVal, newVal);
        };
    }

    private void ApplyLayout()
    {
        bool hasViewers = _viewModel.HasAnyViewer;
        bool isSplit = _viewModel.LayoutMode is "SplitRight" or "Dashboard";
        bool showRight = hasViewers && isSplit;

        ColSpacer.Width = showRight ? new System.Windows.GridLength(12) : new System.Windows.GridLength(0);
        ColRight.Width  = showRight ? new System.Windows.GridLength(0.9, System.Windows.GridUnitType.Star)
                                    : new System.Windows.GridLength(0);

        RightViewersPanel.Visibility = showRight ? Visibility.Visible : Visibility.Collapsed;

        // Adjust sensor row height within right panel
        if (showRight)
        {
            bool hasBoth = _viewModel.HasAlarmConfig && _viewModel.HasSensorConfig;
            RowAlarm.Height       = _viewModel.HasAlarmConfig  ? new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) : new System.Windows.GridLength(0);
            RowSensorSpacer.Height = hasBoth ? new System.Windows.GridLength(12) : new System.Windows.GridLength(0);
            RowSensor.Height      = _viewModel.HasSensorConfig ? new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) : new System.Windows.GridLength(0);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEvent;
        PlcEventContext.EventTriggered += OnPlcEvent;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEvent;
        _dataEventMonitor.Unsubscribe();
    }

    private void OnPlcEvent(object? sender, PlcEventTriggeredEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnPlcEvent(sender, e));
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
                    $"Process completed\n\nMachine: {_viewModel.MachineName}\nAddress: {e.Address}",
                    "Process Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;
            case ProcessEventNames.Alarm:
                _viewModel.MarkProcessFaulted();
                CyberMessageBox.Show(
                    $"Process alarm\n\nMachine: {_viewModel.MachineName}\nAddress: {e.Address}",
                    "Process Alarm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;
        }
    }

    private async void OnCommandExecuted(string commandName)
    {
        var cmd = _viewModel.Commands.FirstOrDefault(c =>
            string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase));

        if (cmd is null)
            return;

        // �p�G�]�w�F�d�I���A���I�s�d�I��
        if (CommandInterceptor is not null)
        {
            var shouldContinue = CommandInterceptor(_viewModel.MachineId, commandName, cmd.Address);
            if (!shouldContinue)
                return;
        }

        var result = await _commandService.ExecuteCommandAsync(
            _viewModel.MachineId,
            _viewModel.MachineName,
            commandName,
            cmd.Address);

        if (result.Success)
        {
            _viewModel.CurrentProcessState = result.State;
        }

        CyberMessageBox.Show(
            result.Message,
            result.Success ? $"{commandName} Sent" : $"{commandName} Failed",
            MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }
}
