using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Helpers;
using System.Windows;

namespace Stackdose.App.SinglePageDemo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        base.OnStartup(e);

        // Auto-login as SuperAdmin (no login dialog in SinglePage mode)
        SecurityContext.CurrentSession.CurrentUser = new UserAccount
        {
            UserId = "superadmin",
            DisplayName = "Super Admin",
            AccessLevel = AccessLevel.SuperAdmin
        };

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}