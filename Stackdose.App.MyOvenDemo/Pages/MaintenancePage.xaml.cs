using Stackdose.UI.Core.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Stackdose.App.MyOvenDemo.Pages;

public partial class MaintenancePage : UserControl, INotifyPropertyChanged
{
    private string _selectedMachineId = string.Empty;

    public MaintenancePage()
    {
        InitializeComponent();
        DataContext = this;
        MachineIds.Add("OVEN1");
        MachineIds.Add("OVEN2");
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
        if (FindName("Panel_OVEN1") is Border bOVEN1)
            bOVEN1.Visibility = machineId == "OVEN1" ? Visibility.Visible : Visibility.Collapsed;
        if (FindName("Panel_OVEN2") is Border bOVEN2)
            bOVEN2.Visibility = machineId == "OVEN2" ? Visibility.Visible : Visibility.Collapsed;
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
