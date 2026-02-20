using System.Windows.Controls;
using Stackdose.App.UbiDemo.ViewModels;

namespace Stackdose.App.UbiDemo.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel();
    }
}
