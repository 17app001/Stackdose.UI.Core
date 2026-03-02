using Stackdose.App.SingleDetailLab.Models;
using System.Text.RegularExpressions;

namespace Stackdose.App.SingleDetailLab.Services;

internal static class LabMonitorAddressBuilder
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public static string Build(LabMachineConfig config)
    {
        var readTags = config.Tags.Status.Values
            .Concat(config.Tags.Process.Values)
            .Where(tag => string.Equals(tag.Access, "read", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ExpandAddresses)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var parsed = readTags
            .Select(ParseAddress)
            .Where(x => x != null)
            .Cast<(string Prefix, int Number, string Raw)>()
            .OrderBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < parsed.Count)
        {
            var start = parsed[i];
            var endIndex = i;

            while (endIndex + 1 < parsed.Count
                   && string.Equals(parsed[endIndex + 1].Prefix, start.Prefix, StringComparison.OrdinalIgnoreCase)
                   && parsed[endIndex + 1].Number == parsed[endIndex].Number + 1)
            {
                endIndex++;
            }

            var length = endIndex - i + 1;
            groups.Add(length > 1 ? $"{start.Raw},{length}" : start.Raw);
            i = endIndex + 1;
        }

        return string.Join(',', groups);
    }

    private static (string Prefix, int Number, string Raw)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address.Trim());
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        var prefix = match.Groups[1].Value.ToUpperInvariant();
        return (prefix, number, $"{prefix}{number}");
    }

    private static IEnumerable<string> ExpandAddresses(LabTagConfig tag)
    {
        var parsed = ParseAddress(tag.Address);
        if (parsed == null)
        {
            yield break;
        }

        var (prefix, start, _) = parsed.Value;
        var length = tag.Type.Equals("string", StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, tag.Length)
            : 1;

        for (var i = 0; i < length; i++)
        {
            yield return $"{prefix}{start + i}";
        }
    }
}
