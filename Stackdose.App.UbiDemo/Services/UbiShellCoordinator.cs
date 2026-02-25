using Stackdose.App.UbiDemo.Pages;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Collections;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiShellCoordinator
{
    private readonly MainContainer _shell;
    private readonly string _overviewTitle;

    public UbiShellCoordinator(MainContainer shell, string overviewTitle)
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
        {
            _shell.CurrentMachineDisplayName = machineDisplayName;
        }
    }

    public void ShowMachineDetail(UbiDevicePage devicePage, string machineId, string machineName)
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

    public void ShowSettings(SettingsPage page)
    {
        _shell.ShellContent = page;
        _shell.PageTitle = "Maintenance Mode";
        _shell.SelectNavigationTarget(ShellNavigationTargets.Settings);
    }
}
