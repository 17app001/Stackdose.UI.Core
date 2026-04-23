using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Controls.Base;
using Stackdose.UI.Core.Helpers;
using System.Windows;
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
    public partial class PlcStatusIndicator : PlcControlBase
    {
        public PlcStatusIndicator()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>顯示的 IP 位址</summary>
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
                indicator.IpDisplay.Text = (string)e.NewValue;
        }

        #endregion

        #region Lifecycle

        protected override void OnPlcControlLoaded()
        {
            var isConnected = PlcContext.GlobalStatus?.CurrentManager?.IsConnected ?? false;
            UpdateStatus(isConnected);
        }

        protected override void OnPlcControlUnloaded() { }

        protected override void OnPlcConnected(IPlcManager manager)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Action(() => UpdateStatus(manager?.IsConnected ?? false)));
        }

        protected override void OnPlcDataUpdated(IPlcManager manager)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Action(() => UpdateStatus(manager?.IsConnected ?? false)));
        }

        #endregion

        #region Status Display

        private void UpdateStatus(bool isConnected)
        {
            if (isConnected)
            {
                StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen);
                StatusLight.Effect = new DropShadowEffect { Color = Colors.LimeGreen, BlurRadius = 15, ShadowDepth = 0 };
                StatusText.Text = "CONNECTED";
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                StatusLight.Fill = new SolidColorBrush(Colors.Red);
                StatusLight.Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0 };
                StatusText.Text = "DISCONNECTED";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        #endregion
    }
}