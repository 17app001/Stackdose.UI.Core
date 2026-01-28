using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Cyber 風格的自訂 MessageBox
    /// </summary>
    public partial class CyberMessageBox : Window
    {
        #region 公開屬性

        /// <summary>
        /// 使用者選擇的結果
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        #endregion

        #region 建構子

        private CyberMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            InitializeComponent();

            // 設定標題
            TitleText.Text = title;
            Title = title;

            // 設定訊息
            MessageText.Text = message;

            // 設定圖示
            SetIcon(icon);

            // 設定按鈕
            SetButtons(buttons);

            // 載入動畫
            Loaded += (s, e) => PlayShowAnimation();
        }

        #endregion

        #region 靜態方法 (類似標準 MessageBox)

        /// <summary>
        /// 顯示訊息（僅 OK 按鈕）
        /// </summary>
        public static MessageBoxResult Show(string message)
        {
            return Show(message, "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 顯示訊息（指定標題）
        /// </summary>
        public static MessageBoxResult Show(string message, string title)
        {
            return Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 顯示訊息（指定按鈕）
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons)
        {
            return Show(message, title, buttons, MessageBoxImage.Information);
        }

        /// <summary>
        /// 顯示訊息（完整參數）
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            // ?? 修正：確保在 UI 執行緒上執行
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var messageBox = new CyberMessageBox(message, title, buttons, icon);
                    messageBox.ShowDialog();
                    return messageBox.Result;
                });
            }
            else
            {
                var messageBox = new CyberMessageBox(message, title, buttons, icon);
                messageBox.ShowDialog();
                return messageBox.Result;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 設定圖示
        /// </summary>
        private void SetIcon(MessageBoxImage icon)
        {
            string iconText = "INFO";
            string color = "#00E5FF"; // 預設藍色

            switch (icon)
            {
                case MessageBoxImage.Information:
                    iconText = "INFO";
                    color = "#00E5FF";
                    break;
                case MessageBoxImage.Warning:
                    iconText = "WARN";
                    color = "#FFA726";
                    break;
                case MessageBoxImage.Error:
                    iconText = "ERR";
                    color = "#FF5252";
                    break;
                case MessageBoxImage.Question:
                    iconText = "ASK";
                    color = "#AB47BC";
                    break;
                default:
                    iconText = "OK";
                    color = "#4CAF50";
                    break;
            }

            TitleIcon.Text = iconText;
            MessageIcon.Text = iconText.Substring(0, 1); // Use first letter for large icon

            // 更新邊框顏色
            var border = (Border)Content;
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

            // 更新標題文字顏色
            TitleText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        /// <summary>
        /// 設定按鈕
        /// </summary>
        private void SetButtons(MessageBoxButton buttons)
        {
            ButtonPanel.Children.Clear();

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("確定", MessageBoxResult.OK, true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton("確定", MessageBoxResult.OK, true);
                    AddButton("取消", MessageBoxResult.Cancel, false, true);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("是", MessageBoxResult.Yes, true);
                    AddButton("否", MessageBoxResult.No, false, true);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("是", MessageBoxResult.Yes, true);
                    AddButton("否", MessageBoxResult.No, false);
                    AddButton("取消", MessageBoxResult.Cancel, false, true);
                    break;
            }
        }


        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }


        /// <summary>
        /// 新增按鈕
        /// </summary>
        private void AddButton(string text, MessageBoxResult result, bool isDefault, bool isCancel = false)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(5, 0, 5, 0),
                Style = isCancel
                    ? (Style)FindResource("CancelButtonStyle")
                    : (Style)FindResource("CyberButtonStyle")
            };

            button.Click += (s, e) =>
            {
                Result = result;
                Close();
            };

            if (isDefault)
            {
                button.IsDefault = true;
            }

            if (isCancel)
            {
                button.IsCancel = true;
            }

            ButtonPanel.Children.Add(button);
        }

        /// <summary>
        /// 關閉按鈕點擊
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        /// <summary>
        /// 播放顯示動畫
        /// </summary>
        private void PlayShowAnimation()
        {
            // ?? 修正：對內容容器（Border）套用動畫，而非 Window 本身
            var border = (Border)Content;

            // 淡入動畫（套用到 Window）
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            // 縮放動畫（套用到 Border）
            var scaleX = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.BackEase
                {
                    Amplitude = 0.3,
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };

            var scaleY = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.BackEase
                {
                    Amplitude = 0.3,
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };

            // ?? 修正：將 ScaleTransform 套用到 Border，而非 Window
            var scaleTransform = new ScaleTransform(0.9, 0.9);
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.RenderTransform = scaleTransform;

            // Window 套用淡入動畫
            BeginAnimation(OpacityProperty, fadeIn);

            // Border 套用縮放動畫
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        #endregion
    }

    #region 擴充方法 (選用)

    /// <summary>
    /// CyberMessageBox 擴充方法
    /// </summary>
    public static class CyberMessageBoxExtensions
    {
        /// <summary>
        /// 顯示成功訊息
        /// </summary>
        public static void ShowSuccess(string message, string title = "成功")
        {
            CyberMessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.None);
        }

        /// <summary>
        /// 顯示警告訊息
        /// </summary>
        public static void ShowWarning(string message, string title = "警告")
        {
            CyberMessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        public static void ShowError(string message, string title = "錯誤")
        {
            CyberMessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 顯示確認對話框
        /// </summary>
        public static bool Confirm(string message, string title = "確認")
        {
            var result = CyberMessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }

    #endregion
}
