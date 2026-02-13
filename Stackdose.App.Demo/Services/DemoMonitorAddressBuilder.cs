using Stackdose.App.Demo.Models;
using System.Text.RegularExpressions;

namespace Stackdose.App.Demo.Services;

public static class DemoMonitorAddressBuilder
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public static string Build(IEnumerable<DemoMachineConfig> configs)
    {
        var addresses = configs
            .SelectMany(config => config.Tags.Status.Values.Concat(config.Tags.Process.Values))
            .Where(tag => string.Equals(tag.Access, "read", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ExpandAddresses)
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

    private static IEnumerable<string> ExpandAddresses(TagConfig tag)
    {
        var parsed = ParseAddress(tag.Address);
        if (parsed == null)
        {
            yield break;
        }

        var (prefix, start) = parsed.Value;
        var span = tag.Type.Equals("string", StringComparison.OrdinalIgnoreCase) ? Math.Max(1, tag.Length) : 1;
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
