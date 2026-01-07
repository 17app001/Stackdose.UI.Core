using Stackdose.Hardware.Plc;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 🔥 在靜態建構函數中設定，確保最早執行
        static App()
        {
            #if DEBUG
            PlcClientFactory.UseSimulator = true;
            System.Diagnostics.Debug.WriteLine("🤖 [App.Static] 開發模式：已啟用 PLC 模擬器");
            #endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            #if DEBUG
            // 再次確認（雙重保險）
            PlcClientFactory.UseSimulator = true;
            System.Diagnostics.Debug.WriteLine("🤖 [App.OnStartup] 開發模式：已啟用 PLC 模擬器");
            #endif

            // 🔥 1. 初始化資料庫並建立預設 Admin 帳號
            var userService = new UserManagementService();
            System.Diagnostics.Debug.WriteLine("[App] UserManagementService initialized");

            // 🔥 2. 提前登入 Admin（在任何 UI 載入之前）
            SecurityContext.QuickLogin(AccessLevel.Admin);
            System.Diagnostics.Debug.WriteLine($"[App] QuickLogin executed: {SecurityContext.CurrentSession.CurrentUserName}");

            // 🔥 3. 初始化 ComplianceContext
            ComplianceContext.LogSystem("========== Application Starting ==========", LogLevel.Info);

            base.OnStartup(e);
        }
    }
}
