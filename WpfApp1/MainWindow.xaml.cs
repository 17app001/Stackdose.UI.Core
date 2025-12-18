using System.Windows;
using WpfApp1.ViewModels;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            //// 🔥 顯示登入對話框（不使用快速登入）
            //bool loginSuccess = LoginDialog.ShowLoginDialog();

            //if (!loginSuccess)
            //{
            //    // 取消登入時預設為 Guest
            //    SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Guest);
            //}

            // 🔥 預設以 Engineer 身份登入（測試用）
            SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Supervisor);

            // 🔥 設定 DataContext 為 ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 🔥 訂閱登入/登出事件（更新 UI 標題）
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // 🔥 更新視窗標題顯示當前使用者
            UpdateWindowTitle();
            UpdateUserInfo();
        }

        private void OnLoginSuccess(object? sender, Stackdose.UI.Core.Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateWindowTitle();
                UpdateUserInfo();
            });
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateWindowTitle();
                UpdateUserInfo();
                
                // 登出後顯示登入對話框
                bool loginSuccess = LoginDialog.ShowLoginDialog();
                if (!loginSuccess)
                {
                    // 如果取消登入，預設以 Operator 身份登入
                    SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Operator);
                }
            });
        }

        private void UpdateWindowTitle()
        {
            var session = SecurityContext.CurrentSession;
            if (session.IsLoggedIn)
            {
                this.Title = $"Stackdose Control System - {session.CurrentUserName} ({session.CurrentLevel})";
            }
            else
            {
                this.Title = "Stackdose Control System - Not Logged In";
            }
        }

        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;
            if (session.IsLoggedIn)
            {
                UserInfoText.Text = $"{session.CurrentUserName}\n{session.CurrentLevel}";
            }
            else
            {
                UserInfoText.Text = "未登入\nGuest";
            }
        }

        #region 權限測試按鈕事件

        private void OperatorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "✅ 操作員功能：啟動製程",
                Stackdose.UI.Core.Models.LogLevel.Success,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "✅ 啟動製程成功！\n\n這是 Level 1 (Operator) 權限功能",
                "操作成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void InstructorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "✅ 指導員功能：查看日誌",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "📊 日誌查看功能\n\n這是 Level 2 (Instructor) 權限功能",
                "查看日誌",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void SupervisorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "✅ 主管功能：管理使用者",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "👥 使用者管理功能\n\n這是 Level 3 (Supervisor) 權限功能\n可以管理 Level 1-2 的帳號",
                "使用者管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void EngineerButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "✅ 工程師功能：修改參數",
                Stackdose.UI.Core.Models.LogLevel.Warning,
                showInUi: true
            );
            
            // 記錄到 Audit Trail
            ComplianceContext.LogAuditTrail(
                "Parameter Modified",
                "D100",
                "100",
                "200",
                $"Modified by {SecurityContext.CurrentSession.CurrentUserName}",
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "⚙️ 參數修改功能\n\n這是 Level 4 (Engineer) 最高權限功能\n已記錄到 Audit Trail",
                "修改參數",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.Logout();
        }

        #endregion

        /// <summary>
        /// 視窗關閉時清理資源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 取消訂閱事件
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;
            
            // 清理 ViewModel
            _viewModel.Cleanup();
            
            // 登出
            SecurityContext.Logout();
        }
    }
}