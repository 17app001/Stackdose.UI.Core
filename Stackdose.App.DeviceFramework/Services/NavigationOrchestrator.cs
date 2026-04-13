using Stackdose.UI.Templates.Pages;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用導航協調器 — 統一管理頁面切換邏輯。
/// </summary>
public sealed class NavigationOrchestrator
{
    private readonly DevicePageService _devicePages;
    private readonly ShellPageService _shellPages;
    private readonly LogViewerPage _logViewerPage;
    private readonly UserManagementPage _userManagementPage;
    private readonly UserControl _settingsPage;

    public NavigationOrchestrator(
        DevicePageService devicePages,
        ShellPageService shellPages,
        LogViewerPage logViewerPage,
        UserManagementPage userManagementPage,
        UserControl settingsPage)
    {
        _devicePages = devicePages;
        _shellPages = shellPages;
        _logViewerPage = logViewerPage;
        _userManagementPage = userManagementPage;
        _settingsPage = settingsPage;
    }

    public void ShowOverview(RuntimeContext runtime, MetaSnapshot snapshot)
    {
        _shellPages.ShowOverview(
            runtime.OverviewPage,
            runtime.Machines,
            _devicePages.SelectedMachineId,
            snapshot.Meta.DefaultPageTitle);
    }

    public bool ShowCurrentOrFirstMachineDetail(RuntimeContext runtime)
    {
        var targetId = _devicePages.GetCurrentOrFirstMachineId(runtime.Machines);
        if (string.IsNullOrWhiteSpace(targetId))
            return false;

        return ShowMachineDetail(runtime, targetId);
    }

    public bool ShowMachineDetail(RuntimeContext runtime, string machineId)
    {
        if (!_devicePages.TryGetDetailPage(machineId, runtime.Machines, out var devicePage, out var machineName)
            || devicePage is null)
            return false;

        _shellPages.ShowDevicePage(devicePage, machineId, machineName);
        return true;
    }

    public void ShowLogViewer()
    {
        _shellPages.ShowLogViewer(_logViewerPage);
    }

    public void ShowUserManagement()
    {
        _shellPages.ShowUserManagement(_userManagementPage);
    }

    public void ShowSettings()
    {
        _shellPages.ShowSettings(_settingsPage);
    }

    public void UpdateCurrentPageTitle(MetaSnapshot snapshot)
    {
        _shellPages.UpdateCurrentPageTitle(snapshot.Meta.DefaultPageTitle);
    }
}
