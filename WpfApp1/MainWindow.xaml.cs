using Stackdose.UI.Core.Controls;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LblTemp.ValueChanged += LblTemp_ValueChanged;
        }

        private void LblTemp_ValueChanged(object? sender, PlcValueChangedEventArgs e)
        {
            var plcLabel = (sender as PlcLabel);

            if (e.Value is null || plcLabel is null) return;

            if (!double.TryParse(e.Value.ToString(), out double currentTemp))
            {
                return;
            }

            if (currentTemp >= 100)
            {
                // 因為 TextBlock 現在是綁定的，所以這裡改 UserControl 的顏色，裡面就會跟著變！
                plcLabel.Foreground = Brushes.Red;
            }
            else if (currentTemp >= 75)
            {
                LblTemp.Foreground = Brushes.Orange;
            }
            else if (currentTemp >= 50)
            {
                LblTemp.Foreground = Brushes.Yellow;
            }
            else
            {
                LblTemp.Foreground = Brushes.Green;
            }


        }
    }
}