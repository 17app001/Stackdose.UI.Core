using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Media;

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

            //  訂閱PlcLabel事件
            LblTemp.ValueChanged += LblTemp_ValueChanged;
            // 訂閱感測器警報事件
            SensorContext.AlarmTriggered += OnSensorAlarmTriggered;
            //SensorContext.AlarmCleared += OnSensorAlarmCleared;
        }

        private void OnSensorAlarmTriggered(object? sender, SensorAlarmEventArgs e)
        {
            // e.Sensor 包含觸發的感測器資訊
            // e.EventTime 包含觸發時間
            // 方式 1：針對特定感測器執行動作
            if (e.Sensor.Device == "D90")
            {
                MessageBox.Show($"緊急警報！{e.Sensor.OperationDescription} 已觸發！");
            }
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