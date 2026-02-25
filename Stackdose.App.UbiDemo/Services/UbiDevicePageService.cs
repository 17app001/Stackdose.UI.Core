using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiDevicePageService
{
    private readonly Dictionary<string, UbiDevicePage> _devicePages = new(StringComparer.OrdinalIgnoreCase);

    public string? SelectedMachineId { get; private set; }

    public string? GetCurrentOrFirstMachineId(IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        if (!string.IsNullOrWhiteSpace(SelectedMachineId) && machines.ContainsKey(SelectedMachineId))
        {
            return SelectedMachineId;
        }

        return machines.Keys.FirstOrDefault();
    }

    public bool TryGetDetailPage(
        string machineId,
        IReadOnlyDictionary<string, UbiMachineConfig> machines,
        out UbiDevicePage? page,
        out string machineName)
    {
        page = null;
        machineName = string.Empty;

        if (!machines.TryGetValue(machineId, out var config))
        {
            return false;
        }

        SelectedMachineId = machineId;
        if (!_devicePages.TryGetValue(machineId, out var devicePage))
        {
            devicePage = new UbiDevicePage();
            _devicePages[machineId] = devicePage;
        }

        devicePage.SetDeviceContext(UbiRuntimeMapper.CreateDeviceContext(config));
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
