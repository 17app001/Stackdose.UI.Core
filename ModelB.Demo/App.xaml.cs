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

            // Enable SecurityContext auto-logout functionality
            SecurityContext.EnableAutoLogout = true;

            // Initialize UserManagementService (automatically creates default Admin account)
            var userService = new Stackdose.UI.Core.Services.UserManagementService();

            ComplianceContext.LogSystem("[ModelB.Demo] Application started", LogLevel.Info);

            // Splash Screen removed - MainWindow will show directly via StartupUri
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush all pending logs before exit
            ComplianceContext.Shutdown();

            base.OnExit(e);
        }
    }
}
