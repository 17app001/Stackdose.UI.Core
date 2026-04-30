using System.Windows;
using Stackdose.Tools.MachinePageDesigner.ViewModels;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class MainWindow : Window
{
    private bool _isLightTheme = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void OnZoomInClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ZoomIn();
    private void OnZoomOutClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ZoomOut();
    private void OnZoomResetClick(object sender, RoutedEventArgs e) => Vm?.Canvas.ResetZoom();

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        _isLightTheme = !_isLightTheme;
        ThemeManager.SwitchTheme(_isLightTheme ? ThemeType.Light : ThemeType.Dark);
        btnThemeToggle.Content = _isLightTheme ? "🌙 Dark" : "☀ Light";
    }
}
