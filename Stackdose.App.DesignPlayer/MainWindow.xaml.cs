using Stackdose.App.DesignPlayer.Models;
using Stackdose.App.DesignPlayer.Pages;
using Stackdose.App.DesignPlayer.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Templates.Controls;
using Stackdose.UI.Templates.Pages;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Stackdose.App.DesignPlayer;

public partial class MainWindow : Window
{
    private readonly string          _configPath;
    private PlayerAppConfig          _config;
    private readonly MonitorPage     _monitorPage;
    private readonly UserManagementPage _userPage = new();
    private readonly SettingsPage    _settingsPage;

    public MainWindow(string configPath, PlayerAppConfig config)
    {
        _configPath = configPath;
        _config     = config;
        InitializeComponent();

        // ── Shell 設定 ──────────────────────────────────────────────
        MainShell.HeaderDeviceName = config.HeaderDeviceName;
        MainShell.PageTitle        = config.AppTitle;
        Title                      = config.AppTitle;

        // ── 監控頁面 ────────────────────────────────────────────────
        _monitorPage = new MonitorPage(config);
        _monitorPage.DashboardModeActivated += OnDashboardModeActivated;
        MainShell.ShellContent = _monitorPage;

        // ── 設定頁面 ────────────────────────────────────────────────
        _settingsPage = new SettingsPage(configPath, config);
        _settingsPage.ApplyRequested += OnSettingsApplied;

        // ── 導航項目 ────────────────────────────────────────────────
        MainShell.NavigationItems = new ObservableCollection<NavigationItem>
        {
            new() { Title = "Main View",        NavigationTarget = "monitor",  IsSelected = true,  RequiredLevel = Stackdose.UI.Core.Models.AccessLevel.Guest },
            new() { Title = "Settings",         NavigationTarget = "settings",                     RequiredLevel = Stackdose.UI.Core.Models.AccessLevel.Operator },
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

            case "settings":
                MainShell.ShellContent = _settingsPage;
                MainShell.PageTitle    = "Settings";
                break;

            case "users":
                MainShell.ShellContent = _userPage;
                MainShell.PageTitle    = "User Management";
                break;
        }
    }

    // ── Dashboard 模式 ────────────────────────────────────────────────────

    private void OnDashboardModeActivated(object? sender, DashboardModeArgs args)
    {
        MainShell.Visibility     = Visibility.Collapsed;
        DashboardLayout.Visibility = Visibility.Visible;
        DashboardContent.Content  = _monitorPage;
        DashTopBar.DeviceName     = _config.HeaderDeviceName;

        WindowState = WindowState.Normal;
        ResizeMode  = ResizeMode.CanMinimize;
        Width       = args.CanvasWidth;
        Height      = args.CanvasHeight + 32;

        Left = (SystemParameters.PrimaryScreenWidth  - Width)  / 2;
        Top  = (SystemParameters.PrimaryScreenHeight - Height) / 2;
    }

    // ── 設定套用 ──────────────────────────────────────────────────────────

    private void OnSettingsApplied(object? sender, PlayerAppConfig newConfig)
    {
        _config = newConfig;
        MainShell.HeaderDeviceName = newConfig.HeaderDeviceName;
        Title = newConfig.AppTitle;
        // MonitorPage 下次重連或熱更新時即會使用新路徑；PLC 重連需重啟
    }
}
