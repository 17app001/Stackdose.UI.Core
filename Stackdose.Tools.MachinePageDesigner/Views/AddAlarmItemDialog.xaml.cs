using System.Windows;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class AddAlarmItemDialog : Window
{
    public AlarmEditItem? Result { get; private set; }

    public AddAlarmItemDialog()
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

        if (!int.TryParse(TxtBit.Text.Trim(), out int bit))
        {
            MessageBox.Show("Bit 索引必須是整數", "格式錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtBit.Focus();
            return;
        }

        Result = new AlarmEditItem
        {
            Group                = TxtGroup.Text.Trim(),
            Device               = TxtDevice.Text.Trim(),
            Bit                  = bit,
            OperationDescription = TxtDescription.Text.Trim(),
        };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
