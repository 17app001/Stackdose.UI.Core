using Stackdose.UI.Core.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Stackdose.App.MyDevice.Pages;

public partial class MaintenancePage : UserControl, INotifyPropertyChanged
{
    private string _selectedMachineId = string.Empty;

    public MaintenancePage()
    {
        InitializeComponent();
        DataContext = this;
        MachineIds.Add("M1");
        if (MachineIds.Count > 0) SelectedMachineId = MachineIds[0];
    }

    public ObservableCollection<string> MachineIds { get; } = [];
    public string SelectedMachineId
    {
        get => _selectedMachineId;
        set { _selectedMachineId = value; N(); ShowMachinePanel(value); }
    }

    private void ShowMachinePanel(string machineId)
    {
        if (FindName("Panel_M1") is Border bM1)
            bM1.Visibility = machineId == "M1" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnMachineChanged(object sender, SelectionChangedEventArgs e) { }

    private async void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string tagStr) return;
        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected) return;
        await manager.WriteAsync(tagStr);
    }

    private async void OnMomentaryDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string addr) return;
        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected) return;
        await manager.WriteAsync($"{addr},1");
    }

    private async void OnMomentaryUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string addr) return;
        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected) return;
        await manager.WriteAsync($"{addr},0");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
