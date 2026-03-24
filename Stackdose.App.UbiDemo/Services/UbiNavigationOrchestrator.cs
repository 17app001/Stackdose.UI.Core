using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiNavigationOrchestrator
{
    private readonly UbiDevicePageService _devicePages;
    private readonly UbiShellPageService _shellPages;
    private readonly LogViewerPage _logViewerPage;
    private readonly UserManagementPage _userManagementPage;
    private readonly SettingsPage _settingsPage;

    public UbiNavigationOrchestrator(
        UbiDevicePageService devicePages,
        UbiShellPageService shellPages,
        LogViewerPage logViewerPage,
        UserManagementPage userManagementPage,
        SettingsPage settingsPage)
    {
        _devicePages = devicePages;
        _shellPages = shellPages;
        _logViewerPage = logViewerPage;
        _userManagementPage = userManagementPage;
        _settingsPage = settingsPage;
    }

    public void ShowOverview(UbiRuntimeContext runtime, UbiMetaSnapshot snapshot)
    {
        _shellPages.ShowOverview(
            runtime.OverviewPage,
            runtime.Machines,
            _devicePages.SelectedMachineId,
            snapshot.Meta.DefaultPageTitle);
    }

    public bool ShowCurrentOrFirstMachineDetail(UbiRuntimeContext runtime)
    {
        var targetId = _devicePages.GetCurrentOrFirstMachineId(runtime.Machines);
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        return ShowMachineDetail(runtime, targetId);
    }

    public bool ShowMachineDetail(UbiRuntimeContext runtime, string machineId)
    {
        if (!_devicePages.TryGetDetailPage(machineId, runtime.Machines, out var devicePage, out var machineName)
            || devicePage is null)
        {
            return false;
        }

        _shellPages.ShowMachineDetail(devicePage, machineId, machineName);
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

    public void ShowSettings(UbiRuntimeContext runtime)
    {
        _settingsPage.SetMonitorAddresses(runtime.OverviewPage.PlcMonitorAddresses);

        var machineId = _devicePages.GetCurrentOrFirstMachineId(runtime.Machines);

        // 傳入所有機台讓使用者在 SettingsPage 上選擇操作目標（如 PrintHeadController）
        _settingsPage.SetMachines(runtime.Machines, runtime.ConfigDirectory, machineId);

        _shellPages.ShowSettings(_settingsPage);
    }

    public void UpdateCurrentPageTitle(UbiMetaSnapshot snapshot)
    {
        _shellPages.UpdateCurrentPageTitle(snapshot.Meta.DefaultPageTitle);
    }
}
