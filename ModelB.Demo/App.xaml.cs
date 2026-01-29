using System.Windows;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace ModelB.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize ComplianceContext (triggers database initialization)
            var _ = ComplianceContext.CurrentUser;

            // ?? 啟用 SecurityContext 自動登出功能
            SecurityContext.EnableAutoLogout = true;

            // ?? 初始化 UserManagementService（會自動建立預設 Admin 帳號）
            var userService = new Stackdose.UI.Core.Services.UserManagementService();

            ComplianceContext.LogSystem("[ModelB.Demo] Application started", LogLevel.Info);
            System.Diagnostics.Debug.WriteLine("[ModelB.Demo] SecurityContext initialized");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush all pending logs before exit
            ComplianceContext.Shutdown();

            // ?? 記錄應用程式關閉
            System.Diagnostics.Debug.WriteLine("[ModelB.Demo] Application exiting");

            base.OnExit(e);
        }
    }
}
