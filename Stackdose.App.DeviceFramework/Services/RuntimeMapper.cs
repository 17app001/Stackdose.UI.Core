using Stackdose.Abstractions.Hardware;
using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 RuntimeMapper — 統一管理 MappingAdapter + OverviewRuntimeMapper。
/// 子類別可覆寫 CreateDeviceContext 以客製化。
/// </summary>
public class RuntimeMapper
{
    private IRuntimeMappingAdapter _mappingAdapter;
    private readonly OverviewRuntimeMapper _overviewRuntimeMapper = new();

    public RuntimeMapper(IRuntimeMappingAdapter? adapter = null)
    {
        _mappingAdapter = adapter ?? new DefaultRuntimeMappingAdapter();
    }

    public IRuntimeMappingAdapter MappingAdapter => _mappingAdapter;

    public void ConfigureMappingAdapter(IRuntimeMappingAdapter? adapter)
    {
        _mappingAdapter = adapter ?? new DefaultRuntimeMappingAdapter();
    }

    public void BuildRuntimeMaps(IReadOnlyDictionary<string, MachineConfig> machines)
    {
        _overviewRuntimeMapper.BuildRuntimeMaps(machines, _mappingAdapter);
    }

    public void UpdateOverviewCards(IPlcManager manager, IReadOnlyList<MachineOverviewCard> cards)
    {
        _overviewRuntimeMapper.UpdateOverviewCards(manager, cards);
    }

    public virtual DeviceContext CreateDeviceContext(MachineConfig config)
    {
        return DeviceContextMapper.CreateDeviceContext(config, _mappingAdapter);
    }

    public void ApplyMeta(MachineOverviewPage page, AppMeta meta)
    {
        var result = OverviewMetaMapper.ApplyMeta(page, meta);
        _overviewRuntimeMapper.EnableOverviewAlarmCount = result.EnableOverviewAlarmCount;
    }

    public void BindOverview(MachineOverviewPage page, IReadOnlyList<MachineConfig> configs)
    {
        var monitorAddresses = MonitorAddressBuilder.Build(configs, _mappingAdapter);
        OverviewBindingMapper.BindOverview(page, configs, monitorAddresses);
    }

    public string GetTagAddress(MachineConfig config, string section, string key)
        => _mappingAdapter.GetTagAddress(config, section, key);

    public string GetAlarmConfigFile(MachineConfig config)
        => _mappingAdapter.GetAlarmConfigFile(config);

    public string GetSensorConfigFile(MachineConfig config)
        => _mappingAdapter.GetSensorConfigFile(config);

    public IReadOnlyList<string> GetPrintHeadConfigFiles(MachineConfig config)
        => _mappingAdapter.GetPrintHeadConfigFiles(config);
}
