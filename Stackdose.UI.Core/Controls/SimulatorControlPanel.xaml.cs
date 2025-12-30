using System.Windows;
using System.Windows.Controls;
using Stackdose.Hardware.Simulators;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// ?? PLC 模擬器控制面板
    /// </summary>
    /// <remarks>
    /// 提供即時控制虛擬 PLC 的功能：
    /// - 監控運行狀態
    /// - 調整物理參數
    /// - 注入故障測試
    /// - 查看統計資訊
    /// </remarks>
    public partial class SimulatorControlPanel : UserControl
    {
        private SmartPlcSimulator? _simulator;

        public SimulatorControlPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 綁定模擬器實例
        /// </summary>
        public void BindSimulator(SmartPlcSimulator simulator)
        {
            _simulator = simulator;

            // 訂閱事件
            _simulator.OnTemperatureChanged += temp =>
            {
                Dispatcher.InvokeAsync(() => TempDisplay.Text = $"{temp:F1}°C");
            };

            _simulator.OnAxisMoved += pos =>
            {
                Dispatcher.InvokeAsync(() => AxisDisplay.Text = $"{pos:F0} μm");
            };

            _simulator.OnAlarmTriggered += alarm =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    AlarmList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {alarm}");
                    if (AlarmList.Items.Count > 10)
                        AlarmList.Items.RemoveAt(10);
                });
            };

            // 啟動統計更新
            StartStatsUpdate();
        }

        private async void StartStatsUpdate()
        {
            while (_simulator != null && _simulator.IsConnected)
            {
                await Task.Delay(1000);
                
                if (_simulator?.Stats != null)
                {
                    StatsDisplay.Text = _simulator.Stats.ToString();
                }
            }
        }

        // ============================================================
        // 控制按鈕事件
        // ============================================================

        private async void OnStartHeater(object sender, RoutedEventArgs e)
        {
            if (_simulator != null)
                await _simulator.WriteDeviceValueAsync("M0,1");
        }

        private async void OnStopHeater(object sender, RoutedEventArgs e)
        {
            if (_simulator != null)
                await _simulator.WriteDeviceValueAsync("M0,0");
        }

        private async void OnStartMotor(object sender, RoutedEventArgs e)
        {
            if (_simulator != null)
                await _simulator.WriteDeviceValueAsync("M1,1");
        }

        private async void OnStopMotor(object sender, RoutedEventArgs e)
        {
            if (_simulator != null)
                await _simulator.WriteDeviceValueAsync("M1,0");
        }

        private void OnInjectTempError(object sender, RoutedEventArgs e)
        {
            _simulator?.InjectFault("TEMP_SENSOR_ERROR", 1000);
        }

        private void OnInjectServoError(object sender, RoutedEventArgs e)
        {
            _simulator?.InjectFault("SERVO_ERROR", 1000);
        }

        private void OnClearAllFaults(object sender, RoutedEventArgs e)
        {
            _simulator?.ClearAllFaults();
            AlarmList.Items.Clear();
        }
    }
}
