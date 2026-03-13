using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Services;
using System.Collections.ObjectModel;

namespace Stackdose.App.UbiDemo.ViewModels;

public sealed class UbiDevicePageViewModel : ViewModelBase
{
    private readonly UbiMachineCommandService _machineCommandService;
    private string _machineId = string.Empty;
    private string _machineName = "Machine";
    private string _batchAddress = "--";
    private string _recipeAddress = "--";
    private string _nozzleAddress = "--";
    private string _alarmAddress = "--";
    private string _alarmConfigFile = string.Empty;
    private string _sensorConfigFile = string.Empty;
    private string _printHead1ConfigFile = string.Empty;
    private string _printHead2ConfigFile = string.Empty;
    private string _totalTrayAddress = "D3400";
    private string _currentTrayAddress = "D33";
    private string _totalLayerAddress = "D3401";
    private string _currentLayerAddress = "D32";
    private string _switchGraphicLayerAddress = "D510";
    private string _switchAreaLayerAddress = "D512";
    private string _messageIdAddress = "D85";
    private string _batteryAddress = "D120";
    private string _elapsedTimeAddress = "D86";
    private string _printHeadCountAddress = "D87";
    private string _selectedCommandKey = "DeviceInitialization";
    private string _commandParameter = "0";
    private string _spitParameter = "0.1,1,1,1";
    private string _printHeadEditorAddress = "D120";
    private string _printHeadEditorValue = "0";
    private string _printHeadEditorReason = "PrintHead manual operation";

    public UbiDevicePageViewModel()
        : this(new UbiMachineCommandService())
    {
    }

    internal UbiDevicePageViewModel(UbiMachineCommandService machineCommandService)
    {
        _machineCommandService = machineCommandService;
    }

    public string MachineId
    {
        get => _machineId;
        set => SetProperty(ref _machineId, value);
    }

    public string MachineName
    {
        get => _machineName;
        set => SetProperty(ref _machineName, value);
    }

    public string BatchAddress
    {
        get => _batchAddress;
        set => SetProperty(ref _batchAddress, value);
    }

    public string RecipeAddress
    {
        get => _recipeAddress;
        set => SetProperty(ref _recipeAddress, value);
    }

    public string NozzleAddress
    {
        get => _nozzleAddress;
        set => SetProperty(ref _nozzleAddress, value);
    }

    public string AlarmAddress
    {
        get => _alarmAddress;
        set => SetProperty(ref _alarmAddress, value);
    }

    public string AlarmConfigFile
    {
        get => _alarmConfigFile;
        set => SetProperty(ref _alarmConfigFile, value);
    }

    public string SensorConfigFile
    {
        get => _sensorConfigFile;
        set => SetProperty(ref _sensorConfigFile, value);
    }

    public string PrintHead1ConfigFile
    {
        get => _printHead1ConfigFile;
        set => SetProperty(ref _printHead1ConfigFile, value);
    }

    public string PrintHead2ConfigFile
    {
        get => _printHead2ConfigFile;
        set => SetProperty(ref _printHead2ConfigFile, value);
    }

    public string TotalTrayAddress
    {
        get => _totalTrayAddress;
        set => SetProperty(ref _totalTrayAddress, value);
    }

    public string CurrentTrayAddress
    {
        get => _currentTrayAddress;
        set => SetProperty(ref _currentTrayAddress, value);
    }

    public string TotalLayerAddress
    {
        get => _totalLayerAddress;
        set => SetProperty(ref _totalLayerAddress, value);
    }

    public string CurrentLayerAddress
    {
        get => _currentLayerAddress;
        set => SetProperty(ref _currentLayerAddress, value);
    }

    public string SwitchGraphicLayerAddress
    {
        get => _switchGraphicLayerAddress;
        set => SetProperty(ref _switchGraphicLayerAddress, value);
    }

    public string SwitchAreaLayerAddress
    {
        get => _switchAreaLayerAddress;
        set => SetProperty(ref _switchAreaLayerAddress, value);
    }

    public string MessageIdAddress
    {
        get => _messageIdAddress;
        set => SetProperty(ref _messageIdAddress, value);
    }

    public string BatteryAddress
    {
        get => _batteryAddress;
        set => SetProperty(ref _batteryAddress, value);
    }

    public string ElapsedTimeAddress
    {
        get => _elapsedTimeAddress;
        set => SetProperty(ref _elapsedTimeAddress, value);
    }

    public string PrintHeadCountAddress
    {
        get => _printHeadCountAddress;
        set => SetProperty(ref _printHeadCountAddress, value);
    }

    public ObservableCollection<string> CommandKeys { get; } =
    [
        "DeviceInitialization",
        "CleanSurface",
        "ReadAttribute",
        "AsyncTime"
    ];

    public string SelectedCommandKey
    {
        get => _selectedCommandKey;
        set => SetProperty(ref _selectedCommandKey, value);
    }

    public string CommandParameter
    {
        get => _commandParameter;
        set => SetProperty(ref _commandParameter, value);
    }

    public string SpitParameter
    {
        get => _spitParameter;
        set => SetProperty(ref _spitParameter, value);
    }

    public string PrintHeadEditorAddress
    {
        get => _printHeadEditorAddress;
        set => SetProperty(ref _printHeadEditorAddress, value);
    }

    public string PrintHeadEditorValue
    {
        get => _printHeadEditorValue;
        set => SetProperty(ref _printHeadEditorValue, value);
    }

    public string PrintHeadEditorReason
    {
        get => _printHeadEditorReason;
        set => SetProperty(ref _printHeadEditorReason, value);
    }

    public string BuildStartClickMessage()
    {
        var request = new UbiMachineCommandRequest(
            MachineId,
            MachineName,
            SelectedCommandKey,
            CommandParameter);

        return _machineCommandService.BuildStartClickMessage(request);
    }

    public void ApplyDeviceContext(DeviceContext context)
    {
        MachineId = context.MachineId;
        MachineName = context.MachineName;
        BatchAddress = context.BatchAddress;
        RecipeAddress = context.RecipeAddress;
        NozzleAddress = context.NozzleAddress;
        AlarmAddress = context.AlarmAddress;
        AlarmConfigFile = context.AlarmConfigFile;
        SensorConfigFile = context.SensorConfigFile;
        PrintHead1ConfigFile = context.PrintHead1ConfigFile;
        PrintHead2ConfigFile = context.PrintHead2ConfigFile;
        TotalTrayAddress = context.TotalTrayAddress;
        CurrentTrayAddress = context.CurrentTrayAddress;
        TotalLayerAddress = context.TotalLayerAddress;
        CurrentLayerAddress = context.CurrentLayerAddress;
        SwitchGraphicLayerAddress = context.SwitchGraphicLayerAddress;
        SwitchAreaLayerAddress = context.SwitchAreaLayerAddress;
        MessageIdAddress = context.MessageIdAddress;
        BatteryAddress = context.BatteryAddress;
        ElapsedTimeAddress = context.ElapsedTimeAddress;
        PrintHeadCountAddress = context.PrintHeadCountAddress;
    }
}
