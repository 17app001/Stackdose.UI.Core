using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Process Status Indicator - 製程狀態指示器
    /// </summary>
    /// <remarks>
    /// 顯示當前製程狀態：運行中/暫停/停止
    /// 包含旋轉動畫和閃爍效果
    /// </remarks>
    public partial class ProcessStatusIndicator : UserControl
    {
        #region Private Fields

        private Storyboard? _spinnerAnimation;
        private Storyboard? _pulseAnimation;
        private Storyboard? _pausedBlinkAnimation;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 當前製程狀態
        /// </summary>
        public static readonly DependencyProperty ProcessStateProperty =
            DependencyProperty.Register(
                nameof(ProcessState),
                typeof(ProcessState),
                typeof(ProcessStatusIndicator),
                new PropertyMetadata(ProcessState.Idle, OnProcessStateChanged));

        public ProcessState ProcessState
        {
            get => (ProcessState)GetValue(ProcessStateProperty);
            set => SetValue(ProcessStateProperty, value);
        }

        /// <summary>
        /// 批次編號
        /// </summary>
        public static readonly DependencyProperty BatchNumberProperty =
            DependencyProperty.Register(
                nameof(BatchNumber),
                typeof(string),
                typeof(ProcessStatusIndicator),
                new PropertyMetadata(string.Empty, OnBatchNumberChanged));

        public string BatchNumber
        {
            get => (string)GetValue(BatchNumberProperty);
            set => SetValue(BatchNumberProperty, value);
        }

        #endregion

        #region Constructor

        public ProcessStatusIndicator()
        {
            InitializeComponent();
            LoadAnimations();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 載入動畫資源
        /// </summary>
        private void LoadAnimations()
        {
            _spinnerAnimation = (Storyboard)Resources["SpinnerAnimation"];
            _pulseAnimation = (Storyboard)Resources["PulseAnimation"];
            _pausedBlinkAnimation = (Storyboard)Resources["PausedBlinkAnimation"];
        }

        /// <summary>
        /// 製程狀態變更回呼
        /// </summary>
        private static void OnProcessStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProcessStatusIndicator indicator)
            {
                indicator.UpdateVisualState((ProcessState)e.NewValue);
            }
        }

        /// <summary>
        /// 批次編號變更回呼
        /// </summary>
        private static void OnBatchNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProcessStatusIndicator indicator)
            {
                indicator.BatchNumberText.Text = string.IsNullOrWhiteSpace((string)e.NewValue)
                    ? "Batch: N/A"
                    : $"Batch: {e.NewValue}";
            }
        }

        /// <summary>
        /// 更新視覺狀態
        /// </summary>
        private void UpdateVisualState(ProcessState state)
        {
            // 停止所有動畫
            _spinnerAnimation?.Stop();
            _pulseAnimation?.Stop();
            _pausedBlinkAnimation?.Stop();

            switch (state)
            {
                case ProcessState.Idle:
                    // 隱藏指示器
                    RootBorder.Visibility = Visibility.Collapsed;
                    break;

                case ProcessState.Initializing:
                    // 顯示初始化狀態
                    RootBorder.Visibility = Visibility.Visible;
                    StatusText.Text = "Initializing...";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D)); // Amber/Orange

                    // 啟動旋轉動畫（速度較慢）
                    if (_spinnerAnimation != null)
                    {
                        _spinnerAnimation.Duration = TimeSpan.FromSeconds(2); // 較慢的旋轉
                        _spinnerAnimation.Begin();
                    }
                    _pulseAnimation?.Begin();
                    break;

                case ProcessState.Running:
                    // 顯示運行中狀態
                    RootBorder.Visibility = Visibility.Visible;
                    StatusText.Text = "Processing...";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x00, 0xBC, 0xD4)); // Cyan

                    // 啟動旋轉動畫（正常速度）
                    if (_spinnerAnimation != null)
                    {
                        _spinnerAnimation.Duration = TimeSpan.FromSeconds(1.5); // 恢復正常速度
                        _spinnerAnimation.Begin();
                    }
                    _pulseAnimation?.Begin();
                    break;

                case ProcessState.Paused:
                    // 顯示暫停狀態
                    RootBorder.Visibility = Visibility.Visible;
                    StatusText.Text = "Paused";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00)); // Orange

                    // 停止旋轉，啟動閃爍
                    _pausedBlinkAnimation?.Begin();
                    break;

                case ProcessState.Stopped:
                    // 短暫顯示停止狀態，然後隱藏
                    RootBorder.Visibility = Visibility.Visible;
                    StatusText.Text = "Stopped";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36)); // Red

                    // 2秒後自動隱藏
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = System.TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        RootBorder.Visibility = Visibility.Collapsed;
                    };
                    timer.Start();
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// 製程狀態枚舉
    /// </summary>
    public enum ProcessState
    {
        /// <summary>閒置</summary>
        Idle,
        /// <summary>初始化中</summary>
        Initializing,
        /// <summary>運行中</summary>
        Running,
        /// <summary>暫停</summary>
        Paused,
        /// <summary>停止</summary>
        Stopped
    }
}
