using Stackdose.App.DeviceFramework.Models;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用設備頁面管理服務 — 快取已建立的頁面，避免重複建立。
/// 透過 PageFactory 委派讓 App 層決定要建立什麼頁面。
/// </summary>
public sealed class DevicePageService
{
    private readonly Dictionary<string, UserControl> _devicePages = new(StringComparer.OrdinalIgnoreCase);
    private readonly RuntimeMapper _runtimeMapper;

    /// <summary>
    /// 頁面工廠委派 — App 層注入，用來建立設備頁面實例。
    /// </summary>
    public Func<DeviceContext, UserControl>? PageFactory { get; set; }

    /// <summary>
    /// 套用 DeviceContext 到頁面的委派 — App 層注入。
    /// </summary>
    public Action<UserControl, DeviceContext>? ApplyContextAction { get; set; }

    public string? SelectedMachineId { get; private set; }

    public DevicePageService(RuntimeMapper runtimeMapper)
    {
        _runtimeMapper = runtimeMapper;
    }

    public string? GetCurrentOrFirstMachineId(IReadOnlyDictionary<string, MachineConfig> machines)
    {
        if (!string.IsNullOrWhiteSpace(SelectedMachineId) && machines.ContainsKey(SelectedMachineId))
            return SelectedMachineId;

        return machines.Keys.FirstOrDefault();
    }

    public bool TryGetDetailPage(
        string machineId,
        IReadOnlyDictionary<string, MachineConfig> machines,
        out UserControl? page,
        out string machineName)
    {
        page = null;
        machineName = string.Empty;

        if (!machines.TryGetValue(machineId, out var config))
            return false;

        SelectedMachineId = machineId;

        if (!_devicePages.TryGetValue(machineId, out var devicePage))
        {
            var context = _runtimeMapper.CreateDeviceContext(config);

            if (PageFactory != null)
            {
                devicePage = PageFactory(context);
            }
            else
            {
                // 無工廠時不建立頁面
                return false;
            }

            _devicePages[machineId] = devicePage;
        }
        else
        {
            // 頁面已存在，重新套用 context
            var context = _runtimeMapper.CreateDeviceContext(config);
            ApplyContextAction?.Invoke(devicePage, context);
        }

        page = devicePage;
        machineName = config.Machine.Name;
        return true;
    }

    public void Clear()
    {
        _devicePages.Clear();
        SelectedMachineId = null;
    }
}
