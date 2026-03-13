using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.ViewModels;
using Stackdose.UI.Core.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.UbiDemo.Pages;

public partial class UbiDevicePage : UserControl
{
    private readonly UbiDevicePageViewModel _viewModel = new();

    public UbiDevicePage()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void SetDeviceContext(DeviceContext context)
    {
        _viewModel.ApplyDeviceContext(context);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        CyberMessageBox.Show(
            _viewModel.BuildStartClickMessage(),
            "Start Clicked",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
