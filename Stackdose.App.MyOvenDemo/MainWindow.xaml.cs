using Stackdose.App.MyOvenDemo.Pages;
using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.MyOvenDemo.Handlers;
using System.Windows;

namespace Stackdose.App.MyOvenDemo;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    private readonly CommandHandlers _handlers = new();

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.MyOvenDemo");
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
                return page;
            },
            (page, ctx) =>
            {
                if (page is DynamicDevicePage dp)
                    dp.SetContext(ctx);
            });

        Loaded += (_, _) => _controller.Start();
        Unloaded += (_, _) => _controller.Dispose();
    }
}
