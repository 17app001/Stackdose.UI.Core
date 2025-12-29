using System.Windows;
using WpfApp1.ViewModels;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Controls;
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

            // 顯示登入對話框（不使用快速登入）
            //bool loginSuccess = LoginDialog.ShowLoginDialog();

            //if (!loginSuccess)
            //{
            //    // 取消登入時預設為 Guest
            //    SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Guest);
            //}

            // 預設以 Engineer 身份登入（測試用）
            SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Engineer);

            // 設定 DataContext 為 ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 訂閱登入/登出事件（更新 UI 標題）
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // ⭐ 訂閱 PLC 連線成功事件
            // MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;

            // ⭐ 初始化 Recipe 系統 (不自動載入，等待 PLC 連線)
            _ = InitializeRecipeSystemAsync();

            // 更新視窗標題
            UpdateWindowTitle();
        }

        /// <summary>
        /// PLC 連線成功事件處理
        /// </summary>
        private async void OnPlcConnectionEstablished(Stackdose.Abstractions.Hardware.IPlcManager plcManager)
        {
            ComplianceContext.LogSystem(
                "[MainWindow] OnPlcConnectionEstablished called!",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            ComplianceContext.LogSystem(
                "[PLC] Connection established, checking Recipe status...",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );

            // ⭐ 檢查是否已經下載過 Recipe
            if (_recipeDownloadedToPLC)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Recipe already downloaded to PLC, skipping re-download on reconnection.",
                    Stackdose.UI.Core.Models.LogLevel.Info,
                    showInUi: true
                );
                return;
            }

            // 如果 Recipe 還沒載入，自動載入
            if (!RecipeContext.HasActiveRecipe)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Auto-loading Recipe after PLC connection...",
                    Stackdose.UI.Core.Models.LogLevel.Info,
                    showInUi: true
                );

                bool success = await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
                
                if (success && RecipeContext.CurrentRecipe != null)
                {
                    // 自動下載 Recipe 到 PLC
                    int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
                    
                    if (downloadCount > 0)
                    {
                        _recipeDownloadedToPLC = true; // ⭐ 標記已下載
                        
                        ComplianceContext.LogSystem(
                            $"[Recipe] Auto-loaded and downloaded: {downloadCount} parameters written to PLC",
                            Stackdose.UI.Core.Models.LogLevel.Success,
                            showInUi: true
                        );
                    }
                }
            }
            else
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Recipe already loaded, downloading to PLC...",
                    Stackdose.UI.Core.Models.LogLevel.Info,
                    showInUi: true
                );
                
                // Recipe 已經載入，自動下載到 PLC
                int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
                
                if (downloadCount > 0)
                {
                    _recipeDownloadedToPLC = true; // ⭐ 標記已下載
                    
                    ComplianceContext.LogSystem(
                        $"[Recipe] Auto-downloaded to PLC: {downloadCount} parameters written",
                        Stackdose.UI.Core.Models.LogLevel.Success,
                        showInUi: true
                    );
                }
            }
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

        private void OnRecipeLoaded(object? sender, Stackdose.UI.Core.Models.Recipe recipe)
        {
            Dispatcher.Invoke(() =>
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] {recipe.RecipeName} 已載入,共 {recipe.EnabledItemCount} 項參數",
                    Stackdose.UI.Core.Models.LogLevel.Success,
                    showInUi: true
                );
            });
        }

        private void OnRecipeLoadFailed(object? sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] 載入失敗: {errorMessage}",
                    Stackdose.UI.Core.Models.LogLevel.Error,
                    showInUi: true
                );
            });
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