using System.Windows;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnZoomInClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ZoomIn();
    private void OnZoomOutClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ZoomOut();
    private void OnZoomResetClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ResetZoom();
}
