using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    public static string GetAlarmConfigFile(string machineId)
    {
        return machineId.ToUpperInvariant() switch
        {
            "M1" => "Config/MachineA.alarms.json",
            "M2" => "Config/MachineB.alarms.json",
            _ => string.Empty
        };
    }

    public static string GetSensorConfigFile(string machineId)
    {
        return machineId.ToUpperInvariant() switch
        {
            "M1" => "Config/MachineA.sensors.json",
            "M2" => "Config/MachineB.sensors.json",
            _ => string.Empty
        };
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
            LeftBottomValue = "Normal",
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
