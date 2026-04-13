using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Collections;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Shell 協調器 — 控制 MainContainer 的頁面切換與標題。
/// </summary>
public sealed class ShellCoordinator
{
    private readonly MainContainer _shell;
    private readonly string _overviewTitle;

    public ShellCoordinator(MainContainer shell, string overviewTitle)
    {
        _shell = shell;
        _overviewTitle = overviewTitle;
    }

    public void SetMachineOptions(IEnumerable options)
    {
        _shell.MachineOptions = options;
    }

    public void SelectNavigation(string navigationTarget)
    {
        _shell.SelectNavigationTarget(navigationTarget);
    }

    public void ShowOverview(object overviewPage, string? machineDisplayName)
    {
        _shell.ShellContent = overviewPage;
        _shell.PageTitle = _overviewTitle;
        _shell.SelectNavigationTarget(ShellNavigationTargets.Overview);

        if (!string.IsNullOrWhiteSpace(machineDisplayName))
            _shell.CurrentMachineDisplayName = machineDisplayName;
    }

    public void ShowDevicePage(UserControl devicePage, string machineId, string machineName)
    {
        _shell.ShellContent = devicePage;
        _shell.CurrentMachineDisplayName = machineName;
        _shell.SelectedMachineId = machineId;
        _shell.PageTitle = "Machine Detail";
        _shell.SelectNavigationTarget(ShellNavigationTargets.Detail);
    }

    public void ShowLogViewer(LogViewerPage page)
    {
        _shell.ShellContent = page;
        _shell.PageTitle = "Log Viewer";
        _shell.SelectNavigationTarget(ShellNavigationTargets.LogViewer);
    }

    public void ShowUserManagement(UserManagementPage page)
    {
        _shell.ShellContent = page;
        _shell.PageTitle = "User Management";
        _shell.SelectNavigationTarget(ShellNavigationTargets.UserManagement);
    }

    public void ShowSettings(UserControl page)
    {
        _shell.ShellContent = page;
        _shell.PageTitle = "Maintenance Mode";
        _shell.SelectNavigationTarget(ShellNavigationTargets.Settings);
    }
}
