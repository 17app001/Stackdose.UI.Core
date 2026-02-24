using Stackdose.UI.Core.Controls;
using Stackdose.UI.Templates.Helpers;
using System.Windows;

namespace Stackdose.App.UbiDemo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        base.OnStartup(e);

        // 防止 LoginDialog 關閉時 Application 自動結束
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        bool loginSuccess = LoginDialog.ShowLoginDialog();
        if (!loginSuccess)
        {
            Shutdown();
            return;
        }

        // 登入成功，改回正常關閉模式，開啟主視窗
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}


