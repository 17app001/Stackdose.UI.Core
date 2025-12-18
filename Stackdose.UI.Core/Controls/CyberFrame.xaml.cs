using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls
{
    public partial class CyberFrame : UserControl
    {
        private DispatcherTimer? _clockTimer;

        public CyberFrame()
        {
            InitializeComponent();

            // 啟動時鐘，每秒更新一次時間
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // 訂閱登入/登出事件
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // 初始化使用者資訊
            UpdateUserInfo();

            // 當控制項卸載時取消訂閱
            this.Unloaded += (s, e) =>
            {
                SecurityContext.LoginSuccess -= OnLoginSuccess;
                SecurityContext.LogoutOccurred -= OnLogoutOccurred;
                _clockTimer?.Stop();
            };
        }

        #region Dependency Properties

        /// <summary>
        /// 系統標題
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CyberFrame),
                new PropertyMetadata("SYSTEM"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// 是否顯示狀態指示器（Alarm、Total 等）
        /// </summary>
        public static readonly DependencyProperty ShowStatusIndicatorsProperty =
            DependencyProperty.Register("ShowStatusIndicators", typeof(bool), typeof(CyberFrame), 
                new PropertyMetadata(false, OnShowStatusIndicatorsChanged));

        public bool ShowStatusIndicators
        {
            get => (bool)GetValue(ShowStatusIndicatorsProperty);
            set => SetValue(ShowStatusIndicatorsProperty, value);
        }

        private static void OnShowStatusIndicatorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame)
            {
                var panel = frame.FindName("StatusIndicatorsPanel") as StackPanel;
                if (panel != null)
                {
                    panel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 時鐘更新
        /// </summary>
        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 登入成功事件
        /// </summary>
        private void OnLoginSuccess(object? sender, Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        /// <summary>
        /// 登出事件
        /// </summary>
        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        /// <summary>
        /// 登出按鈕點擊
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CyberMessageBox.Show(
                "確定要登出嗎？",
                "登出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                SecurityContext.Logout();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 更新使用者資訊顯示
        /// </summary>
        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;
            
            // 使用 FindName 查找控制項
            var userNameText = this.FindName("UserNameText") as TextBlock;
            var userLevelText = this.FindName("UserLevelText") as TextBlock;

            if (userNameText != null && userLevelText != null)
            {
                if (session.IsLoggedIn)
                {
                    userNameText.Text = session.CurrentUserName;
                    userLevelText.Text = session.CurrentLevel.ToString();
                }
                else
                {
                    userNameText.Text = "Guest";
                    userLevelText.Text = "Not Logged In";
                }
            }
        }

        #endregion
    }
}