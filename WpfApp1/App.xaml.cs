using Stackdose.Hardware.Plc;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;
using System.IO;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static StreamWriter? _logWriter;

        // 🔥 在靜態建構函數中設定，確保最早執行
        static App()
        {
            // 🔥 建立日誌檔案
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_startup.log");
                _logWriter = new StreamWriter(logPath, append: true);
                _logWriter.AutoFlush = true;
                WriteLog("========================================");
                WriteLog($"Application starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                WriteLog("========================================");
            }
            catch { }

            #if DEBUG
            PlcClientFactory.UseSimulator = true;
            WriteLog("DEBUG: PLC Simulator enabled");
            System.Diagnostics.Debug.WriteLine("🤖 [App.Static] 開發模式：已啟用 PLC 模擬器");
            #endif
        }

        // 🔥 公開 WriteLog 方法讓其他類別可以呼叫
        public static void WriteLog(string message)
        {
            try
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                System.Diagnostics.Debug.WriteLine($"[App] {message}");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            WriteLog("OnStartup: Called");
            
            // 🔥 設定 ShutdownMode，防止自動關閉
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            WriteLog("ShutdownMode set to OnExplicitShutdown");
            
            // 🔥 攔截所有未處理的例外
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                WriteLog($"UNHANDLED EXCEPTION: {ex?.Message}");
                WriteLog($"Stack Trace: {ex?.StackTrace}");
                
                MessageBox.Show(
                    $"應用程式發生嚴重錯誤 Fatal Error:\n\n{ex?.Message}\n\n詳細資訊已記錄到 app_startup.log",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            };

            this.DispatcherUnhandledException += (s, args) =>
            {
                WriteLog($"DISPATCHER EXCEPTION: {args.Exception.Message}");
                WriteLog($"Stack Trace: {args.Exception.StackTrace}");
                
                MessageBox.Show(
                    $"UI 執行緒發生錯誤 UI Thread Error:\n\n{args.Exception.Message}\n\n詳細資訊已記錄到 app_startup.log",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                args.Handled = true;
            };

            base.OnStartup(e);
            WriteLog("OnStartup: base.OnStartup called");

            #if DEBUG
            PlcClientFactory.UseSimulator = true;
            #endif

            try
            {
                // 🔥 1. 初始化資料庫並建立預設 Admin 帳號
                WriteLog("Initializing UserManagementService...");
                var userService = new UserManagementService();
                WriteLog("UserManagementService initialized");

                // 🔥 2. 初始化 ComplianceContext
                WriteLog("Initializing ComplianceContext...");
                ComplianceContext.LogSystem("========== Application Starting ==========", LogLevel.Info);
                WriteLog("ComplianceContext initialized");

                // 🔥 3. 提前預熱資料庫和靜態類別（觸發靜態建構子執行）
                WriteLog("Warming up static classes...");
                try
                {
                    // 觸發 SecurityContext 靜態初始化
                    _ = SecurityContext.CurrentSession;
                    WriteLog("SecurityContext warmed up");
                }
                catch (Exception warmupEx)
                {
                    WriteLog($"Warmup warning (non-critical): {warmupEx.Message}");
                }

                // 🔥 4. 顯示登入對話視窗（DEBUG 模式下已預填帳號密碼）
                WriteLog("Showing login dialog...");
                bool loginSuccess = false;
                
                try
                {
                    loginSuccess = LoginDialog.ShowLoginDialog();
                    WriteLog($"Login dialog closed - Success: {loginSuccess}");
                }
                catch (Exception dialogEx)
                {
                    WriteLog($"Login dialog exception: {dialogEx.Message}");
                    WriteLog($"Login dialog stack trace: {dialogEx.StackTrace}");
                    
                    MessageBox.Show(
                        $"登入對話框發生錯誤:\n\n{dialogEx.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    
                    this.Shutdown();
                    return;
                }

                if (!loginSuccess)
                {
                    WriteLog("Login cancelled or failed. Shutting down...");
                    ComplianceContext.LogSystem("Application startup cancelled (Login failed or cancelled)", LogLevel.Warning);
                    
                    this.Shutdown();
                    return;
                }

                // 🔥 5. 登入成功，記錄到日誌
                var currentUser = SecurityContext.CurrentSession.CurrentUser;
                
                WriteLog($"Login successful: {currentUser?.DisplayName} ({currentUser?.AccessLevel})");
                
                ComplianceContext.LogSystem(
                    $"User logged in: {currentUser?.DisplayName} ({currentUser?.AccessLevel})",
                    LogLevel.Success
                );

                // 🔥 6. 建立並顯示 MainWindow
                WriteLog("Creating MainWindow...");
                
                var mainWindow = new MainWindow();
                
                // 🔥 設定為應用程式的主視窗
                this.MainWindow = mainWindow;
                WriteLog("MainWindow set as Application.MainWindow");
                
                // 🔥 修改 ShutdownMode 為當主視窗關閉時才關閉應用程式
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                WriteLog("ShutdownMode changed to OnMainWindowClose");
                
                WriteLog("MainWindow created, calling Show()...");
                
                mainWindow.Show();
                
                WriteLog("MainWindow.Show() completed successfully!");
                WriteLog("MainWindow.IsVisible = " + mainWindow.IsVisible);
                WriteLog("MainWindow.IsLoaded = " + mainWindow.IsLoaded);
                WriteLog("========================================");
                WriteLog("Application startup COMPLETED");
                WriteLog("========================================");
            }
            catch (Exception ex)
            {
                WriteLog("========================================");
                WriteLog($"FATAL ERROR in OnStartup: {ex.Message}");
                WriteLog($"Exception Type: {ex.GetType().FullName}");
                WriteLog($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    WriteLog($"Inner Exception: {ex.InnerException.Message}");
                    WriteLog($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                WriteLog("========================================");

                MessageBox.Show(
                    $"應用程式啟動失敗 Application Startup Failed\n\n" +
                    $"錯誤: {ex.Message}\n\n" +
                    $"類型: {ex.GetType().Name}\n\n" +
                    $"詳細資訊已記錄到:\n{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_startup.log")}",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                this.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            WriteLog("OnExit: Application shutting down...");

            try
            {
                // 🔥 應用程式關閉時，刷新所有待寫入的日誌
                ComplianceContext.LogSystem("========== Application Shutting Down ==========", LogLevel.Info);
                ComplianceContext.FlushLogs();
                ComplianceContext.Shutdown();

                WriteLog("OnExit: Cleanup completed");
            }
            catch (Exception ex)
            {
                WriteLog($"OnExit: Error during cleanup: {ex.Message}");
            }
            
            base.OnExit(e);

            WriteLog("OnExit: Application exit completed");
            WriteLog("========================================");
            
            _logWriter?.Dispose();
        }
    }
}
