using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stackdose.App.Monitor.Services;

internal sealed class SinglePageRuntimeService
{
    private readonly string _projectFolderName;

    public SinglePageRuntimeService(string projectFolderName)
    {
        _projectFolderName = projectFolderName;
    }

    public bool TryLoad(out SinglePageRuntimeConfig runtime)
    {
        runtime = default;

        var configPath = ResolveConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("machine", out var machine) || !root.TryGetProperty("plc", out var plc))
        {
            return false;
        }

        var machineName = machine.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "Machine" : "Machine";
        var machineId = machine.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "M1" : "M1";
        var ip = plc.TryGetProperty("ip", out var ipElement) ? ipElement.GetString() ?? "127.0.0.1" : "127.0.0.1";
        var port = plc.TryGetProperty("port", out var portElement) && portElement.TryGetInt32(out var portValue) ? portValue : 5000;
        var interval = plc.TryGetProperty("pollIntervalMs", out var intervalElement) && intervalElement.TryGetInt32(out var intervalValue) ? intervalValue : 150;
        var autoConnect = !plc.TryGetProperty("autoConnect", out var autoElement) || autoElement.GetBoolean();
        var sensorConfigFile = root.TryGetProperty("sensorConfigFile", out var sensorElement) && sensorElement.ValueKind == JsonValueKind.String
            ? sensorElement.GetString() ?? "Config/Machine1.sensors.json"
            : "Config/Machine1.sensors.json";
        var alarmConfigFile = root.TryGetProperty("alarmConfigFile", out var alarmElement) && alarmElement.ValueKind == JsonValueKind.String
            ? alarmElement.GetString() ?? "Config/Machine1.alarms.json"
            : "Config/Machine1.alarms.json";
        var sensorConfigPath = ResolveCompanionConfigPath(configPath, sensorConfigFile);
        var alarmConfigPath = ResolveCompanionConfigPath(configPath, alarmConfigFile);

        var monitorAddress = BuildMonitorAddress(root);
        if (plc.TryGetProperty("manualMonitorAddress", out var manualMonitor) && manualMonitor.ValueKind == JsonValueKind.String)
        {
            var manual = manualMonitor.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(manual))
            {
                monitorAddress = string.IsNullOrWhiteSpace(monitorAddress)
                    ? manual
                    : $"{monitorAddress},{manual}";
            }
        }

        runtime = new SinglePageRuntimeConfig(machineName, machineId, ip, port, interval, autoConnect, monitorAddress, sensorConfigPath, alarmConfigPath);
        return true;
    }

    private static string ResolveCompanionConfigPath(string mainConfigPath, string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var normalized = configuredPath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, normalized);
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var configDir = Path.GetDirectoryName(mainConfigPath) ?? AppContext.BaseDirectory;
        if (normalized.StartsWith($"Config{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            var projectRoot = Directory.GetParent(configDir)?.FullName ?? configDir;
            return Path.Combine(projectRoot, normalized);
        }

        return Path.Combine(configDir, normalized);
    }

    private string ResolveConfigPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            var projectConfig = Path.Combine(current.FullName, _projectFolderName, "Config", "Machine1.config.json");
            if (File.Exists(projectConfig))
            {
                return projectConfig;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Config", "Machine1.config.json");
    }

    private static string BuildMonitorAddress(JsonElement root)
    {
        if (!root.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var expanded = new List<(string Prefix, int Number, string Raw)>();
        AddSectionAddresses(tags, "status", expanded);
        AddSectionAddresses(tags, "process", expanded);

        if (expanded.Count == 0)
        {
            return string.Empty;
        }

        var sorted = expanded
            .Distinct()
            .OrderBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < sorted.Count)
        {
            var start = sorted[i];
            var end = i;

            while (end + 1 < sorted.Count
                   && string.Equals(sorted[end + 1].Prefix, start.Prefix, StringComparison.OrdinalIgnoreCase)
                   && sorted[end + 1].Number == sorted[end].Number + 1)
            {
                end++;
            }

            var length = end - i + 1;
            groups.Add(length > 1 ? $"{start.Raw},{length}" : start.Raw);
            i = end + 1;
        }

        return string.Join(',', groups);
    }

    private static void AddSectionAddresses(JsonElement tags, string sectionName, List<(string Prefix, int Number, string Raw)> result)
    {
        if (!tags.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var tagEntry in section.EnumerateObject())
        {
            var tag = tagEntry.Value;
            if (tag.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (tag.TryGetProperty("access", out var accessElement)
                && accessElement.ValueKind == JsonValueKind.String
                && !string.Equals(accessElement.GetString(), "read", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!tag.TryGetProperty("address", out var addressElement) || addressElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var address = addressElement.GetString();
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var baseAddress = ParseAddress(address);
            if (baseAddress == null)
            {
                continue;
            }

            var length = 1;
            if (tag.TryGetProperty("type", out var typeElement)
                && typeElement.ValueKind == JsonValueKind.String
                && string.Equals(typeElement.GetString(), "string", StringComparison.OrdinalIgnoreCase)
                && tag.TryGetProperty("length", out var lengthElement)
                && lengthElement.TryGetInt32(out var configuredLength))
            {
                length = Math.Max(1, configuredLength);
            }

            for (var i = 0; i < length; i++)
            {
                result.Add((baseAddress.Value.Prefix, baseAddress.Value.Number + i, $"{baseAddress.Value.Prefix}{baseAddress.Value.Number + i}"));
            }
        }
    }

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = Regex.Match(address.Trim(), "^([A-Za-z]+)(\\d+)$");
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[2].Value, out var number)
            ? (match.Groups[1].Value.ToUpperInvariant(), number)
            : null;
    }
}

internal readonly record struct SinglePageRuntimeConfig(
    string MachineName,
    string MachineId,
    string Ip,
    int Port,
    int PollIntervalMs,
    bool AutoConnect,
    string MonitorAddress,
    string SensorConfigPath,
    string AlarmConfigPath);
