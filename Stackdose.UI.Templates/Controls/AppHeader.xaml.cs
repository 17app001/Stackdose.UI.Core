using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Templates.Controls
{
    /// <summary>
    /// AppHeader - 應用程式標題列（整合 SecurityContext）
    /// </summary>
    public partial class AppHeader : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(AppHeader), 
                new PropertyMetadata("Page Title"));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        #endregion

        #region Properties (動態綁定到 SecurityContext)

        private string _userName = "Guest";
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _userRole = "Not Logged In";
        public string UserRole
        {
            get => _userRole;
            set
            {
                _userRole = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Events (保留向後相容性)

        public event RoutedEventHandler? LogoutClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? SwitchUserClicked;

        #endregion

        #region Constructor

        public AppHeader()
        {
            InitializeComponent();
            
            // 設定 DataContext
            DataContext = this;

            // 訂閱 SecurityContext 事件
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // 初始化使用者資訊
            UpdateUserInfo();

            // 載入時更新
            Loaded += AppHeader_Loaded;
            Unloaded += AppHeader_Unloaded;
        }

        #endregion

        #region Lifecycle Events

        private void AppHeader_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUserInfo();
        }

        private void AppHeader_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消訂閱事件
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;
        }

        #endregion

        #region SecurityContext Events

        private void OnLoginSuccess(object? sender, Stackdose.UI.Core.Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateUserInfo();
            });
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateUserInfo();
            });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 更新使用者資訊顯示
        /// </summary>
        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;

            if (session.IsLoggedIn)
            {
                UserName = session.CurrentUserName;
                UserRole = session.CurrentLevel.ToString();
            }
            else
            {
                UserName = "Guest";
                UserRole = "Not Logged In";
            }
        }

        #endregion

        #region Button Click Handlers

        /// <summary>
        /// 登出按鈕點擊
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
                    
                    // 觸發事件（向後相容）
                    LogoutClicked?.Invoke(this, e);

                    // ? 登出後立即顯示登入視窗
                    var loginDialog = new LoginDialog
                    {
                        Owner = Window.GetWindow(this),
                        Title = "請重新登入"
                    };
                    loginDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show(
                    $"登出失敗: {ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 切換使用者按鈕點擊
        /// </summary>
        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new LoginDialog
                {
                    Owner = Window.GetWindow(this),
                    Title = "切換使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 登入成功會自動觸發 SecurityContext.LoginSuccess 事件
                    // UpdateUserInfo() 會被自動呼叫
                    
                    // 觸發事件（向後相容）
                    SwitchUserClicked?.Invoke(this, e);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show(
                    $"切換使用者失敗: {ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 最小化按鈕點擊
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.WindowState = WindowState.Minimized;
                }

                // 觸發事件（向後相容）
                MinimizeClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppHeader] Minimize Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 關閉按鈕點擊
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = CyberMessageBox.Show(
                    "確定要關閉程式嗎？",
                    "關閉確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    var window = Window.GetWindow(this);
                    if (window != null)
                    {
                        window.Close();
                    }

                    // 觸發事件（向後相容）
                    CloseClicked?.Invoke(this, e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppHeader] Close Error: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
