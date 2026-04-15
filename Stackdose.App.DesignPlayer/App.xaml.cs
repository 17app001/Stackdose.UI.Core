using Stackdose.App.DesignPlayer.Models;
using Stackdose.App.DesignPlayer.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Templates.Helpers;
using System.IO;
using System.Windows;

namespace Stackdose.App.DesignPlayer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        base.OnStartup(e);

        // 載入設定
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "app-config.json");
        var config = PlayerConfigLoader.Load(configPath);

        // LoginRequired = true 才顯示登入視窗
        if (config.LoginRequired)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            bool ok = LoginDialog.ShowLoginDialog();
            if (!ok) { Shutdown(); return; }
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        var window = new MainWindow(configPath, config);
        MainWindow = window;
        window.Show();
    }
}
