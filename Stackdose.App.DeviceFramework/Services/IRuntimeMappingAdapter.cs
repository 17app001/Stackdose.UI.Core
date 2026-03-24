using Stackdose.App.DeviceFramework.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 位址映射適配器介面 — 將 MachineConfig 轉換為具體的 PLC 位址。
/// 不同設備可提供自己的實作。
/// </summary>
public interface IRuntimeMappingAdapter
{
    string GetTagAddress(MachineConfig config, string section, string key);
    string GetDetailLabelAddress(MachineConfig config, string key, string fallback);
    string GetAlarmConfigFile(MachineConfig config);
    string GetSensorConfigFile(MachineConfig config);
    IReadOnlyList<string> GetPrintHeadConfigFiles(MachineConfig config);
    IEnumerable<string> GetDetailLabelAddresses(IEnumerable<MachineConfig> configs);
    IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<MachineConfig> configs);
    IEnumerable<string> GetMachineAlertAddresses(IEnumerable<MachineConfig> configs);
    IEnumerable<(string Device, int Bit)> LoadAlarmBitPoints(MachineConfig config);
}
