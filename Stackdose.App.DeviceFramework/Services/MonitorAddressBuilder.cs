using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Templates.Pages;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Monitor 位址建構器 — 自動收集所有需要的 PLC 位址並合併連續位址。
/// </summary>
public static class MonitorAddressBuilder
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// 從所有機台設定中收集可讀 PLC 位址，自動合併連續位址。
    /// </summary>
    public static string Build(IEnumerable<MachineConfig> configs, IRuntimeMappingAdapter adapter)
    {
        var configList = configs as IReadOnlyList<MachineConfig> ?? configs.ToList();

        // 從 Tags 區段收集所有可讀位址
        var tagAddresses = configList.SelectMany(CollectTagAddresses);

        var addresses = tagAddresses
            .Concat(adapter.GetManualPlcMonitorAddresses(configList))
            .Concat(adapter.GetDetailLabelAddresses(configList))
            .Concat(adapter.GetMachineAlertAddresses(configList))
            .Concat(CollectComponentAddresses(configList, adapter))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return MergeAddresses(addresses);
    }

    /// <summary>
    /// 建立 Overview 卡片的預設值。
    /// </summary>
    public static MachineOverviewCard CreateCard(MachineConfig config) => new()
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

    /// <summary>
    /// 將位址清單合併為 PLC Monitor 格式字串（例如 "D0,10,M100,5"）。
    /// </summary>
    public static string MergeAddresses(IEnumerable<string> addresses)
    {
        var parsed = addresses
            .Select(ParseAddress)
            .Where(x => x != null)
            .Cast<(string Prefix, int Number)>()
            .Distinct()
            .OrderBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < parsed.Count)
        {
            var start = parsed[i];
            var end = i;

            while (end + 1 < parsed.Count
                   && string.Equals(parsed[end + 1].Prefix, start.Prefix, StringComparison.OrdinalIgnoreCase)
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

    private static IEnumerable<string> CollectTagAddresses(MachineConfig config)
    {
        var allTags = config.Tags.Status.Values.Concat(config.Tags.Process.Values);
        foreach (var tag in allTags)
        {
            if (string.IsNullOrWhiteSpace(tag.Address))
                continue;

            if (!string.IsNullOrWhiteSpace(tag.Access) && !tag.Access.Equals("read", StringComparison.OrdinalIgnoreCase))
                continue;

            var parsed = ParseAddress(tag.Address);
            if (parsed == null)
                continue;

            for (int i = 0; i < Math.Max(1, tag.Length); i++)
            {
                yield return $"{parsed.Value.Prefix}{parsed.Value.Number + i}";
            }
        }
    }

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number))
            return null;

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }

    private static IEnumerable<string> CollectComponentAddresses(IReadOnlyList<MachineConfig> configs, IRuntimeMappingAdapter adapter)
    {
        var addresses = new List<string>();
        foreach (var config in configs)
        {
            CollectAddressesFromJsonFile(adapter.GetSensorConfigFile(config), isAlarmFile: false, addresses);
            CollectAddressesFromJsonFile(adapter.GetAlarmConfigFile(config), isAlarmFile: true, addresses);
        }

        return addresses
            .Select(a => a.Split(',')[0].Trim())
            .Where(a => !string.IsNullOrEmpty(a));
    }

    private static void CollectAddressesFromJsonFile(string filePath, bool isAlarmFile, List<string> addresses)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            return;

        try
        {
            var json = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            using var doc = JsonDocument.Parse(json);

            JsonElement root;
            if (isAlarmFile)
            {
                if (!doc.RootElement.TryGetProperty("Alarms", out root) || root.ValueKind != JsonValueKind.Array)
                    return;
            }
            else
            {
                root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                    return;
            }

            foreach (var item in root.EnumerateArray())
            {
                if (item.TryGetProperty("Device", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    var device = d.GetString();
                    if (!string.IsNullOrWhiteSpace(device))
                    {
                        addresses.Add(device);
                    }
                }
            }
        }
        catch { }
    }
}
