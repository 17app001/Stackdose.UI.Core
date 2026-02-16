using Stackdose.Abstractions.Hardware;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Templates.Pages;

public partial class MachineOverviewPage : UserControl
{
    public event Action<IPlcManager>? PlcScanUpdated;
    public event Action<string>? MachineSelected;

    public static readonly DependencyProperty PlcIpAddressProperty =
        DependencyProperty.Register(
            nameof(PlcIpAddress),
            typeof(string),
            typeof(MachineOverviewPage),
            new PropertyMetadata("127.0.0.1"));

    public static readonly DependencyProperty PlcPortProperty =
        DependencyProperty.Register(
            nameof(PlcPort),
            typeof(int),
            typeof(MachineOverviewPage),
            new PropertyMetadata(5000));

    public static readonly DependencyProperty PlcScanIntervalProperty =
        DependencyProperty.Register(
            nameof(PlcScanInterval),
            typeof(int),
            typeof(MachineOverviewPage),
            new PropertyMetadata(300));

    public static readonly DependencyProperty PlcAutoConnectProperty =
        DependencyProperty.Register(
            nameof(PlcAutoConnect),
            typeof(bool),
            typeof(MachineOverviewPage),
            new PropertyMetadata(true));

    public static readonly DependencyProperty PlcMonitorAddressesProperty =
        DependencyProperty.Register(
            nameof(PlcMonitorAddresses),
            typeof(string),
            typeof(MachineOverviewPage),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MachineCardsProperty =
        DependencyProperty.Register(
            nameof(MachineCards),
            typeof(ObservableCollection<MachineOverviewCard>),
            typeof(MachineOverviewPage),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShowMachineCardsProperty =
        DependencyProperty.Register(
            nameof(ShowMachineCards),
            typeof(bool),
            typeof(MachineOverviewPage),
            new PropertyMetadata(true));

    public static readonly DependencyProperty BottomPanelHeightProperty =
        DependencyProperty.Register(
            nameof(BottomPanelHeight),
            typeof(GridLength),
            typeof(MachineOverviewPage),
            new PropertyMetadata(new GridLength(320)));

    public static readonly DependencyProperty ShowSoftwareInfoProperty =
        DependencyProperty.Register(
            nameof(ShowSoftwareInfo),
            typeof(bool),
            typeof(MachineOverviewPage),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowLiveLogProperty =
        DependencyProperty.Register(
            nameof(ShowLiveLog),
            typeof(bool),
            typeof(MachineOverviewPage),
            new PropertyMetadata(true));

    public static readonly DependencyProperty BottomLeftTitleProperty =
        DependencyProperty.Register(
            nameof(BottomLeftTitle),
            typeof(string),
            typeof(MachineOverviewPage),
            new PropertyMetadata("Software Information"));

    public static readonly DependencyProperty BottomRightTitleProperty =
        DependencyProperty.Register(
            nameof(BottomRightTitle),
            typeof(string),
            typeof(MachineOverviewPage),
            new PropertyMetadata("Live Log"));

    public static readonly DependencyProperty SoftwareInfoItemsProperty =
        DependencyProperty.Register(
            nameof(SoftwareInfoItems),
            typeof(ObservableCollection<OverviewInfoItem>),
            typeof(MachineOverviewPage),
            new PropertyMetadata(null));

    public string PlcIpAddress
    {
        get => (string)GetValue(PlcIpAddressProperty);
        set => SetValue(PlcIpAddressProperty, value);
    }

    public int PlcPort
    {
        get => (int)GetValue(PlcPortProperty);
        set => SetValue(PlcPortProperty, value);
    }

    public int PlcScanInterval
    {
        get => (int)GetValue(PlcScanIntervalProperty);
        set => SetValue(PlcScanIntervalProperty, value);
    }

    public bool PlcAutoConnect
    {
        get => (bool)GetValue(PlcAutoConnectProperty);
        set => SetValue(PlcAutoConnectProperty, value);
    }

    public string PlcMonitorAddresses
    {
        get => (string)GetValue(PlcMonitorAddressesProperty);
        set => SetValue(PlcMonitorAddressesProperty, value);
    }

    public ObservableCollection<MachineOverviewCard> MachineCards
    {
        get => (ObservableCollection<MachineOverviewCard>)GetValue(MachineCardsProperty);
        set => SetValue(MachineCardsProperty, value);
    }

    public bool ShowMachineCards
    {
        get => (bool)GetValue(ShowMachineCardsProperty);
        set => SetValue(ShowMachineCardsProperty, value);
    }

    public GridLength BottomPanelHeight
    {
        get => (GridLength)GetValue(BottomPanelHeightProperty);
        set => SetValue(BottomPanelHeightProperty, value);
    }

    public bool ShowSoftwareInfo
    {
        get => (bool)GetValue(ShowSoftwareInfoProperty);
        set => SetValue(ShowSoftwareInfoProperty, value);
    }

    public bool ShowLiveLog
    {
        get => (bool)GetValue(ShowLiveLogProperty);
        set => SetValue(ShowLiveLogProperty, value);
    }

    public string BottomLeftTitle
    {
        get => (string)GetValue(BottomLeftTitleProperty);
        set => SetValue(BottomLeftTitleProperty, value);
    }

    public string BottomRightTitle
    {
        get => (string)GetValue(BottomRightTitleProperty);
        set => SetValue(BottomRightTitleProperty, value);
    }

    public ObservableCollection<OverviewInfoItem> SoftwareInfoItems
    {
        get => (ObservableCollection<OverviewInfoItem>)GetValue(SoftwareInfoItemsProperty);
        set => SetValue(SoftwareInfoItemsProperty, value);
    }

    public ICommand SelectMachineCommand { get; }

    public MachineOverviewPage()
    {
        InitializeComponent();

        SelectMachineCommand = new DelegateCommand(param =>
        {
            if (param is string machineId && !string.IsNullOrWhiteSpace(machineId))
            {
                MachineSelected?.Invoke(machineId);
            }
        });

        PlcConnectionStatus.ScanUpdated += OnPlcScanUpdated;

        MachineCards = [];
        SoftwareInfoItems =
        [
            new OverviewInfoItem("Application", "Stackdose.App.Demo"),
            new OverviewInfoItem("Version", "v0.9.0-demo"),
            new OverviewInfoItem("Build", "2026.02.16.1"),
            new OverviewInfoItem("Runtime", ".NET Windows"),
            new OverviewInfoItem("PLC Driver", "Mitsubishi MC Protocol")
        ];
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        PlcScanUpdated?.Invoke(manager);
    }
}

public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public sealed class MachineOverviewCard
{
    public string MachineId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BatchValue { get; set; } = string.Empty;
    public string RecipeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusBrush { get; set; } = Brushes.Gray;
    public string LeftTopLabel { get; set; } = string.Empty;
    public string LeftTopValue { get; set; } = string.Empty;
    public string LeftBottomLabel { get; set; } = string.Empty;
    public string LeftBottomValue { get; set; } = string.Empty;
    public string RightTopLabel { get; set; } = string.Empty;
    public string RightTopValue { get; set; } = string.Empty;
    public string RightBottomLabel { get; set; } = string.Empty;
    public string RightBottomValue { get; set; } = string.Empty;
}

public sealed class OverviewInfoItem
{
    public OverviewInfoItem(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }
}
