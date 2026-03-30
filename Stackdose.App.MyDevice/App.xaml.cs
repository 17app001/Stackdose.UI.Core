using Stackdose.UI.Core.Controls;
using Stackdose.UI.Templates.Helpers;
using System.Windows;

namespace Stackdose.App.MyDevice;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        bool loginSuccess = LoginDialog.ShowLoginDialog();
        if (!loginSuccess) { Shutdown(); return; }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}