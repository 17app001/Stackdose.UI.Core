using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Examples;
using Stackdose.UI.Core.Helpers;
using System.Diagnostics;
using System.Windows;
using WpfApp1.ViewModels;

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
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Constructor: Start");
                App.WriteLog("MainWindow: Constructor Start");
                #endif

                InitializeComponent();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Constructor: InitializeComponent completed");
                App.WriteLog("MainWindow: InitializeComponent completed");
                #endif

                // 設定 DataContext 為 ViewModel
                _viewModel = new MainViewModel();
                DataContext = _viewModel;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Constructor: ViewModel created");
                App.WriteLog("MainWindow: ViewModel created");
                #endif

                // 🔥 修改：只在 MainWindow 顯示後才訂閱事件（避免時序問題）
                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;
                this.Closed += MainWindow_Closed;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Constructor: Event handlers registered");
                App.WriteLog("MainWindow: Event handlers registered");
                #endif

                // 更新視窗標題
                UpdateWindowTitle();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Constructor: Completed successfully");
                App.WriteLog("MainWindow: Constructor Completed");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Constructor EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack Trace: {ex.StackTrace}");
                App.WriteLog($"MainWindow: Constructor EXCEPTION: {ex.Message}");
                App.WriteLog($"MainWindow: Stack Trace: {ex.StackTrace}");
                #endif

                MessageBox.Show(
                    $"MainWindow 初始化失敗 Initialization Failed\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Loaded event: Start");
                App.WriteLog("MainWindow: Loaded event Start");
                #endif

                // 訂閱登入/登出事件
                SecurityContext.LoginSuccess += OnLoginSuccess;
                SecurityContext.LogoutOccurred += OnLogoutOccurred;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Loaded event: Events subscribed");
                App.WriteLog("MainWindow: Events subscribed");
                #endif

                // 更新標題
                UpdateWindowTitle();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Loaded event: Completed");
                App.WriteLog("MainWindow: Loaded event Completed");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Loaded EXCEPTION: {ex.Message}");
                App.WriteLog($"MainWindow: Loaded EXCEPTION: {ex.Message}");
                #endif

                ComplianceContext.LogSystem(
                    $"[MainWindow] Loaded error: {ex.Message}",
                    Stackdose.UI.Core.Models.LogLevel.Error,
                    showInUi: true
                );
            }
        }

        // 🔥 新增：XAML 中的 Closing 事件處理
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #if DEBUG
            var stackTrace = new StackTrace(true);
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Window_Closing called from:");
            App.WriteLog("MainWindow: Window_Closing called from:");
            
            for (int i = 0; i < Math.Min(stackTrace.FrameCount, 10); i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                var logLine = $"  {i}: {method?.DeclaringType?.Name}.{method?.Name} at {frame?.GetFileName()}:{frame?.GetFileLineNumber()}";
                System.Diagnostics.Debug.WriteLine(logLine);
                App.WriteLog(logLine);
            }
            #endif
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Closing event: Start");
                App.WriteLog("MainWindow: Closing event Start");
                #endif

                // 取消訂閱事件
                SecurityContext.LoginSuccess -= OnLoginSuccess;
                SecurityContext.LogoutOccurred -= OnLogoutOccurred;

                // 清理 ViewModel
                _viewModel?.Cleanup();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[MainWindow] Closing event: Cleanup completed");
                App.WriteLog("MainWindow: Closing event Cleanup completed");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Closing EXCEPTION: {ex.Message}");
                App.WriteLog($"MainWindow: Closing EXCEPTION: {ex.Message}");
                #endif
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[MainWindow] Closed event: MainWindow has been closed");
            App.WriteLog("MainWindow: Closed event - Window has been closed");
            #endif
        }

        private void OnLoginSuccess(object? sender, Stackdose.UI.Core.Models.UserAccount user)
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    UpdateWindowTitle();
                });
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] OnLoginSuccess EXCEPTION: {ex.Message}");
                #endif
            }
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    UpdateWindowTitle();
                    
                    // 登出後顯示登入對話框
                    bool loginSuccess = LoginDialog.ShowLoginDialog();
                    if (!loginSuccess)
                    {
                        // 如果取消登入，以 Guest 身份留在首頁
                        SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Guest);
                        ComplianceContext.LogSystem(
                            "[Logout] User cancelled login, staying as Guest",
                            Stackdose.UI.Core.Models.LogLevel.Info,
                            showInUi: true
                        );
                    }
                });
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] OnLogoutOccurred EXCEPTION: {ex.Message}");
                #endif
            }
        }

        private void UpdateWindowTitle()
        {
            try
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
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainWindow] UpdateWindowTitle EXCEPTION: {ex.Message}");
                #endif
                
                this.Title = "Stackdose Control System";
            }
        }

        #region 主題測試按鈕事件

        public void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
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

        public void ShowThemeStats_Click(object sender, RoutedEventArgs e)
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
        }

        public void OpenThemeDemo_Click(object sender, RoutedEventArgs e)
        {
            var demoWindow = new ThemeManagerDemoWindow();
            demoWindow.Show();
        }

        public void PrintRegistered_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.PrintRegisteredControls();
            
            CyberMessageBox.Show(
                "已將已註冊控制項清單輸出到 Debug Console\n\n請查看 Visual Studio 的「輸出」視窗",
                "列印完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        public void CleanupTheme_Click(object sender, RoutedEventArgs e)
        {
            var statsBefore = ThemeManager.GetStatistics();
            ThemeManager.Cleanup();
            var statsAfter = ThemeManager.GetStatistics();
            int removed = statsBefore.Dead;
            
            string message = $"🗑️ 清理完成\n\n" +
                           $"清理前: {statsBefore.Total} 個\n" +
                           $"清理後: {statsAfter.Total} 個\n\n" +
                           $"已移除 {removed} 個失效參考";
            
            CyberMessageBox.Show(
                message,
                "清理完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

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

        #region Public Methods for Panels

        public void StartProcess_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem(
                "[START] 製程開始",
                Stackdose.UI.Core.Models.LogLevel.Info,
                showInUi: true
            );

            var plcManager = PlcContext.GlobalStatus?.CurrentManager;
            if (plcManager != null && plcManager.IsConnected)
            {
                _ = plcManager.WriteAsync("M100,1");
                
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
                CyberMessageBox.Show(
                    "[WARNING] PLC 未連線\n無法啟動製程",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            CyberMessageBox.Show(
                "✅ 製程已啟動",
                "製程開始",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        #endregion
    }
}