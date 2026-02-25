using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiShellPageService
{
    private readonly UbiShellCoordinator _shell;
    private readonly IShellNavigationService _navigation;
    private readonly MainContainer _mainShell;

    public UbiShellPageService(UbiShellCoordinator shell, IShellNavigationService navigation, MainContainer mainShell)
    {
        _shell = shell;
        _navigation = navigation;
        _mainShell = mainShell;
    }

    public void ShowOverview(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, UbiMachineConfig> machines, string? selectedMachineId, string defaultOverviewTitle)
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
        _mainShell.PageTitle = _navigation.GetTitle("MachineOverviewPage", fallbackTitle);
    }

    public void ShowMachineDetail(UbiDevicePage page, string machineId, string machineName)
    {
        _shell.ShowMachineDetail(page, machineId, machineName);
        _mainShell.PageTitle = _navigation.GetTitle("MachineDetailPage", _mainShell.PageTitle);
    }

    public void ShowLogViewer(LogViewerPage page)
    {
        _shell.ShowLogViewer(page);
        _mainShell.PageTitle = _navigation.GetTitle("LogViewerPage", _mainShell.PageTitle);
    }

    public void ShowUserManagement(UserManagementPage page)
    {
        _shell.ShowUserManagement(page);
        _mainShell.PageTitle = _navigation.GetTitle("UserManagementPage", _mainShell.PageTitle);
    }

    public void ShowSettings(SettingsPage page)
    {
        _shell.ShowSettings(page);
        _mainShell.PageTitle = _navigation.GetTitle("SettingsPage", _mainShell.PageTitle);
    }

    public void UpdateCurrentPageTitle(string defaultOverviewTitle)
    {
        var target = _mainShell.ShellContent switch
        {
            MachineOverviewPage => "MachineOverviewPage",
            UbiDevicePage => "MachineDetailPage",
            LogViewerPage => "LogViewerPage",
            UserManagementPage => "UserManagementPage",
            SettingsPage => "SettingsPage",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (string.Equals(target, "MachineOverviewPage", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = string.IsNullOrWhiteSpace(defaultOverviewTitle)
                ? _mainShell.PageTitle
                : defaultOverviewTitle;
            _mainShell.PageTitle = _navigation.GetTitle(target, fallback);
            return;
        }

        _mainShell.PageTitle = _navigation.GetTitle(target, _mainShell.PageTitle);
    }
}
