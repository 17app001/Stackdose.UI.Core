using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.SimpleDemo.Handlers;
using System.Windows;

namespace Stackdose.App.SimpleDemo;

public partial class MainWindow : Window
{
    private readonly AppController _controller;
    private readonly CommandHandlers _handlers = new();

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.SimpleDemo");
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

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