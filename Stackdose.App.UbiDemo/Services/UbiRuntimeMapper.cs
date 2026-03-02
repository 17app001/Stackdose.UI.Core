using Stackdose.Abstractions.Hardware;
using Stackdose.App.ShellShared.Services;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    private static Dictionary<string, int> _cachedAlarmCount = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, DateTime> _cachedAlarmCountUpdatedAt = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan AlarmCountRefreshInterval = TimeSpan.FromMilliseconds(500);
    private static bool _enableOverviewAlarmCount;
    private static IUbiRuntimeMappingAdapter _mappingAdapter = new UbiRuntimeMappingAdapter();

    internal static void ConfigureMappingAdapter(IUbiRuntimeMappingAdapter? adapter)
    {
        _mappingAdapter = adapter ?? new UbiRuntimeMappingAdapter();
    }

    // �w�w Overview card PLC update �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w

    /// <summary>
    /// Builds and caches the internal address/alarm maps. Call once after config is loaded.
    /// </summary>
    public static void BuildRuntimeMaps(IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        _cachedAddressMap.Clear();
        _cachedAlarmMap.Clear();
        _cachedAlarmCount.Clear();
        _cachedAlarmCountUpdatedAt.Clear();

        foreach (var (key, config) in machines)
        {
            _cachedAddressMap[key] = new OverviewAddressMap(
                GetTagAddress(config, "process", "batchNo"),
                GetTagAddress(config, "process", "recipeNo"),
                GetTagAddress(config, "process", "nozzleTemp"),
                GetTagAddress(config, "status",  "isRunning"),
                GetTagAddress(config, "status",  "isAlarm"));

            _cachedAlarmMap[key] =
            [
                .. _mappingAdapter.LoadAlarmBitPoints(config)
                    .Select(point => new AlarmBitPoint(point.Device, point.Bit))
            ];
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

            var isRunning = ReadBoolAddress(manager, map.RunningAddress);
            var isAlarm = ReadBoolAddress(manager, map.AlarmAddress);
            var alarmCount = _enableOverviewAlarmCount && isAlarm
                ? GetActiveAlarmCount(manager, card.MachineId)
                : 0;

            if (!isAlarm || !_enableOverviewAlarmCount)
            {
                _cachedAlarmCount[card.MachineId] = 0;
                _cachedAlarmCountUpdatedAt[card.MachineId] = DateTime.UtcNow;
            }

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
        var now = DateTime.UtcNow;
        if (_cachedAlarmCountUpdatedAt.TryGetValue(machineId, out var updatedAt)
            && now - updatedAt < AlarmCountRefreshInterval
            && _cachedAlarmCount.TryGetValue(machineId, out var cachedValue))
        {
            return cachedValue;
        }

        if (!_cachedAlarmMap.TryGetValue(machineId, out var points) || points.Count == 0)
        {
            _cachedAlarmCount[machineId] = 0;
            _cachedAlarmCountUpdatedAt[machineId] = now;
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

        _cachedAlarmCount[machineId] = active;
        _cachedAlarmCountUpdatedAt[machineId] = now;
        return active;
    }

    public static DeviceContext CreateDeviceContext(UbiMachineConfig config)
    {
        var printHeadConfigs = _mappingAdapter.GetPrintHeadConfigFiles(config);
        return new DeviceContext
        {
            MachineId    = config.Machine.Id,
            MachineName  = config.Machine.Name,
            BatchAddress  = GetTagAddress(config, "process", "batchNo"),
            RecipeAddress = GetTagAddress(config, "process", "recipeNo"),
            NozzleAddress = GetTagAddress(config, "process", "nozzleTemp"),
            RunningAddress = GetTagAddress(config, "status", "isRunning"),
            AlarmAddress   = GetTagAddress(config, "status", "isAlarm"),
            AlarmConfigFile  = _mappingAdapter.GetAlarmConfigFile(config),
            SensorConfigFile = _mappingAdapter.GetSensorConfigFile(config),
            PrintHead1ConfigFile = printHeadConfigs.ElementAtOrDefault(0) ?? string.Empty,
            PrintHead2ConfigFile = printHeadConfigs.ElementAtOrDefault(1) ?? string.Empty,
            TotalTrayAddress    = _mappingAdapter.GetDetailLabelAddress(config, "totalTray",          "D3400"),
            CurrentTrayAddress  = _mappingAdapter.GetDetailLabelAddress(config, "currentTray",        "D33"),
            TotalLayerAddress   = _mappingAdapter.GetDetailLabelAddress(config, "totalLayer",         "D3401"),
            CurrentLayerAddress = _mappingAdapter.GetDetailLabelAddress(config, "currentLayer",       "D32"),
            SwitchGraphicLayerAddress = _mappingAdapter.GetDetailLabelAddress(config, "switchGraphicLayer", "D510"),
            SwitchAreaLayerAddress    = _mappingAdapter.GetDetailLabelAddress(config, "switchAreaLayer",    "D512"),
            MessageIdAddress      = _mappingAdapter.GetDetailLabelAddress(config, "messageId",     "D85"),
            BatteryAddress        = _mappingAdapter.GetDetailLabelAddress(config, "battery",       "D120"),
            ElapsedTimeAddress    = _mappingAdapter.GetDetailLabelAddress(config, "elapsedTime",   "D86"),
            PrintHeadCountAddress = _mappingAdapter.GetDetailLabelAddress(config, "printHeadCount","D87")
        };
    }

    // �w�w Existing public API (unchanged) �w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w�w

    public static void ApplyMeta(MachineOverviewPage page, UbiAppMeta meta)
    {
        page.ShowMachineCards    = meta.ShowMachineCards;
        page.ShowSoftwareInfo    = meta.ShowSoftwareInfo;
        page.ShowLiveLog         = meta.ShowLiveLog;
        _enableOverviewAlarmCount = meta.EnableOverviewAlarmCount;
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
        var configList = configs as IReadOnlyList<UbiMachineConfig> ?? configs.ToList();
        var sharedAddresses = ShellMonitorAddressBuilder.CollectReadableAddresses(
            configList.Select(UbiShellSharedAdapter.ToShellMachineConfig));

        var addresses = sharedAddresses
            .Concat(GetManualPlcMonitorAddresses(configList))
            .Concat(GetUbiDevicePageDetailLabelAddresses(configList))
            .Concat(GetMachineAlertAddresses(configList))
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
        return _mappingAdapter.GetDetailLabelAddresses(configs);
    }

    private static IEnumerable<string> GetManualPlcMonitorAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        return _mappingAdapter.GetManualPlcMonitorAddresses(configs);
    }

    private static IEnumerable<string> GetMachineAlertAddresses(IEnumerable<UbiMachineConfig> configs)
    {
        return _mappingAdapter.GetMachineAlertAddresses(configs);
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
