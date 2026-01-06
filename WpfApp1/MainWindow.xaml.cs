using System.Windows;
using WpfApp1.ViewModels;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Examples;
using Stackdose.UI.Core.Models;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        
        /// <summary>
        /// 標記 Recipe 是否已經下載到 PLC（避免重連時重複下載）
        /// </summary>
        private bool _recipeDownloadedToPLC = false;

        public MainWindow()
        {
            InitializeComponent();

            // 🔥 ComplianceContext 已在 App.OnStartup 中初始化
            
            // 預設以 Admin 身份登入（測試用）
            // 🔥 已移至 App.OnStartup，避免時序問題
            // SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Admin);

            // 設定 DataContext 為 ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 訂閱登入/登出事件（更新 UI 標題）
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // ⭐ 初始化 Recipe 系統 (不自動載入，等待 PLC 連線)
            _ = InitializeRecipeSystemAsync();

            // 更新視窗標題
            UpdateWindowTitle();
        }

        /// <summary>
        /// 初始化 Recipe 系統
        /// </summary>
        private async Task InitializeRecipeSystemAsync()
        {
            // ⭐ 預設載入 Recipe 1
            bool success = await RecipeContext.LoadRecipeAsync("Recipe1.json", isAutoLoad: true, setAsActive: true);

            if (success)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Recipe system initialized successfully",
                    Stackdose.UI.Core.Models.LogLevel.Success,
                    showInUi: true
                );
            }
            else
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Failed to initialize Recipe system",
                    Stackdose.UI.Core.Models.LogLevel.Warning,
                    showInUi: true
                );
            }
        }

        private void OnLoginSuccess(object? sender, Stackdose.UI.Core.Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateWindowTitle();
            });
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateWindowTitle();
                
                // 🔥 修正：登出後顯示登入對話框
                bool loginSuccess = LoginDialog.ShowLoginDialog();
                if (!loginSuccess)
                {
                    // 🔥 修正：如果取消登入，以 Guest 身份留在首頁（而不是關閉程式）
                    SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Guest);
                    ComplianceContext.LogSystem(
                        "[Logout] User cancelled login, staying as Guest",
                        Stackdose.UI.Core.Models.LogLevel.Info,
                        showInUi: true
                    );
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

        #region 權限測試按鈕事件

        private void OperatorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[OK] 操作員功能：啟動製程",
                Stackdose.UI.Core.Models.LogLevel.Success,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "[OK] 啟動製程成功！\n\n這是 Level 1 (Operator) 權限功能",
                "操作成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void InstructorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[OK] 指導員功能：查看日誌",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "[LOG] 日誌查看功能\n\n這是 Level 2 (Instructor) 權限功能",
                "查看日誌",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void SupervisorButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[OK] 主管功能：管理使用者",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "[USER] 使用者管理功能\n\n這是 Level 3 (Supervisor) 權限功能\n可以管理 Level 1-2 的帳號",
                "使用者管理",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void EngineerButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[OK] 工程師功能：修改參數",
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
                "[CONFIG] 參數修改功能\n\n這是 Level 4 (Engineer) 最高權限功能\n已記錄到 Audit Trail",
                "修改參數",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.Logout();
        }

        private void StartProcess_Click(object sender, RoutedEventArgs e)
        {
            // 製程開始邏輯
            
            // 1. 記錄到系統日誌
            ComplianceContext.LogSystem(
                "[START] 製程開始",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );

            // 2. 寫入 PLC 啟動信號（例如：M100 = 1）
            var plcManager = PlcContext.GlobalStatus?.CurrentManager;
            if (plcManager != null && plcManager.IsConnected)
            {
                // 寫入啟動信號
                _ = plcManager.WriteAsync("M100,1");
                
                // 記錄到 Audit Trail
                ComplianceContext.LogAuditTrail(
                    deviceName: "製程控制",
                    address: "M100",
                    oldValue: "0",
                    newValue: "1",
                    reason: $"製程開始 by {SecurityContext.CurrentSession.CurrentUserName}",
                    showInUi: true
                );
            }
            else
            {
                // PLC 未連線警告
                CyberMessageBox.Show(
                    "[WARNING] PLC 未連線\n無法啟動製程",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // 3. 顯示確認訊息
            CyberMessageBox.Show(
                "✅ 製程已啟動\n\n請確認設備運行狀態",
                "製程開始",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        #endregion

        #region 主題測試按鈕事件

        /// <summary>
        /// 切換 Dark/Light 主題
        /// </summary>
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            // 直接使用 CyberFrame 的主題切換按鈕功能
            var cyberFrame = FindCyberFrame(this);
            if (cyberFrame != null)
            {
                cyberFrame.ToggleTheme();
                
                ComplianceContext.LogSystem(
                    $"主題已切換為 {(cyberFrame.UseLightTheme ? "Light" : "Dark")} 模式",
                    Stackdose.UI.Core.Models.LogLevel.Info,
                    showInUi: true
                );
            }
        }

        /// <summary>
        /// 顯示主題統計資訊
        /// </summary>
        private void ShowThemeStats_Click(object sender, RoutedEventArgs e)
        {
            var stats = ThemeManager.GetStatistics();
            var currentTheme = ThemeManager.CurrentTheme;
            
            string message = $"📊 主題管理統計資訊\n\n" +
                           $"當前主題: {currentTheme.ThemeName} ({(currentTheme.IsLightTheme ? "Light" : "Dark")})\n" +
                           $"變更時間: {currentTheme.ChangedAt:yyyy-MM-dd HH:mm:ss}\n\n" +
                           $"已註冊控制項:\n" +
                           $"  • 總數: {stats.Total}\n" +
                           $"  • 存活: {stats.Alive}\n" +
                           $"  • 失效: {stats.Dead}\n\n" +
                           $"記憶體效率: {(stats.Total > 0 ? (stats.Alive * 100.0 / stats.Total):0):F1}%";
            
            CyberMessageBox.Show(
                message,
                "主題統計資訊",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            
            ComplianceContext.LogSystem(
                $"[Theme Stats] Total={stats.Total}, Alive={stats.Alive}, Dead={stats.Dead}",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
        }

        /// <summary>
        /// 開啟完整主題測試視窗
        /// </summary>
        private void OpenThemeDemo_Click(object sender, RoutedEventArgs e)
        {
            var demoWindow = new ThemeManagerDemoWindow();
            demoWindow.Show();
            
            ComplianceContext.LogSystem(
                "已開啟 ThemeManager 測試視窗",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
        }

        /// <summary>
        /// 列印已註冊控制項（輸出到 Debug Console）
        /// </summary>
        private void PrintRegistered_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.PrintRegisteredControls();
            
            CyberMessageBox.Show(
                "已將已註冊控制項清單輸出到 Debug Console\n\n請查看 Visual Studio 的「輸出」視窗",
                "列印完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            
            ComplianceContext.LogSystem(
                "已列印所有已註冊的主題感知控制項",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
        }

        /// <summary>
        /// 手動清理失效的 WeakReference
        /// </summary>
        private void CleanupTheme_Click(object sender, RoutedEventArgs e)
        {
            var statsBefore = ThemeManager.GetStatistics();
            
            ThemeManager.Cleanup();
            
            var statsAfter = ThemeManager.GetStatistics();
            int removed = statsBefore.Dead;
            
            string message = $"🗑️ 清理完成\n\n" +
                           $"清理前: {statsBefore.Total} 個 (存活: {statsBefore.Alive}, 失效: {statsBefore.Dead})\n" +
                           $"清理後: {statsAfter.Total} 個 (存活: {statsAfter.Alive}, 失效: {statsAfter.Dead})\n\n" +
                           $"已移除 {removed} 個失效參考";
            
            CyberMessageBox.Show(
                message,
                "清理完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            
            ComplianceContext.LogSystem(
                $"[Theme Cleanup] Removed {removed} dead references",
                Stackdose.UI.Core.Models.LogLevel.Success,
                showInUi: true
            );
        }

        /// <summary>
        /// 搜尋視覺樹中的 CyberFrame
        /// </summary>
        private CyberFrame? FindCyberFrame(DependencyObject parent)
        {
            if (parent is CyberFrame cyberFrame)
                return cyberFrame;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                var result = FindCyberFrame(child);
                if (result != null)
                    return result;
            }

            return null;
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
            
            // 🔥 新增：關閉合規引擎並刷新所有待寫入日誌
            ComplianceContext.Shutdown();
            
            #if DEBUG
            // 顯示批次寫入統計資訊
            var stats = ComplianceContext.GetBatchStatistics();
            System.Diagnostics.Debug.WriteLine("========== Compliance Context Statistics ==========");
            System.Diagnostics.Debug.WriteLine($"Total DataLogs Written: {stats.DataLogs}");
            System.Diagnostics.Debug.WriteLine($"Total AuditLogs Written: {stats.AuditLogs}");
            System.Diagnostics.Debug.WriteLine($"Total Batch Flushes: {stats.BatchFlushes}");
            System.Diagnostics.Debug.WriteLine($"Pending DataLogs: {stats.PendingDataLogs}");
            System.Diagnostics.Debug.WriteLine($"Pending AuditLogs: {stats.PendingAuditLogs}");
            System.Diagnostics.Debug.WriteLine("===================================================");
            #endif
        }
    }
}