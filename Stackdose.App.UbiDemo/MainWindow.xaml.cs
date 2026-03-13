using Stackdose.App.UbiDemo.Services;
using System.Windows;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private readonly UbiAppController _controller;

    public MainWindow()
    {
        InitializeComponent();
        _controller = new UbiAppController(MainShell, Dispatcher);
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
