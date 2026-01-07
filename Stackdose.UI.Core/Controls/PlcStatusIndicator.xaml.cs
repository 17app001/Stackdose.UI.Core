using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PLC 狀態指示器控件（只顯示狀態，不負責連線）
    /// </summary>
    /// <remarks>
    /// 此控件訂閱 PlcContext.GlobalStatus 的事件來顯示 PLC 連線狀態
    /// 不會執行連線/斷線操作，適合放在需要顯示狀態但不控制連線的地方
    /// </remarks>
    public partial class PlcStatusIndicator : UserControl
    {
        private PlcStatus? _globalStatus;

        public PlcStatusIndicator()
        {
            InitializeComponent();
            this.Loaded += PlcStatusIndicator_Loaded;
            this.Unloaded += PlcStatusIndicator_Unloaded;
        }

        #region Dependency Properties

        /// <summary>
        /// 顯示的 IP 位址
        /// </summary>
        public static readonly DependencyProperty DisplayAddressProperty =
            DependencyProperty.Register(
                nameof(DisplayAddress),
                typeof(string),
                typeof(PlcStatusIndicator),
                new PropertyMetadata("192.168.22.39:3000", OnDisplayAddressChanged));

        public string DisplayAddress
        {
            get => (string)GetValue(DisplayAddressProperty);
            set => SetValue(DisplayAddressProperty, value);
        }

        private static void OnDisplayAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatusIndicator indicator)
            {
                indicator.IpDisplay.Text = (string)e.NewValue;
            }
        }

        #endregion

        private void PlcStatusIndicator_Loaded(object sender, RoutedEventArgs e)
        {
            // 訂閱全域 PlcStatus
            _globalStatus = PlcContext.GlobalStatus;

            if (_globalStatus != null)
            {
                // 訂閱 ScanUpdated 事件
                _globalStatus.ScanUpdated += OnPlcScanUpdated;

                // 立即更新狀態
                UpdateStatus(_globalStatus.CurrentManager != null && _globalStatus.CurrentManager.IsConnected);
            }
            else
            {
                // 沒有全域 PlcStatus
                UpdateStatus(false);
            }
        }

        private void PlcStatusIndicator_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消訂閱
            if (_globalStatus != null)
            {
                _globalStatus.ScanUpdated -= OnPlcScanUpdated;
                _globalStatus = null;
            }
        }

        private void OnPlcScanUpdated(IPlcManager manager)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;

                Dispatcher.Invoke(() =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        UpdateStatus(manager != null && manager.IsConnected);
                    }
                });
            }
            catch
            {
                // 忽略 Dispatcher 錯誤
            }
        }

        private void UpdateStatus(bool isConnected)
        {
            if (isConnected)
            {
                // 連線狀態
                StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen);
                StatusLight.Effect = new DropShadowEffect
                {
                    Color = Colors.LimeGreen,
                    BlurRadius = 15,
                    ShadowDepth = 0
                };
                StatusText.Text = "CONNECTED";
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                // 斷線狀態
                StatusLight.Fill = new SolidColorBrush(Colors.Red);
                StatusLight.Effect = new DropShadowEffect
                {
                    Color = Colors.Red,
                    BlurRadius = 10,
                    ShadowDepth = 0
                };
                StatusText.Text = "DISCONNECTED";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }
}
