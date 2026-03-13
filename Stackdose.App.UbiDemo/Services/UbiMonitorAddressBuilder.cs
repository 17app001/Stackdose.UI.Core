using Stackdose.App.ShellShared.Services;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiMonitorAddressBuilder
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public static string Build(IEnumerable<UbiMachineConfig> configs, IUbiRuntimeMappingAdapter adapter)
    {
        var configList = configs as IReadOnlyList<UbiMachineConfig> ?? configs.ToList();
        var sharedAddresses = ShellMonitorAddressBuilder.CollectReadableAddresses(
            configList.Select(UbiShellSharedAdapter.ToShellMachineConfig));

        var addresses = sharedAddresses
            .Concat(adapter.GetManualPlcMonitorAddresses(configList))
            .Concat(adapter.GetDetailLabelAddresses(configList))
            .Concat(adapter.GetMachineAlertAddresses(configList))
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

    public static MachineOverviewCard CreateCard(UbiMachineConfig config) => new()
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

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }
}
