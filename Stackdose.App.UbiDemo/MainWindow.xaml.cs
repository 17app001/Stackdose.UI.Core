using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using System.Windows;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        var adapter = new UbiFrameworkMappingAdapter();
        var runtimeMapper = new UbiRuntimeMapper(adapter);
        var runtimeHost = new RuntimeHost(runtimeMapper, "Stackdose.App.UbiDemo");
        var settingsPage = new SettingsPage();

        _controller = new AppController(MainShell, Dispatcher, runtimeHost);
        _controller.SettingsPage = settingsPage;

        _controller.ConfigurePageFactory(
            ctx =>
            {
                var page = new UbiDevicePage();
                page.SetDeviceContext(UbiDeviceContextMapper.FromFrameworkContext(ctx));
                return page;
            },
            (page, ctx) =>
            {
                if (page is UbiDevicePage ubiPage)
                {
                    ubiPage.SetDeviceContext(UbiDeviceContextMapper.FromFrameworkContext(ctx));
                }
            });

        _controller.OnSettingsNavigating = (page, runtime, machineId) =>
        {
            if (page is SettingsPage sp)
            {
                sp.SetMonitorAddresses(runtime.OverviewPage.PlcMonitorAddresses);
                sp.SetMachines(runtime.Machines, runtime.ConfigDirectory, machineId);
            }
        };

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _controller.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _controller.Dispose();
    }
}
