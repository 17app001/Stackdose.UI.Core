using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System.Windows.Media;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiOverviewRuntimeMapper
{
    private sealed record AlarmBitPoint(string Device, int Bit);
    private sealed record OverviewAddressMap(
        string BatchAddress,
        string RecipeAddress,
        string NozzleAddress,
        string RunningAddress,
        string AlarmAddress);

    private readonly Dictionary<string, OverviewAddressMap> _cachedAddressMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<AlarmBitPoint>> _cachedAlarmMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _cachedAlarmCount = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _cachedAlarmCountUpdatedAt = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan AlarmCountRefreshInterval = TimeSpan.FromMilliseconds(500);

    public bool EnableOverviewAlarmCount { get; set; }

    public void BuildRuntimeMaps(IReadOnlyDictionary<string, UbiMachineConfig> machines, IUbiRuntimeMappingAdapter adapter)
    {
        _cachedAddressMap.Clear();
        _cachedAlarmMap.Clear();
        _cachedAlarmCount.Clear();
        _cachedAlarmCountUpdatedAt.Clear();

        foreach (var (key, config) in machines)
        {
            _cachedAddressMap[key] = new OverviewAddressMap(
                adapter.GetTagAddress(config, "process", "batchNo"),
                adapter.GetTagAddress(config, "process", "recipeNo"),
                adapter.GetTagAddress(config, "process", "nozzleTemp"),
                adapter.GetTagAddress(config, "status", "isRunning"),
                adapter.GetTagAddress(config, "status", "isAlarm"));

            _cachedAlarmMap[key] =
            [
                .. adapter.LoadAlarmBitPoints(config)
                    .Select(point => new AlarmBitPoint(point.Device, point.Bit))
            ];
        }
    }

    public void UpdateOverviewCards(IPlcManager manager, IReadOnlyList<MachineOverviewCard> cards)
    {
        foreach (var card in cards)
        {
            if (!_cachedAddressMap.TryGetValue(card.MachineId, out var map))
            {
                continue;
            }

            var isRunning = ReadBoolAddress(manager, map.RunningAddress);
            var isAlarm = ReadBoolAddress(manager, map.AlarmAddress);
            var alarmCount = EnableOverviewAlarmCount && isAlarm
                ? GetActiveAlarmCount(manager, card.MachineId)
                : 0;

            if (!isAlarm || !EnableOverviewAlarmCount)
            {
                _cachedAlarmCount[card.MachineId] = 0;
                _cachedAlarmCountUpdatedAt[card.MachineId] = DateTime.UtcNow;
            }

            card.StatusText = isRunning ? "Running" : "Idle";
            card.StatusBrush = isAlarm
                ? Brushes.OrangeRed
                : isRunning ? Brushes.LimeGreen : Brushes.SlateGray;

            card.LeftBottomLabel = "Alarm";
            card.LeftBottomValue = alarmCount.ToString();
            card.RightTopLabel = "Nozzle";
            card.RightTopValue = ReadWordText(manager, map.NozzleAddress);
            card.BatchValue = ReadWordText(manager, map.BatchAddress);
            card.RecipeText = ReadWordText(manager, map.RecipeAddress);
        }
    }

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

    private int GetActiveAlarmCount(IPlcManager manager, string machineId)
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
}
