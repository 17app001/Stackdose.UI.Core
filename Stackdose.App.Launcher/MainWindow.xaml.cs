using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using System.Windows;

namespace Stackdose.App.Launcher;

/// <summary>
/// Generic launcher - no device-specific code.
/// Everything is driven by JSON Config + DynamicDevicePage.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.Launcher");

        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

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
