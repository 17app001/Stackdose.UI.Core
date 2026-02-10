using System.Windows;
using System.Windows.Controls;
using Stackdose.Hardware.Simulators;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// ?? PLC ����������O
    /// </summary>
    /// <remarks>
    /// ���ѧY�ɱ������ PLC ���\��G
    /// - �ʱ��B�檬�A
    /// - �վ㪫�z�Ѽ�
    /// - �`�J�G�ٴ���
    /// - �d�ݲέp��T
    /// </remarks>
    public partial class SimulatorControlPanel : UserControl
    {
        private SmartPlcSimulator? _simulator;

        public SimulatorControlPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// �j�w���������
        /// </summary>
        public void BindSimulator(SmartPlcSimulator simulator)
        {
            _simulator = simulator;

            // �q�\�ƥ�
            _simulator.OnTemperatureChanged += temp =>
            {
                Dispatcher.InvokeAsync(() => TempDisplay.Text = $"{temp:F1}�XC");
            };

            _simulator.OnAxisMoved += pos =>
            {
                Dispatcher.InvokeAsync(() => AxisDisplay.Text = $"{pos:F0} �gm");
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

            // �Ұʲέp��s
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
        // ������s�ƥ�
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
