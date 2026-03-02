using Stackdose.App.UbiDemo.Models;

namespace Stackdose.App.UbiDemo.Services;

internal interface IUbiRuntimeMappingAdapter
{
    string GetTagAddress(UbiMachineConfig config, string section, string key);

    string GetDetailLabelAddress(UbiMachineConfig config, string key, string fallback);

    string GetAlarmConfigFile(UbiMachineConfig config);

    string GetSensorConfigFile(UbiMachineConfig config);

    IReadOnlyList<string> GetPrintHeadConfigFiles(UbiMachineConfig config);

    IEnumerable<string> GetDetailLabelAddresses(IEnumerable<UbiMachineConfig> configs);

    IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<UbiMachineConfig> configs);

    IEnumerable<string> GetMachineAlertAddresses(IEnumerable<UbiMachineConfig> configs);

    IEnumerable<(string Device, int Bit)> LoadAlarmBitPoints(UbiMachineConfig config);
}
