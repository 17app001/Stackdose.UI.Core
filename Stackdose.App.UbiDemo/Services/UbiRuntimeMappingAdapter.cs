using Stackdose.App.UbiDemo.Models;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiRuntimeMappingAdapter : IUbiRuntimeMappingAdapter
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public string GetTagAddress(UbiMachineConfig config, string section, string key)
    {
        Dictionary<string, UbiTagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status" => config.Tags.Status,
            "process" => config.Tags.Process,
            _ => null
        };

        if (tags is null || !tags.TryGetValue(key, out var tag) || string.IsNullOrWhiteSpace(tag.Address))
        {
            return "--";
        }

        return tag.Address;
    }

    public string GetDetailLabelAddress(UbiMachineConfig config, string key, string fallback)
    {
        if (config.DetailLabels.TryGetValue(key, out var address) && !string.IsNullOrWhiteSpace(address))
        {
            return address.Trim();
        }

        return fallback;
    }

    public string GetAlarmConfigFile(UbiMachineConfig config)
    {
        var relativePath = !string.IsNullOrWhiteSpace(config.AlarmConfigFile)
            ? config.AlarmConfigFile
            : config.Machine.Id.ToUpperInvariant() switch
            {
                "M1" => "Config/MachineA/alarms.json",
                "M2" => "Config/MachineB/alarms.json",
                _ => string.Empty
            };

        return string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public string GetSensorConfigFile(UbiMachineConfig config)
    {
        var relativePath = !string.IsNullOrWhiteSpace(config.SensorConfigFile)
            ? config.SensorConfigFile
            : config.Machine.Id.ToUpperInvariant() switch
            {
                "M1" => "Config/MachineA/sensors.json",
                "M2" => "Config/MachineB/sensors.json",
                _ => string.Empty
            };

        return string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public IReadOnlyList<string> GetPrintHeadConfigFiles(UbiMachineConfig config)
    {
        if (config.PrintHeadConfigs.Count > 0)
        {
            return config.PrintHeadConfigs
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(ToAbsoluteConfigPath)
                .ToList();
        }

        var fallback = config.Machine.Id.ToUpperInvariant() switch
        {
            "M1" => new[] { "Config/MachineA/feiyang_head1.json", "Config/MachineA/feiyang_head2.json" },
            "M2" => new[] { "Config/MachineB/feiyang_head1.json", "Config/MachineB/feiyang_head2.json" },
            _ => []
        };

        return fallback.Select(ToAbsoluteConfigPath).ToList();
    }

    public IEnumerable<string> GetDetailLabelAddresses(IEnumerable<UbiMachineConfig> configs)
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

    public IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var entry in config.Plc.MonitorAddresses)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                var tokens = entry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (var i = 0; i < tokens.Length; i++)
                {
                    var parsed = ParseAddress(tokens[i]);
                    if (parsed == null)
                    {
                        continue;
                    }

                    var (prefix, start) = parsed.Value;
                    var length = 1;

                    if (i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out var parsedLength) && parsedLength > 1)
                    {
                        length = parsedLength;
                        i++;
                    }

                    for (var offset = 0; offset < length; offset++)
                    {
                        yield return $"{prefix}{start + offset}";
                    }
                }
            }
        }
    }

    public IEnumerable<string> GetMachineAlertAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var address in ReadAddressesFromSensorFile(GetSensorConfigFile(config)))
            {
                yield return address;
            }

            foreach (var address in ReadAddressesFromAlarmFile(GetAlarmConfigFile(config)))
            {
                yield return address;
            }
        }
    }

    public IEnumerable<(string Device, int Bit)> LoadAlarmBitPoints(UbiMachineConfig config)
    {
        var alarmConfigPath = GetAlarmConfigFile(config);
        if (string.IsNullOrWhiteSpace(alarmConfigPath) || !File.Exists(alarmConfigPath))
        {
            return [];
        }

        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(alarmConfigPath));
            if (!json.RootElement.TryGetProperty("Alarms", out var alarms) || alarms.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var points = new List<(string Device, int Bit)>();
            foreach (var item in alarms.EnumerateArray())
            {
                if (!item.TryGetProperty("Device", out var deviceProp) || deviceProp.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (!item.TryGetProperty("Bit", out var bitProp) || !bitProp.TryGetInt32(out var bit))
                {
                    continue;
                }

                var device = deviceProp.GetString();
                if (!string.IsNullOrWhiteSpace(device))
                {
                    points.Add((device.Trim().ToUpperInvariant(), bit));
                }
            }

            return points;
        }
        catch
        {
            return [];
        }
    }

    private static string ToAbsoluteConfigPath(string relativePath)
        => Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }

    private static IEnumerable<string> ReadAddressesFromSensorFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("Device", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    var device = d.GetString();
                    if (!string.IsNullOrWhiteSpace(device))
                    {
                        addresses.Add(device.Trim().ToUpperInvariant());
                    }
                }
            }

            return addresses;
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> ReadAddressesFromAlarmFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            if (!doc.RootElement.TryGetProperty("Alarms", out var alarms) || alarms.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in alarms.EnumerateArray())
            {
                if (item.TryGetProperty("Device", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    var device = d.GetString();
                    if (!string.IsNullOrWhiteSpace(device))
                    {
                        addresses.Add(device.Trim().ToUpperInvariant());
                    }
                }
            }

            return addresses;
        }
        catch
        {
            return [];
        }
    }
}
