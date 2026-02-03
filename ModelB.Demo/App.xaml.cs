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
            System.Diagnostics.Debug.WriteLine("[ModelB.Demo] SecurityContext initialized");

            // Check startup arguments
            if (e.Args.Length > 0)
            {
                // /test - Launch test window
                if (e.Args[0] == "/test")
                {
                    System.Diagnostics.Debug.WriteLine("[ModelB.Demo] ========== TEST MODE ==========");
                    var testWindow = new UserManagerTest();
                    testWindow.Show();
                    return;
                }

                // /generate-fda-data - Generate FDA test data directly
                if (e.Args[0] == "/generate-fda-data")
                {
                    System.Diagnostics.Debug.WriteLine("[ModelB.Demo] ========== GENERATE FDA TEST DATA ==========");
                    GenerateFdaTestData();
                    return;
                }

                // /fda-generator - Open FDA Test Data Generator window
                if (e.Args[0] == "/fda-generator")
                {
                    System.Diagnostics.Debug.WriteLine("[ModelB.Demo] ========== FDA GENERATOR WINDOW ==========");
                    var fdaWindow = new FdaTestDataWindow();
                    fdaWindow.ShowDialog();
                    Shutdown();
                    return;
                }
            }

            // Splash Screen removed - MainWindow will show directly via StartupUri
        }

        /// <summary>
        /// Generate FDA 21 CFR Part 11 compliant test data
        /// </summary>
        private void GenerateFdaTestData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ModelB.Demo] Starting FDA test data generation...");
                
                // Generate Event Logs and Periodic Data Logs
                FdaLogDataGenerator.GenerateCompleteFdaTestData(
                    eventLogCount: 100,
                    periodicDataCount: 500,
                    daysBack: 7
                );

                System.Diagnostics.Debug.WriteLine("[ModelB.Demo] FDA test data generated successfully");
                
                MessageBox.Show(
                    "FDA Test Data Generated!\n\nEvent Logs: 100\nPeriodic Data Logs: 500\n\nPlease restart the application to view in Log Viewer.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelB.Demo] FDA test data generation failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ModelB.Demo] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Failed to generate test data:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush all pending logs before exit
            ComplianceContext.Shutdown();

            // Log application exit
            System.Diagnostics.Debug.WriteLine("[ModelB.Demo] Application exiting");

            base.OnExit(e);
        }
    }
}
