using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using System.Windows;

namespace Stackdose.App.SimpleDemo;

/// <summary>
/// SimpleDemo - uses DynamicDevicePage, zero device-specific code.
/// Everything is driven by JSON Config files.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        // 1. RuntimeHost - only needs the project folder name to find Config/
        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.SimpleDemo");

        // 2. AppController
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

        // 3. Use DynamicDevicePage (framework built-in, no custom page needed)
        _controller.ConfigurePageFactory(
            ctx =>
            {
                var page = new DynamicDevicePage();
                page.SetContext(ctx);
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
