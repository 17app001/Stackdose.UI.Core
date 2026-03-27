using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using System.Windows;

namespace Stackdose.App.SinglePageDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var host = new RuntimeHost(projectFolderName: "Stackdose.App.SinglePageDemo");
        var (configs, _) = host.LoadConfigs();
        if (configs.Count == 0) return;

        var mapper = new RuntimeMapper();
        var ctx = mapper.CreateDeviceContext(configs[0]);
        var page = new DynamicDevicePage();
        page.SetContext(ctx);
        DeviceContent.Content = page;
    }
}