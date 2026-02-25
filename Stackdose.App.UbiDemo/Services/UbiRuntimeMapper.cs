using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows.Media;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeMapper
{
    // �w�w Internal address map types �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w
    private sealed record AlarmBitPoint(string Device, int Bit);
    private sealed record OverviewAddressMap(
        string BatchAddress, string RecipeAddress,
        string NozzleAddress, string RunningAddress, string AlarmAddress);

    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    // �w�w Runtime state (held internally) �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w
    private static Dictionary<string, OverviewAddressMap> _cachedAddressMap = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, IReadOnlyList<AlarmBitPoint>> _cachedAlarmMap = new(StringComparer.OrdinalIgnoreCase);

    // �w�w Overview card PLC update �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w

    /// <summary>
    /// Builds and caches the internal address/alarm maps. Call once after config is loaded.
    /// </summary>
    public static void BuildRuntimeMaps(IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        _cachedAddressMap.Clear();
        _cachedAlarmMap.Clear();

        foreach (var (key, config) in machines)
        {
            _cachedAddressMap[key] = new OverviewAddressMap(
                GetTagAddress(config, "process", "batchNo"),
                GetTagAddress(config, "process", "recipeNo"),
                GetTagAddress(config, "process", "nozzleTemp"),
                GetTagAddress(config, "status",  "isRunning"),
                GetTagAddress(config, "status",  "isAlarm"));

            _cachedAlarmMap[key] = LoadAlarmBitPoints(GetAlarmConfigFile(config));
        }
    }

    /// <summary>
    /// Updates all overview cards from the latest PLC scan using cached maps.
    /// </summary>
    public static void UpdateOverviewCards(IPlcManager manager, IReadOnlyList<MachineOverviewCard> cards)
    {
        foreach (var card in cards)
        {
            if (!_cachedAddressMap.TryGetValue(card.MachineId, out var map))
            {
                continue;
            }

            var isRunning  = ReadBoolAddress(manager, map.RunningAddress);
            var isAlarm    = ReadBoolAddress(manager, map.AlarmAddress);
            var alarmCount = GetActiveAlarmCount(manager, card.MachineId);

            card.StatusText  = isRunning ? "Running" : "Idle";
            card.StatusBrush = isAlarm
                ? Brushes.OrangeRed
                : isRunning ? Brushes.LimeGreen : Brushes.SlateGray;

            card.LeftBottomLabel  = "Alarm";
            card.LeftBottomValue  = alarmCount.ToString();
            card.RightTopLabel    = "Nozzle";
            card.RightTopValue    = ReadWordText(manager, map.NozzleAddress);
            card.BatchValue       = ReadWordText(manager, map.BatchAddress);
            card.RecipeText       = ReadWordText(manager, map.RecipeAddress);
        }
    }

    // �w�w PLC read helpers �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w

    private static bool ReadBoolAddress(IPlcManager manager, string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address == "--")
        {
            return false;
        }

        if (address.Contains('.', StringComparison.Ordinal))
        {
            var parts = address.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[1], out var bitIndex))
            {
                return manager.Monitor?.GetBit(parts[0], bitIndex) ?? false;
            }
        }

        return manager.ReadBit(address) ?? false;
    }

    private static string ReadWordText(IPlcManager manager, string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address == "--")
        {
            return "--";
        }

        var value = manager.ReadWord(address);
        return value?.ToString() ?? "--";
    }

    private static int GetActiveAlarmCount(IPlcManager manager, string machineId)
    {
        if (!_cachedAlarmMap.TryGetValue(machineId, out var points) || points.Count == 0)
        {
            return 0;
        }

        var wordCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var active = 0;
        foreach (var point in points)
        {
            if (!wordCache.TryGetValue(point.Device, out var word))
            {
                word = manager.ReadWord(point.Device);
                wordCache[point.Device] = word;
            }

            if (word.HasValue && ((word.Value >> point.Bit) & 1) == 1)
            {
                active++;
            }
        }

        return active;
    }

    private static IReadOnlyList<AlarmBitPoint> LoadAlarmBitPoints(string alarmConfigPath)
    {
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

            var points = new List<AlarmBitPoint>();
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
                    points.Add(new AlarmBitPoint(device.Trim().ToUpperInvariant(), bit));
                }
            }

            return points;
        }
        catch
        {
            return [];
        }
    }

    public static DeviceContext CreateDeviceContext(UbiMachineConfig config)
    {
        var printHeadConfigs = GetPrintHeadConfigFiles(config);
        return new DeviceContext
        {
            MachineId    = config.Machine.Id,
            MachineName  = config.Machine.Name,
            BatchAddress  = GetTagAddress(config, "process", "batchNo"),
            RecipeAddress = GetTagAddress(config, "process", "recipeNo"),
            NozzleAddress = GetTagAddress(config, "process", "nozzleTemp"),
            RunningAddress = GetTagAddress(config, "status", "isRunning"),
            AlarmAddress   = GetTagAddress(config, "status", "isAlarm"),
            AlarmConfigFile  = GetAlarmConfigFile(config),
            SensorConfigFile = GetSensorConfigFile(config),
            PrintHead1ConfigFile = printHeadConfigs.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = printHeadConfigs.ElementAtOrDefault(1) ?? string.Empty,
            TotalTrayAddress    = GetDetailLabelAddress(config, "totalTray",          "D3400"),
            CurrentTrayAddress  = GetDetailLabelAddress(config, "currentTray",        "D33"),
            TotalLayerAddress   = GetDetailLabelAddress(config, "totalLayer",         "D3401"),
            CurrentLayerAddress = GetDetailLabelAddress(config, "currentLayer",       "D32"),
            SwitchGraphicLayerAddress = GetDetailLabelAddress(config, "switchGraphicLayer", "D510"),
            SwitchAreaLayerAddress    = GetDetailLabelAddress(config, "switchAreaLayer",    "D512"),
            MessageIdAddress      = GetDetailLabelAddress(config, "messageId",     "D85"),
            BatteryAddress        = GetDetailLabelAddress(config, "battery",       "D120"),
            ElapsedTimeAddress    = GetDetailLabelAddress(config, "elapsedTime",   "D86"),
            PrintHeadCountAddress = GetDetailLabelAddress(config, "printHeadCount","D87")
        };
    }

    // �w�w Existing public API (unchanged) �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w

    public static void ApplyMeta(MachineOverviewPage page, UbiAppMeta meta)
    {
        page.ShowMachineCards    = meta.ShowMachineCards;
        page.ShowSoftwareInfo    = meta.ShowSoftwareInfo;
        page.ShowLiveLog         = meta.ShowLiveLog;
        page.BottomPanelHeight   = new System.Windows.GridLength(meta.BottomPanelHeight);
        page.BottomLeftTitle     = meta.BottomLeftTitle;
        page.BottomRightTitle    = meta.BottomRightTitle;
        page.SoftwareInfoItems   =
        [
            .. meta.SoftwareInfoItems.Select(item => new OverviewInfoItem(item.Label, item.Value))
        ];
    }

    public static void BindOverview(MachineOverviewPage page, IReadOnlyList<UbiMachineConfig> configs)
    {
        if (configs.Count == 0)
        {
            page.MachineCards        = [];
            page.PlcMonitorAddresses = string.Empty;
            return;
        }

        var first = configs[0];
        page.PlcIpAddress        = first.Plc.Ip;
        page.PlcPort             = first.Plc.Port;
        page.PlcScanInterval     = first.Plc.PollIntervalMs;
        page.PlcAutoConnect      = first.Plc.AutoConnect;
        page.PlcMonitorAddresses = BuildMonitorAddresses(configs);
        page.MachineCards        = [.. configs.Select(CreateCard)];
    }

    public static string GetTagAddress(UbiMachineConfig config, string section, string key)
    {
        Dictionary<string, UbiTagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status"  => config.Tags.Status,
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
                _    => string.Empty
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
                _    => string.Empty
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
            _    => []
        };

        return fallback.Select(ToAbsoluteConfigPath).ToList();
    }

    private static string ToAbsoluteConfigPath(string relativePath)
        => Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static MachineOverviewCard CreateCard(UbiMachineConfig config) => new()
    {
        MachineId        = config.Machine.Id,
        Title            = config.Machine.Name,
        BatchValue       = "--",
        RecipeText       = "--",
        StatusText       = "Idle",
        StatusBrush      = Brushes.SlateGray,
        LeftTopLabel     = "Heartbeat",
        LeftTopValue     = "0",
        LeftBottomLabel  = "Alarm",
        LeftBottomValue  = "0",
        RightTopLabel    = "Nozzle",
        RightTopValue    = "--",
        RightBottomLabel = "Mode",
        RightBottomValue = "Manual"
    };

    private static string BuildMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        var addresses = configs
            .SelectMany(config => config.Tags.Status.Values.Concat(config.Tags.Process.Values))
            .Where(tag => string.Equals(tag.Access, "read", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ExpandAddresses)
            .Concat(GetManualPlcMonitorAddresses(configs))
            .Concat(GetUbiDevicePageDetailLabelAddresses(configs))
            .Concat(GetMachineAlertAddresses(configs))
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
            var end   = i;

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
        var defaultAddresses = new[] { "D3400", "D33", "D3401", "D32", "D510", "D512", "D85", "D120", "D86", "D87" };

        foreach (var config in configs)
        {
            if (config.DetailLabels.Count == 0)
            {
                foreach (var address in defaultAddresses) yield return address;
                continue;
            }

            foreach (var address in config.DetailLabels.Values)
            {
                if (!string.IsNullOrWhiteSpace(address)) yield return address.Trim();
            }
        }
    }

    private static IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
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

    private static IEnumerable<string> GetMachineAlertAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        foreach (var config in configs)
        {
            foreach (var a in ReadAddressesFromSensorFile(GetSensorConfigFile(config))) yield return a;
            foreach (var a in ReadAddressesFromAlarmFile(GetAlarmConfigFile(config)))  yield return a;
        }
    }

    private static IEnumerable<string> ReadAddressesFromSensorFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return [];

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("Device", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    var device = d.GetString();
                    if (!string.IsNullOrWhiteSpace(device)) addresses.Add(device.Trim().ToUpperInvariant());
                }
            }

            return addresses;
        }
        catch { return []; }
    }

    private static IEnumerable<string> ReadAddressesFromAlarmFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return [];

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
                    if (!string.IsNullOrWhiteSpace(device)) addresses.Add(device.Trim().ToUpperInvariant());
                }
            }

            return addresses;
        }
        catch { return []; }
    }

    private static IEnumerable<string> ExpandAddresses(UbiTagConfig tag)
    {
        var parsed = ParseAddress(tag.Address);
        if (parsed == null) yield break;

        var (prefix, start) = parsed.Value;
        var span = tag.Type.Equals("string", StringComparison.OrdinalIgnoreCase) ? Math.Max(1, tag.Length) : 1;
        for (var i = 0; i < span; i++) yield return $"{prefix}{start + i}";
    }

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number)) return null;
        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }
}
