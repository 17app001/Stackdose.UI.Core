using Stackdose.App.Demo.Services;
using System.Windows;

namespace Stackdose.App.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DemoRuntimeHost.Start(MainShell);
    }
}
