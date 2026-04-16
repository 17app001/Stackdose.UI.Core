using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // ── Page Tab Rename ───────────────────────────────────────────────────

    private void OnPageNameEditKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            if (sender is TextBox tb && tb.DataContext is PageTabViewModel vm)
                vm.IsEditing = false;
            e.Handled = true;
        }
    }

    private void OnPageNameEditLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is PageTabViewModel vm)
            vm.IsEditing = false;
    }
}
