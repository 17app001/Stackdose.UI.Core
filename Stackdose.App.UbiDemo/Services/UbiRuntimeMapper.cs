using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeMapper
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public static void ApplyMeta(MachineOverviewPage page, UbiAppMeta meta)
    {
        page.ShowMachineCards = meta.ShowMachineCards;
        page.ShowSoftwareInfo = meta.ShowSoftwareInfo;
        page.ShowLiveLog = meta.ShowLiveLog;
        page.BottomPanelHeight = new GridLength(meta.BottomPanelHeight);
        page.BottomLeftTitle = meta.BottomLeftTitle;
        page.BottomRightTitle = meta.BottomRightTitle;

        page.SoftwareInfoItems =
        [
            .. meta.SoftwareInfoItems.Select(item => new OverviewInfoItem(item.Label, item.Value))
        ];
    }

    public static void BindOverview(MachineOverviewPage page, IReadOnlyList<UbiMachineConfig> configs)
    {
        if (configs.Count == 0)
        {
            page.MachineCards = [];
            page.PlcMonitorAddresses = string.Empty;
            return;
        }

        var first = configs[0];
        page.PlcIpAddress = first.Plc.Ip;
        page.PlcPort = first.Plc.Port;
        page.PlcScanInterval = first.Plc.PollIntervalMs;
        page.PlcAutoConnect = first.Plc.AutoConnect;
        page.PlcMonitorAddresses = BuildMonitorAddresses(configs);
        page.MachineCards = [.. configs.Select(CreateCard)];
    }

    public static string GetTagAddress(UbiMachineConfig config, string section, string key)
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

    public static string GetDetailLabelAddress(UbiMachineConfig config, string key, string fallback)
    {
        if (config.DetailLabels.TryGetValue(key, out var address) && !string.IsNullOrWhiteSpace(address))
        {
            return address.Trim();
        }

        return fallback;
    }

    public static string GetAlarmConfigFile(UbiMachineConfig config)
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

    public static string GetSensorConfigFile(UbiMachineConfig config)
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

    public static IReadOnlyList<string> GetPrintHeadConfigFiles(UbiMachineConfig config)
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

    private static string ToAbsoluteConfigPath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static MachineOverviewCard CreateCard(UbiMachineConfig config)
    {
        return new MachineOverviewCard
        {
            MachineId = config.Machine.Id,
            Title = config.Machine.Name,
            BatchValue = "--",
            RecipeText = "--",
            StatusText = "Idle",
            StatusBrush = Brushes.SlateGray,
            LeftTopLabel = "Heartbeat",
            LeftTopValue = "0",
            LeftBottomLabel = "Alarm",
            LeftBottomValue = "0",
            RightTopLabel = "Nozzle",
            RightTopValue = "--",
            RightBottomLabel = "Mode",
            RightBottomValue = "Manual"
        };
    }

    private static string BuildMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        var addresses = configs
            .SelectMany(config => config.Tags.Status.Values.Concat(config.Tags.Process.Values))
            .Where(tag => string.Equals(tag.Access, "read", System.StringComparison.OrdinalIgnoreCase))
            .SelectMany(ExpandAddresses)
            .Concat(GetManualPlcMonitorAddresses(configs))
            .Concat(GetUbiDevicePageDetailLabelAddresses(configs))
            .Concat(GetMachineAlertAddresses(configs))
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        var parsed = addresses
            .Select(ParseAddress)
            .Where(x => x != null)
            .Cast<(string Prefix, int Number)>()
            .OrderBy(x => x.Prefix)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < parsed.Count)
        {
            var start = parsed[i];
            var end = i;

            while (end + 1 < parsed.Count
                   && parsed[end + 1].Prefix == start.Prefix
                   && parsed[end + 1].Number == parsed[end].Number + 1)
            {
                end++;
            }

            var length = end - i + 1;
            groups.Add(length > 1 ? $"{start.Prefix}{start.Number},{length}" : $"{start.Prefix}{start.Number}");
            i = end + 1;
        }

        return string.Join(",", groups);
    }

    private static IEnumerable<string> GetUbiDevicePageDetailLabelAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        var defaultAddresses = new[]
        {
            "D3400", "D33", "D3401", "D32", "D510", "D512", "D85", "D120", "D86", "D87"
        };

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

    private static IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
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
                    var token = tokens[i];
                    var parsed = ParseAddress(token);
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

    private static IEnumerable<string> GetMachineAlertAddresses(IEnumerable<UbiMachineConfig> configs)
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
                if (item.TryGetProperty("Device", out var deviceProp) && deviceProp.ValueKind == JsonValueKind.String)
                {
                    var device = deviceProp.GetString();
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
                if (item.TryGetProperty("Device", out var deviceProp) && deviceProp.ValueKind == JsonValueKind.String)
                {
                    var device = deviceProp.GetString();
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

    private static IEnumerable<string> ExpandAddresses(UbiTagConfig tag)
    {
        var parsed = ParseAddress(tag.Address);
        if (parsed == null)
        {
            yield break;
        }

        var (prefix, start) = parsed.Value;
        var span = tag.Type.Equals("string", System.StringComparison.OrdinalIgnoreCase) ? System.Math.Max(1, tag.Length) : 1;
        for (var i = 0; i < span; i++)
        {
            yield return $"{prefix}{start + i}";
        }
    }

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }
}
