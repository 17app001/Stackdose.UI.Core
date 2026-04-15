using Stackdose.App.DesignPlayer.Models;
using Stackdose.App.DesignPlayer.Pages;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Templates.Controls;
using Stackdose.UI.Templates.Pages;
using System.Collections.ObjectModel;
using System.Windows;

namespace Stackdose.App.DesignPlayer;

public partial class MainWindow : Window
{
    private readonly PlayerAppConfig _config;
    private readonly MonitorPage     _monitorPage;
    private readonly UserManagementPage _userPage = new();

    public MainWindow(PlayerAppConfig config)
    {
        _config = config;
        InitializeComponent();

        // ── Shell 設定 ──────────────────────────────────────────────
        MainShell.HeaderDeviceName = config.HeaderDeviceName;
        MainShell.PageTitle        = config.AppTitle;
        Title                      = config.AppTitle;

        // ── 監控頁面 ────────────────────────────────────────────────
        _monitorPage = new MonitorPage(config);
        MainShell.ShellContent = _monitorPage;

        // ── 導航項目 ────────────────────────────────────────────────
        // Title = 左側選單顯示文字，NavigationTarget = 路由 key
        MainShell.NavigationItems = new ObservableCollection<NavigationItem>
        {
            new() { Title = "Main View",        NavigationTarget = "monitor",  IsSelected = true,  RequiredLevel = Stackdose.UI.Core.Models.AccessLevel.Guest },
            new() { Title = "User Management",  NavigationTarget = "users",                        RequiredLevel = Stackdose.UI.Core.Models.AccessLevel.Operator },
        };

        MainShell.NavigationRequested += OnNavigate;

        // ── 關閉 ────────────────────────────────────────────────────
        Closing += (_, _) =>
        {
            _monitorPage.DisposePlc();
            ComplianceContext.Shutdown();
        };
    }

    // ── 導航處理 ──────────────────────────────────────────────────────────

    private void OnNavigate(object? sender, string target)
    {
        switch (target)
        {
            case "monitor":
                MainShell.ShellContent = _monitorPage;
                MainShell.PageTitle    = _config.AppTitle;
                break;

            case "users":
                MainShell.ShellContent = _userPage;
                MainShell.PageTitle    = "User Management";
                break;
        }
    }
}
