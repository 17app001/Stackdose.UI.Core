using Stackdose.App.DeviceFramework.Models;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 預設的位址映射適配器 — 直接從 MachineConfig 讀取。
/// </summary>
public class DefaultRuntimeMappingAdapter : IRuntimeMappingAdapter
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public virtual string GetTagAddress(MachineConfig config, string section, string key)
    {
        Dictionary<string, TagConfig>? tags = section.ToLowerInvariant() switch
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

    public virtual string GetDetailLabelAddress(MachineConfig config, string key, string fallback)
    {
        if (config.DetailLabels.TryGetValue(key, out var address) && !string.IsNullOrWhiteSpace(address))
        {
            return address.Trim();
        }

        return fallback;
    }

    public virtual string GetAlarmConfigFile(MachineConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.AlarmConfigFile))
        {
            return string.Empty;
        }

        return Path.Combine(AppContext.BaseDirectory, config.AlarmConfigFile.Replace('/', Path.DirectorySeparatorChar));
    }

    public virtual string GetSensorConfigFile(MachineConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SensorConfigFile))
        {
            return string.Empty;
        }

        return Path.Combine(AppContext.BaseDirectory, config.SensorConfigFile.Replace('/', Path.DirectorySeparatorChar));
    }

    public virtual IReadOnlyList<string> GetPrintHeadConfigFiles(MachineConfig config)
    {
        return config.PrintHeadConfigs
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.Combine(AppContext.BaseDirectory, path.Replace('/', Path.DirectorySeparatorChar)))
            .ToList();
    }

    public virtual IEnumerable<string> GetDetailLabelAddresses(IEnumerable<MachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var address in config.DetailLabels.Values)
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    yield return address.Trim();
                }
            }
        }
    }

    public virtual IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<MachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var entry in config.Plc.MonitorAddresses)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;

                var tokens = entry.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (var i = 0; i < tokens.Length; i++)
                {
                    var parsed = ParseAddress(tokens[i]);
                    if (parsed == null) continue;

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

    public virtual IEnumerable<string> GetMachineAlertAddresses(IEnumerable<MachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var address in ReadAddressesFromFile(GetSensorConfigFile(config), isAlarmFile: false))
                yield return address;

            foreach (var address in ReadAddressesFromFile(GetAlarmConfigFile(config), isAlarmFile: true))
                yield return address;
        }
    }

    public virtual IEnumerable<(string Device, int Bit)> LoadAlarmBitPoints(MachineConfig config)
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
                    continue;
                if (!item.TryGetProperty("Bit", out var bitProp) || !bitProp.TryGetInt32(out var bit))
                    continue;

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

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number))
            return null;

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }

    private static IEnumerable<string> ReadAddressesFromFile(string filePath, bool isAlarmFile)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            yield break;

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(File.ReadAllText(filePath));

            JsonElement root;
            if (isAlarmFile)
            {
                if (!doc.RootElement.TryGetProperty("Alarms", out root) || root.ValueKind != JsonValueKind.Array)
                    yield break;
            }
            else
            {
                root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                    yield break;
            }

            foreach (var item in root.EnumerateArray())
            {
                if (item.TryGetProperty("Device", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    var device = d.GetString();
                    if (!string.IsNullOrWhiteSpace(device))
                    {
                        yield return device.Trim().ToUpperInvariant();
                    }
                }
            }
        }
        finally
        {
            doc?.Dispose();
        }
    }
}
