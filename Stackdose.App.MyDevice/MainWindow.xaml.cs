using Stackdose.App.MyDevice.Pages;
using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.MyDevice.Handlers;
using System.Windows;

namespace Stackdose.App.MyDevice;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    private readonly CommandHandlers _handlers = new();
    private readonly DataEventHandlers _dataEventHandlers = new();

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.MyDevice");
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

        var settingsPage = new SettingsPage();
        _controller.SettingsPage = settingsPage;
        _controller.OnSettingsNavigating = (page, runtime, machineId) =>
        {
            if (page is SettingsPage sp)
            {
                sp.SetMonitorAddresses(runtime.OverviewPage.PlcMonitorAddresses);
                sp.SetMachines(runtime.Machines, runtime.ConfigDirectory, machineId);
            }
        };

        _controller.ConfigurePageFactory(
            ctx =>
            {
                var page = new DynamicDevicePage();
                page.SetContext(ctx);
                page.CommandInterceptor = (machineId, commandName, address) =>
                    _handlers.HandleCommand(machineId, commandName, address);
                page.DataEventInterceptor = (name, addr, oldVal, newVal) =>
                    _dataEventHandlers.HandleEvent(name, addr, oldVal, newVal);
                return page;
            },
            (page, ctx) =>
            {
                if (page is DynamicDevicePage dp)
                {
                    dp.SetContext(ctx);
                    dp.DataEventInterceptor = (name, addr, oldVal, newVal) =>
                        _dataEventHandlers.HandleEvent(name, addr, oldVal, newVal);
                }
            });

        Loaded += (_, _) => _controller.Start();
        Unloaded += (_, _) => _controller.Dispose();
    }
}
