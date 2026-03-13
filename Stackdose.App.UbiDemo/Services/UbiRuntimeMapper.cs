using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeMapper
{
    private static IUbiRuntimeMappingAdapter _mappingAdapter = new UbiRuntimeMappingAdapter();
    private static readonly UbiOverviewRuntimeMapper _overviewRuntimeMapper = new();

    internal static void ConfigureMappingAdapter(IUbiRuntimeMappingAdapter? adapter)
    {
        _mappingAdapter = adapter ?? new UbiRuntimeMappingAdapter();
    }

    public static void BuildRuntimeMaps(IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        _overviewRuntimeMapper.BuildRuntimeMaps(machines, _mappingAdapter);
    }

    public static void UpdateOverviewCards(IPlcManager manager, IReadOnlyList<MachineOverviewCard> cards)
    {
        _overviewRuntimeMapper.UpdateOverviewCards(manager, cards);
    }

    public static DeviceContext CreateDeviceContext(UbiMachineConfig config)
    {
        return UbiDeviceContextMapper.CreateDeviceContext(config, _mappingAdapter);
    }

    public static void ApplyMeta(MachineOverviewPage page, UbiAppMeta meta)
    {
        var result = UbiOverviewMetaMapper.ApplyMeta(page, meta);
        _overviewRuntimeMapper.EnableOverviewAlarmCount = result.EnableOverviewAlarmCount;
    }

    public static void BindOverview(MachineOverviewPage page, IReadOnlyList<UbiMachineConfig> configs)
    {
        var monitorAddresses = UbiMonitorAddressBuilder.Build(configs, _mappingAdapter);
        UbiOverviewBindingMapper.BindOverview(page, configs, monitorAddresses);
    }

    public static string GetTagAddress(UbiMachineConfig config, string section, string key)
    {
        return _mappingAdapter.GetTagAddress(config, section, key);
    }

    public static string GetDetailLabelAddress(UbiMachineConfig config, string key, string fallback)
    {
        return _mappingAdapter.GetDetailLabelAddress(config, key, fallback);
    }

    public static string GetAlarmConfigFile(UbiMachineConfig config)
    {
        return _mappingAdapter.GetAlarmConfigFile(config);
    }

    public static string GetSensorConfigFile(UbiMachineConfig config)
    {
        return _mappingAdapter.GetSensorConfigFile(config);
    }

    public static IReadOnlyList<string> GetPrintHeadConfigFiles(UbiMachineConfig config)
    {
        return _mappingAdapter.GetPrintHeadConfigFiles(config);
    }
}
