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

            // 🔥 強制初始化 ComplianceContext（觸發 SqliteLogger.Initialize）
            ComplianceContext.LogSystem("========== Application Starting ==========", Stackdose.UI.Core.Models.LogLevel.Info);
            
            // 🔥 診斷：顯示批次設定
            var stats = ComplianceContext.GetBatchStatistics();
            Console.WriteLine($"[MainWindow] Batch Statistics: Pending={stats.PendingDataLogs + stats.PendingAuditLogs}");
            
            // 顯示登入對話框（不使用快速登入）
            //bool loginSuccess = LoginDialog.ShowLoginDialog();

            //if (!loginSuccess)
            //{
            //    // 取消登入時預設為 Guest
            //    SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Guest);
            //}

            // 預設以 Admin 身份登入（測試用）
            SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Admin);

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
            
            // 🔥 測試：5 秒後自動觸發批次寫入
            Task.Run(async () =>
            {
                await Task.Delay(5000); // 等待 5 秒讓 UI 完全載入

                Console.WriteLine("========== 開始批次寫入測試 ==========");
                ComplianceContext.LogSystem("========== 開始批次寫入測試 ==========", Stackdose.UI.Core.Models.LogLevel.Warning);
                
                // 寫入 150 筆數據（會觸發批次刷新，因為預設 100 筆就刷新）
                for (int i = 0; i < 150; i++)
                {
                    ComplianceContext.LogDataHistory($"AutoTest_{i}", $"D{i}", i.ToString());
                    await Task.Delay(10);
                }
                
                Console.WriteLine("========== 批次寫入測試完成 ==========");
                ComplianceContext.LogSystem("========== 批次寫入測試完成 ==========", Stackdose.UI.Core.Models.LogLevel.Success);
            });
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

        #region 批次寫入測試

        /// <summary>
        /// 測試批次寫入（寫入 500 筆日誌）
        /// </summary>
        private void TestBatchWrite_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[TEST] 開始批次寫入測試...",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );

            Task.Run(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                
                for (int i = 0; i < 500; i++)
                {
                    // 模擬 PlcLabel 數據記錄
                    ComplianceContext.LogDataHistory($"TestLabel_{i % 10}", $"D{100 + i % 100}", i.ToString());
                    
                    // 每 10 筆記錄一次 Audit Trail
                    if (i % 10 == 0)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: $"TestDevice_{i}",
                            address: $"D{i}",
                            oldValue: i.ToString(),
                            newValue: (i + 1).ToString(),
                            reason: "Batch Write Test",
                            showInUi: false
                        );
                    }
                    
                    Thread.Sleep(10); // 模擬實際寫入間隔
                }
                
                sw.Stop();
                
                Dispatcher.Invoke(() =>
                {
                    var stats = ComplianceContext.GetBatchStatistics();
                    
                    ComplianceContext.LogSystem(
                        $"[TEST] 批次寫入測試完成！耗時: {sw.ElapsedMilliseconds}ms",
                        Stackdose.UI.Core.Models.LogLevel.Success,
                        showInUi: true
                    );
                    
                    CyberMessageBox.Show(
                        $"✅ 批次寫入測試完成\n\n" +
                        $"寫入 500 筆 DataLogs\n" +
                        $"寫入 50 筆 AuditLogs\n" +
                        $"總耗時: {sw.ElapsedMilliseconds}ms\n\n" +
                        $"統計資訊：\n" +
                        $"已寫入 DataLogs: {stats.DataLogs}\n" +
                        $"已寫入 AuditLogs: {stats.AuditLogs}\n" +
                        $"批次刷新次數: {stats.BatchFlushes}\n" +
                        $"待寫入 DataLogs: {stats.PendingDataLogs}\n" +
                        $"待寫入 AuditLogs: {stats.PendingAuditLogs}",
                        "批次寫入測試",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                });
            });
        }

        /// <summary>
        /// 手動刷新所有日誌
        /// </summary>
        private void FlushLogs_Click(object sender, RoutedEventArgs e)
        {
            var statsBefore = ComplianceContext.GetBatchStatistics();
            
            ComplianceContext.LogSystem(
                "[FLUSH] 手動刷新日誌...",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );
            
            ComplianceContext.FlushLogs();
            
            var statsAfter = ComplianceContext.GetBatchStatistics();
            
            ComplianceContext.LogSystem(
                $"[FLUSH] 刷新完成！寫入 {statsBefore.PendingDataLogs} 筆 DataLogs, {statsBefore.PendingAuditLogs} 筆 AuditLogs",
                Stackdose.UI.Core.Models.LogLevel.Success,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                $"✅ 日誌已刷新到資料庫\n\n" +
                $"刷新前待寫入：\n" +
                $"DataLogs: {statsBefore.PendingDataLogs}\n" +
                $"AuditLogs: {statsBefore.PendingAuditLogs}\n\n" +
                $"刷新後待寫入：\n" +
                $"DataLogs: {statsAfter.PendingDataLogs}\n" +
                $"AuditLogs: {statsAfter.PendingAuditLogs}",
                "手動刷新",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// 顯示批次寫入統計資訊
        /// </summary>
        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            var stats = ComplianceContext.GetBatchStatistics();
            
            CyberMessageBox.Show(
                $"📊 批次寫入統計資訊\n\n" +
                $"已寫入資料庫：\n" +
                $"  DataLogs: {stats.DataLogs:N0} 筆\n" +
                $"  AuditLogs: {stats.AuditLogs:N0} 筆\n" +
                $"  批次刷新次數: {stats.BatchFlushes:N0} 次\n\n" +
                $"待寫入佇列：\n" +
                $"  DataLogs: {stats.PendingDataLogs} 筆\n" +
                $"  AuditLogs: {stats.PendingAuditLogs} 筆\n\n" +
                $"💡 提示：\n" +
                $"- 超過 100 筆會自動刷新\n" +
                $"- 每 5 秒定時刷新\n" +
                $"- 關閉程式時自動刷新",
                "批次統計",
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