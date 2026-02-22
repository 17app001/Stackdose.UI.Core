using Stackdose.App.UbiDemo.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.UbiDemo.Pages;

public partial class UbiDevicePage : UserControl
{
    public static readonly DependencyProperty MachineNameProperty =
        DependencyProperty.Register(nameof(MachineName), typeof(string), typeof(UbiDevicePage), new PropertyMetadata("Machine"));

    public static readonly DependencyProperty BatchAddressProperty =
        DependencyProperty.Register(nameof(BatchAddress), typeof(string), typeof(UbiDevicePage), new PropertyMetadata("--"));

    public static readonly DependencyProperty RecipeAddressProperty =
        DependencyProperty.Register(nameof(RecipeAddress), typeof(string), typeof(UbiDevicePage), new PropertyMetadata("--"));

    public static readonly DependencyProperty NozzleAddressProperty =
        DependencyProperty.Register(nameof(NozzleAddress), typeof(string), typeof(UbiDevicePage), new PropertyMetadata("--"));

    public static readonly DependencyProperty AlarmAddressProperty =
        DependencyProperty.Register(nameof(AlarmAddress), typeof(string), typeof(UbiDevicePage), new PropertyMetadata("--"));

    public static readonly DependencyProperty AlarmConfigFileProperty =
        DependencyProperty.Register(nameof(AlarmConfigFile), typeof(string), typeof(UbiDevicePage), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SensorConfigFileProperty =
        DependencyProperty.Register(nameof(SensorConfigFile), typeof(string), typeof(UbiDevicePage), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrintHead1ConfigFileProperty =
        DependencyProperty.Register(nameof(PrintHead1ConfigFile), typeof(string), typeof(UbiDevicePage), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrintHead2ConfigFileProperty =
        DependencyProperty.Register(nameof(PrintHead2ConfigFile), typeof(string), typeof(UbiDevicePage), new PropertyMetadata(string.Empty));

    public string MachineName
    {
        get => (string)GetValue(MachineNameProperty);
        set => SetValue(MachineNameProperty, value);
    }

    public string BatchAddress
    {
        get => (string)GetValue(BatchAddressProperty);
        set => SetValue(BatchAddressProperty, value);
    }

    public string RecipeAddress
    {
        get => (string)GetValue(RecipeAddressProperty);
        set => SetValue(RecipeAddressProperty, value);
    }

    public string NozzleAddress
    {
        get => (string)GetValue(NozzleAddressProperty);
        set => SetValue(NozzleAddressProperty, value);
    }

    public string AlarmAddress
    {
        get => (string)GetValue(AlarmAddressProperty);
        set => SetValue(AlarmAddressProperty, value);
    }

    public string AlarmConfigFile
    {
        get => (string)GetValue(AlarmConfigFileProperty);
        set => SetValue(AlarmConfigFileProperty, value);
    }

    public string SensorConfigFile
    {
        get => (string)GetValue(SensorConfigFileProperty);
        set => SetValue(SensorConfigFileProperty, value);
    }

    public string PrintHead1ConfigFile
    {
        get => (string)GetValue(PrintHead1ConfigFileProperty);
        set => SetValue(PrintHead1ConfigFileProperty, value);
    }

    public string PrintHead2ConfigFile
    {
        get => (string)GetValue(PrintHead2ConfigFileProperty);
        set => SetValue(PrintHead2ConfigFileProperty, value);
    }

    public ObservableCollection<string> CommandKeys { get; } =
    [
        "DeviceInitialization",
        "CleanSurface",
        "ReadAttribute",
        "AsyncTime"
    ];

    public string SelectedCommandKey { get; set; } = "DeviceInitialization";
    public string CommandParameter { get; set; } = "0";

    public UbiDevicePage()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void SetDeviceContext(DeviceContext context)
    {
        MachineName = context.MachineName;
        BatchAddress = context.BatchAddress;
        RecipeAddress = context.RecipeAddress;
        NozzleAddress = context.NozzleAddress;
        AlarmAddress = context.AlarmAddress;
        AlarmConfigFile = context.AlarmConfigFile;
        SensorConfigFile = context.SensorConfigFile;
        PrintHead1ConfigFile = context.PrintHead1ConfigFile;
        PrintHead2ConfigFile = context.PrintHead2ConfigFile;
    }
}
