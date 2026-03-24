using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stackdose.App.UbiDemo.Services;

/// <summary>
/// Ubi 專用的 IRuntimeMappingAdapter 實作，
/// 繼承框架預設邏輯並加入 Ubi 特有的 fallback 規則。
/// </summary>
internal sealed class UbiFrameworkMappingAdapter : DefaultRuntimeMappingAdapter
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public override string GetAlarmConfigFile(MachineConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.AlarmConfigFile))
        {
            return Path.Combine(AppContext.BaseDirectory, config.AlarmConfigFile.Replace('/', Path.DirectorySeparatorChar));
        }

        // Ubi fallback: M1 → MachineA, M2 → MachineB
        var relativePath = config.Machine.Id.ToUpperInvariant() switch
        {
            "M1" => "Config/MachineA/alarms.json",
            "M2" => "Config/MachineB/alarms.json",
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public override string GetSensorConfigFile(MachineConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.SensorConfigFile))
        {
            return Path.Combine(AppContext.BaseDirectory, config.SensorConfigFile.Replace('/', Path.DirectorySeparatorChar));
        }

        var relativePath = config.Machine.Id.ToUpperInvariant() switch
        {
            "M1" => "Config/MachineA/sensors.json",
            "M2" => "Config/MachineB/sensors.json",
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public override IReadOnlyList<string> GetPrintHeadConfigFiles(MachineConfig config)
    {
        if (config.PrintHeadConfigs.Count > 0)
        {
            return config.PrintHeadConfigs
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.Combine(AppContext.BaseDirectory, path.Replace('/', Path.DirectorySeparatorChar)))
                .ToList();
        }

        var fallback = config.Machine.Id.ToUpperInvariant() switch
        {
            "M1" => new[] { "Config/MachineA/feiyang_head1.json", "Config/MachineA/feiyang_head2.json" },
            "M2" => new[] { "Config/MachineB/feiyang_head1.json", "Config/MachineB/feiyang_head2.json" },
            _ => Array.Empty<string>()
        };

        return fallback
            .Select(p => Path.Combine(AppContext.BaseDirectory, p.Replace('/', Path.DirectorySeparatorChar)))
            .ToList();
    }

    public override IEnumerable<string> GetDetailLabelAddresses(IEnumerable<MachineConfig> configs)
    {
        var defaultAddresses = new[] { "D3400", "D33", "D3401", "D32", "D510", "D512", "D85", "D120", "D86", "D87" };

        foreach (var config in configs)
        {
            if (config.DetailLabels.Count == 0)
            {
                foreach (var address in defaultAddresses)
                {
                    yield return address;
                }

                continue;
            }

            foreach (var address in config.DetailLabels.Values)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    yield return address.Trim();
                }
            }
        }
    }
}
