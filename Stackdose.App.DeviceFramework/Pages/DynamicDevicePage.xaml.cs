using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.DeviceFramework.ViewModels;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
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

    /// <summary>
    /// 命令攔截器 — App 端可設定此委派來攔截命令執行。
    /// 參數：(machineId, commandName, address)
    /// 回傳 true  → 繼續執行預設 PLC 寫入
    /// 回傳 false → 跳過預設 PLC 寫入（由 App 端自行處理）
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
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEvent;
        PlcEventContext.EventTriggered += OnPlcEvent;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEvent;
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

        // 如果設定了攔截器，先呼叫攔截器
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
