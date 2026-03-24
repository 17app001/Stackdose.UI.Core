using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Shell 頁面服務 — 管理頁面導航與標題同步。
/// </summary>
public sealed class ShellPageService
{
    private readonly ShellCoordinator _shell;
    private readonly IShellNavigationService _navigation;
    private readonly MainContainer _mainShell;

    public ShellPageService(ShellCoordinator shell, IShellNavigationService navigation, MainContainer mainShell)
    {
        _shell = shell;
        _navigation = navigation;
        _mainShell = mainShell;
    }

    public void ShowOverview(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, MachineConfig> machines, string? selectedMachineId, string defaultOverviewTitle)
    {
        var machineDisplayName = string.Empty;
        if (!string.IsNullOrWhiteSpace(selectedMachineId)
            && machines.TryGetValue(selectedMachineId, out var selectedMachine))
        {
            machineDisplayName = selectedMachine.Machine.Name;
        }

        _shell.ShowOverview(overviewPage, machineDisplayName);
        var fallbackTitle = string.IsNullOrWhiteSpace(defaultOverviewTitle)
            ? _mainShell.PageTitle
            : defaultOverviewTitle;
        _mainShell.PageTitle = _navigation.GetTitle(ShellNavigationTargets.Overview, fallbackTitle);
    }

    public void ShowDevicePage(UserControl page, string machineId, string machineName)
    {
        _shell.ShowDevicePage(page, machineId, machineName);
        _mainShell.PageTitle = _navigation.GetTitle(ShellNavigationTargets.Detail, _mainShell.PageTitle);
    }

    public void ShowLogViewer(LogViewerPage page)
    {
        _shell.ShowLogViewer(page);
        _mainShell.PageTitle = _navigation.GetTitle(ShellNavigationTargets.LogViewer, _mainShell.PageTitle);
    }

    public void ShowUserManagement(UserManagementPage page)
    {
        _shell.ShowUserManagement(page);
        _mainShell.PageTitle = _navigation.GetTitle(ShellNavigationTargets.UserManagement, _mainShell.PageTitle);
    }

    public void ShowSettings(UserControl page)
    {
        _shell.ShowSettings(page);
        _mainShell.PageTitle = _navigation.GetTitle(ShellNavigationTargets.Settings, _mainShell.PageTitle);
    }

    public void UpdateCurrentPageTitle(string defaultOverviewTitle)
    {
        var target = _mainShell.ShellContent switch
        {
            MachineOverviewPage => ShellNavigationTargets.Overview,
            LogViewerPage => ShellNavigationTargets.LogViewer,
            UserManagementPage => ShellNavigationTargets.UserManagement,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(target))
            return;

        if (string.Equals(target, ShellNavigationTargets.Overview, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = string.IsNullOrWhiteSpace(defaultOverviewTitle) ? _mainShell.PageTitle : defaultOverviewTitle;
            _mainShell.PageTitle = _navigation.GetTitle(target, fallback);
            return;
        }

        _mainShell.PageTitle = _navigation.GetTitle(target, _mainShell.PageTitle);
    }
}
