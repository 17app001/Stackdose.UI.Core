using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Templates.Controls
{
    /// <summary>
    /// AppHeader - 應用程式標題列（整合 SecurityContext）
    /// </summary>
    public partial class AppHeader : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        /// <summary>
        /// 追蹤是否為全螢幕模式
        /// </summary>
        private bool _isFullscreen = true; // 預設為 true，因為 MainWindow 預設全螢幕

        #endregion

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

        #region Events (保留向外通知能力)

        public event RoutedEventHandler? LogoutClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? SwitchUserClicked;
        public event RoutedEventHandler? FullscreenClicked;

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
                // ?? 新增確認對話框
                var result = CyberMessageBox.Show(
                    "確定要切換帳號嗎？",
                    "切換帳號確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                var dialog = new LoginDialog
                {
                    Owner = Window.GetWindow(this),
                    Title = "切換使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    // 登入成功會自動觸發 SecurityContext.LoginSuccess 事件
                    // UpdateUserInfo() 會被自動呼叫
                    
                    // 觸發事件（向外通知）
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

                // 觸發事件（向外通知）
                MinimizeClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppHeader] Minimize Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 全螢幕切換按鈕點擊
        /// </summary>
        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window == null) return;

                System.Diagnostics.Debug.WriteLine($"[AppHeader] Fullscreen Toggle - Current: {_isFullscreen}");

                if (_isFullscreen)
                {
                    // 目前是全螢幕模式，還原為視窗模式
                    System.Diagnostics.Debug.WriteLine("[AppHeader] Switching to Windowed Mode");
                    
                    // 先還原 WindowState，再改 WindowStyle（避免衝突）
                    window.WindowState = WindowState.Normal;
                    
                    // 使用 Dispatcher 延遲執行 WindowStyle 變更
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        window.WindowStyle = WindowStyle.SingleBorderWindow;
                        window.ResizeMode = ResizeMode.CanResize;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    
                    _isFullscreen = false;
                    
                    // 更新按鈕圖示為全螢幕圖示（四個角向外）
                    if (sender is Button btn && btn.Content is Path path)
                    {
                        path.Data = Geometry.Parse("M0,0 L8,0 L8,2 L2,2 L2,8 L0,8 Z M10,0 L18,0 L18,8 L16,8 L16,2 L10,2 Z M0,10 L2,10 L2,16 L8,16 L8,18 L0,18 Z M16,16 L16,10 L18,10 L18,18 L10,18 L10,16 Z");
                        btn.ToolTip = "Enter Fullscreen";
                    }
                }
                else
                {
                    // 目前是視窗模式，切換到全螢幕模式
                    System.Diagnostics.Debug.WriteLine("[AppHeader] Switching to Fullscreen Mode");
                    
                    // 先改 WindowStyle，再最大化（避免衝突）
                    window.WindowStyle = WindowStyle.None;
                    window.ResizeMode = ResizeMode.NoResize;
                    
                    // 使用 Dispatcher 延遲執行 WindowState 變更
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        window.WindowState = WindowState.Maximized;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    
                    _isFullscreen = true;
                    
                    // 更新按鈕圖示為還原圖示（四個角向內）
                    if (sender is Button btn && btn.Content is Path path)
                    {
                        path.Data = Geometry.Parse("M2,0 L2,2 L0,2 L0,8 L6,8 L6,6 L8,6 L8,0 Z M10,0 L10,6 L12,6 L12,8 L18,8 L18,2 L16,2 L16,0 Z M0,10 L0,16 L6,16 L6,18 L8,18 L8,12 L6,12 L6,10 Z M12,10 L10,10 L10,18 L16,18 L16,16 L18,16 L18,10 Z");
                        btn.ToolTip = "Exit Fullscreen";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AppHeader] Fullscreen Toggle - After: {_isFullscreen}");
                
                // 觸發事件（向外通知）
                FullscreenClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppHeader] Fullscreen Error: {ex.Message}");
                CyberMessageBox.Show(
                    $"Full screen toggle failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
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
