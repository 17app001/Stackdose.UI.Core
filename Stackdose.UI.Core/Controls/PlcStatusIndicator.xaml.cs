using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PLC 状态指示器控件（只显示状态，不负责连线）
    /// </summary>
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
            if (d is PlcStatusIndicator ctrl)
                ctrl.IpDisplay.Text = (string)e.NewValue;
        }

        // ── Label ────────────────────────────────────────────────────────────

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(PlcStatusIndicator),
                new PropertyMetadata(null, OnLabelChanged));

        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatusIndicator ctrl)
            {
                var text = e.NewValue as string;
                ctrl.LabelText.Text = text ?? string.Empty;
                ctrl.LabelText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // ── CardBackground ───────────────────────────────────────────────────

        public static readonly DependencyProperty CardBackgroundProperty =
            DependencyProperty.Register(
                nameof(CardBackground),
                typeof(Brush),
                typeof(PlcStatusIndicator),
                new PropertyMetadata(null, OnCardBackgroundChanged));

        public Brush? CardBackground
        {
            get => (Brush?)GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }

        private static void OnCardBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatusIndicator ctrl && e.NewValue is Brush brush)
                ctrl.CardBorder.Background = brush;
        }

        // ── LabelForeground ──────────────────────────────────────────────────

        public static readonly DependencyProperty LabelForegroundProperty =
            DependencyProperty.Register(
                nameof(LabelForeground),
                typeof(Brush),
                typeof(PlcStatusIndicator),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0xB0)), OnLabelForegroundChanged));

        public Brush LabelForeground
        {
            get => (Brush)GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }

        private static void OnLabelForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatusIndicator ctrl && e.NewValue is Brush brush)
                ctrl.LabelText.Foreground = brush;
        }

        #endregion

        private void PlcStatusIndicator_Loaded(object sender, RoutedEventArgs e)
        {
            _globalStatus = PlcContext.GlobalStatus;
            if (_globalStatus != null)
            {
                _globalStatus.ScanUpdated += OnPlcScanUpdated;
                UpdateStatus(_globalStatus.CurrentManager?.IsConnected == true);
            }
            else
            {
                UpdateStatus(false);
            }
        }

        private void PlcStatusIndicator_Unloaded(object sender, RoutedEventArgs e)
        {
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
                        UpdateStatus(manager?.IsConnected == true);
                });
            }
            catch { }
        }

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
    }
}