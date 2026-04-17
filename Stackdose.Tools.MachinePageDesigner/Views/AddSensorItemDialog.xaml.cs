using System.Windows;
using System.Windows.Controls;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class AddSensorItemDialog : Window
{
    public SensorEditItem? Result { get; private set; }

    public AddSensorItemDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => TxtGroup.Focus();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtDevice.Text))
        {
            MessageBox.Show("PLC 位址不可為空", "必填", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtDevice.Focus();
            return;
        }

        var mode = (CmbMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "AND";

        Result = new SensorEditItem
        {
            Group                = TxtGroup.Text.Trim(),
            Device               = TxtDevice.Text.Trim(),
            Bit                  = TxtBit.Text.Trim(),
            Value                = TxtValue.Text.Trim(),
            Mode                 = mode,
            OperationDescription = TxtDescription.Text.Trim(),
        };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
