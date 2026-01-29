using System;
using System.Windows;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Controls;
using Stackdose.Abstractions.Logging;

namespace ModelB.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// ?? 防止重複進入導航邏輯（避免當機）
        /// </summary>
        private bool _isNavigating = false;

        /// <summary>
        /// ?? 當前頁面的弱引用（避免記憶體洩漏）
        /// </summary>
        private WeakReference? _currentPageReference;

        public MainWindow()
        {
            InitializeComponent();

            // ?? 訂閱 SecurityContext 事件
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // ?? 視窗載入後顯示登入對話框
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            ComplianceContext.LogSystem("MainWindow initialized", LogLevel.Info);
        }

        /// <summary>
        /// ?? 視窗載入後顯示登入對話框
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 延遲顯示登入對話框，確保 MainWindow 完全載入
            Dispatcher.InvokeAsync(() =>
            {
                ShowLoginDialog();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// ?? 顯示登入對話框
        /// </summary>
        private void ShowLoginDialog()
        {
            var loginDialog = new LoginDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var result = loginDialog.ShowDialog();

            if (result != true)
            {
                // 使用者取消登入，關閉應用程式
                ComplianceContext.LogSystem("User cancelled login, closing application", LogLevel.Warning);
                Application.Current.Shutdown();
                return;
            }

            // 登入成功後初始化首頁
            ShowPage("MainPage", "MODEL-B System Overview");

            // ?? 更新 AppHeader 的使用者資訊
            UpdateAppHeaderUserInfo();
        }

        /// <summary>
        /// ?? 登入成功事件處理
        /// </summary>
        private void OnLoginSuccess(object? sender, Stackdose.UI.Core.Models.UserAccount user)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateAppHeaderUserInfo();
                ComplianceContext.LogSystem($"User {user.DisplayName} logged in successfully", LogLevel.Success);
            });
        }

        /// <summary>
        /// ?? 登出事件處理
        /// </summary>
        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 清空當前頁面
                Container.SetContent(null, "Logged Out");

                // 顯示登入對話框
                ShowLoginDialog();
            });
        }

        /// <summary>
        /// ?? 更新 AppHeader 的使用者資訊
        /// </summary>
        private void UpdateAppHeaderUserInfo()
        {
            try
            {
                var session = SecurityContext.CurrentSession;

                // 透過 MainContainer 更新 AppHeader
                if (Container?.FindName("AppHeaderControl") is Stackdose.UI.Templates.Controls.AppHeader appHeader)
                {
                    appHeader.UserName = session.CurrentUserName;
                    appHeader.UserRole = session.CurrentLevel.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] UpdateAppHeaderUserInfo Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle navigation request from LeftNavigation
        /// </summary>
        private void OnNavigationRequested(object sender, string navigationTarget)
        {
            ComplianceContext.LogSystem($"Navigation requested: {navigationTarget}", LogLevel.Info);
            ShowPage(navigationTarget, GetPageTitle(navigationTarget));
        }

        /// <summary>
        /// Show specified page (修復當機問題)
        /// </summary>
        private void ShowPage(string pageName, string pageTitle)
        {
            // ?? 防止重複進入（避免當機）
            if (_isNavigating)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Navigation already in progress, skipping {pageName}");
                return;
            }

            _isNavigating = true;

            try
            {
                // ?? 先釋放舊頁面的引用
                if (_currentPageReference != null && _currentPageReference.IsAlive)
                {
                    var oldPage = _currentPageReference.Target;
                    if (oldPage is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                // ?? 在 UI 執行緒上建立頁面
                Dispatcher.Invoke(() =>
                {
                    object? pageContent = pageName switch
                    {
                        "MainPage" => new Pages.MainPage(),
                        "DeviceControlPage" => new Pages.DeviceControlPage(),
                        "LogViewerPage" => new Stackdose.UI.Templates.Pages.LogViewerPage(),
                        "SettingsPage" => new Pages.SettingsPage(),
                        "UserManagementPage" => new Stackdose.UI.Templates.Pages.UserManagementPage(),
                        _ => null
                    };

                    if (pageContent != null)
                    {
                        // ?? 儲存弱引用
                        _currentPageReference = new WeakReference(pageContent);

                        Container.SetContent(pageContent, pageTitle);
                        ComplianceContext.LogSystem($"Page loaded: {pageName}", LogLevel.Success);
                    }
                    else
                    {
                        MessageBox.Show($"Page not found: {pageName}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, System.Windows.Threading.DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem($"Page load error: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Failed to load page: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // ?? 重置導航鎖
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Get page title by page name
        /// </summary>
        private string GetPageTitle(string pageName) => pageName switch
        {
            "MainPage" => "System Overview",
            "DeviceControlPage" => "Device Control",
            "LogViewerPage" => "Operation Logs",
            "SettingsPage" => "Settings",
            "UserManagementPage" => "User Management",
            _ => "MODEL-B DEMO"
        };

        #region Window Control Events

        private void OnLogoutRequested(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // ?? 使用 SecurityContext 登出
                SecurityContext.Logout();
                ComplianceContext.LogSystem("User logout requested", LogLevel.Warning);
            }
        }

        private void OnCloseRequested(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to close the application?", "Close Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ComplianceContext.LogSystem("Application closing", LogLevel.Warning);
                Close();
            }
        }

        private void OnMinimizeRequested(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// ?? 視窗關閉前清理資源
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 取消訂閱事件
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;

            // 釋放當前頁面
            if (_currentPageReference != null && _currentPageReference.IsAlive)
            {
                var page = _currentPageReference.Target;
                if (page is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            System.Diagnostics.Debug.WriteLine("[MainWindow] Closing and cleaning up resources");
        }

        #endregion
    }
}
