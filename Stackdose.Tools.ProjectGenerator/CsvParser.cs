namespace Stackdose.Tools.ProjectGenerator;

/// <summary>
/// Parses a Device-Spec CSV file with multiple sheet sections.
/// Sections are separated by lines starting with "## Sheet:".
/// </summary>
public static class CsvParser
{
    private enum SheetKind { None, Project, Machines, Commands, Labels, Tags, Panels, MaintenanceItems }

    public static DeviceSpec Parse(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Spec file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        var spec = new DeviceSpec();

        var currentSheet = SheetKind.None;
        string[]? headers = null;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex].Trim();

            // Strip BOM if present (can appear on first line of UTF-8 files)
            if (lineIndex == 0 && line.Length > 0 && line[0] == '\uFEFF')
                line = line[1..];

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Detect sheet boundary — match "## Sheet:" anywhere in a comment line
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                if (line.Contains("Sheet:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Sheet :", StringComparison.OrdinalIgnoreCase))
                {
                    currentSheet = DetectSheet(line);
                    headers = null; // next non-comment CSV line is the header
                }
                // Either way, skip this comment line
                continue;
            }

            // Skip other comment lines (single #)
            if (line.StartsWith('#'))
                continue;

            // We must be inside a sheet to parse data
            if (currentSheet == SheetKind.None)
                continue;

            // Parse CSV row
            var fields = SplitCsv(line);
            if (fields.Length == 0)
                continue;

            // First CSV line after a sheet marker = header
            if (headers is null)
            {
                headers = fields;
                continue;
            }

            // Data row
            var row = BuildRow(headers, fields);

            switch (currentSheet)
            {
                case SheetKind.Project:
                    spec.Project = ParseProject(row);
                    break;
                case SheetKind.Machines:
                    spec.Machines.Add(ParseMachine(row));
                    break;
                case SheetKind.Commands:
                    spec.Commands.Add(ParseCommand(row));
                    break;
                case SheetKind.Labels:
                    spec.Labels.Add(ParseLabel(row));
                    break;
                case SheetKind.Tags:
                    spec.Tags.Add(ParseTag(row));
                    break;
                case SheetKind.Panels:
                    spec.Panels.Add(ParsePanel(row));
                    break;
                case SheetKind.MaintenanceItems:
                    spec.MaintenanceItems.Add(ParseMaintenanceItem(row));
                    break;
            }
        }

        Validate(spec);
        return spec;
    }

    private static SheetKind DetectSheet(string line)
    {
        var lower = line.ToLowerInvariant();
        // Order matters: check more specific terms first
        if (lower.Contains("maintenanceitem") || lower.Contains("maintenance item")) return SheetKind.MaintenanceItems;
        if (lower.Contains("panel")) return SheetKind.Panels;
        if (lower.Contains("project")) return SheetKind.Project;
        if (lower.Contains("machine")) return SheetKind.Machines;
        if (lower.Contains("command")) return SheetKind.Commands;
        if (lower.Contains("label")) return SheetKind.Labels;
        if (lower.Contains("tag")) return SheetKind.Tags;
        return SheetKind.None;
    }

    private static string[] SplitCsv(string line)
    {
        // Split, trim each field, then remove trailing empty fields
        // (Excel/VS often pads rows with trailing commas)
        var fields = line.Split(',').Select(f => f.Trim()).ToArray();

        // Find the last non-empty field
        int lastNonEmpty = -1;
        for (int i = fields.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(fields[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        // If all fields are empty, return empty array (skip this row)
        if (lastNonEmpty < 0)
            return Array.Empty<string>();

        return fields[..(lastNonEmpty + 1)];
    }

    private static Dictionary<string, string> BuildRow(string[] headers, string[] fields)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length && i < fields.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(headers[i]))
                row[headers[i]] = fields[i];
        }
        return row;
    }

    private static string Get(Dictionary<string, string> row, string key, string defaultValue = "")
    {
        return row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val) ? val : defaultValue;
    }

    private static int GetInt(Dictionary<string, string> row, string key, int defaultValue)
    {
        return row.TryGetValue(key, out var val) && int.TryParse(val, out var n) ? n : defaultValue;
    }

    private static bool GetBool(Dictionary<string, string> row, string key, bool defaultValue)
    {
        if (!row.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
            return defaultValue;
        return val.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectInfo ParseProject(Dictionary<string, string> row) => new()
    {
        ProjectName = Get(row, "ProjectName"),
        HeaderDeviceName = Get(row, "HeaderDeviceName", "DEVICE"),
        Version = Get(row, "Version", "v1.0.0"),
        PageMode = Get(row, "PageMode", "DynamicDevicePage"),
        AutoConnect = GetBool(row, "AutoConnect", false),
    };

    private static MachineInfo ParseMachine(Dictionary<string, string> row) => new()
    {
        MachineId = Get(row, "MachineId"),
        MachineName = Get(row, "MachineName"),
        PlcIp = Get(row, "PlcIp", "127.0.0.1"),
        PlcPort = GetInt(row, "PlcPort", 3000),
        PollIntervalMs = GetInt(row, "PollIntervalMs", 200),
        ProcessMonitorIsRunning = Get(row, "ProcessMonitor.IsRunning", "M200"),
        ProcessMonitorIsCompleted = Get(row, "ProcessMonitor.IsCompleted", "M202"),
        ProcessMonitorIsAlarm = Get(row, "ProcessMonitor.IsAlarm", "M201"),
        Modules = Get(row, "Modules", "processControl"),
        MachineDesignFile = Get(row, "MachineDesignFile"),
    };

    private static CommandInfo ParseCommand(Dictionary<string, string> row) => new()
    {
        MachineId = Get(row, "MachineId"),
        CommandName = Get(row, "CommandName"),
        Address = Get(row, "Address"),
    };

    private static LabelInfo ParseLabel(Dictionary<string, string> row) => new()
    {
        MachineId = Get(row, "MachineId"),
        LabelName = Get(row, "LabelName"),
        Address = Get(row, "Address"),
    };

    private static TagInfo ParseTag(Dictionary<string, string> row) => new()
    {
        MachineId = Get(row, "MachineId"),
        Section = Get(row, "Section", "status"),
        TagName = Get(row, "TagName"),
        Address = Get(row, "Address"),
        Type = Get(row, "Type", "int16"),
        Access = Get(row, "Access", "read"),
        Length = GetInt(row, "Length", 1),
    };

    private static PanelInfo ParsePanel(Dictionary<string, string> row) => new()
    {
        PanelType = Get(row, "PanelType"),
        MachineId = Get(row, "MachineId", "*"),
        Position = Get(row, "Position", "Separate"),
        Title = Get(row, "Title", ""),
        RequiredLevel = Get(row, "RequiredLevel", "Supervisor"),
    };

    private static MaintenanceItemInfo ParseMaintenanceItem(Dictionary<string, string> row) => new()
    {
        MachineId = Get(row, "MachineId"),
        ItemName = Get(row, "ItemName"),
        Address = Get(row, "Address"),
        Type = Get(row, "Type", "editor"),
        Label = Get(row, "Label", ""),
    };

    private static void Validate(DeviceSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Project.ProjectName))
            throw new InvalidOperationException("CSV 缺少 Project.ProjectName");

        if (!spec.Project.ProjectName.StartsWith("Stackdose.App.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"ProjectName 必須以 'Stackdose.App.' 開頭，目前為: {spec.Project.ProjectName}");

        if (spec.Machines.Count == 0)
            throw new InvalidOperationException("CSV 至少需要定義一台 Machine");

        var ids = spec.Machines.Select(m => m.MachineId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ids.Count != spec.Machines.Count)
            throw new InvalidOperationException("Machine ID 不可重複");

        // Validate command/label references
        foreach (var cmd in spec.Commands)
        {
            if (cmd.MachineId != "*" && !ids.Contains(cmd.MachineId))
                throw new InvalidOperationException($"Command 參考了不存在的 MachineId: {cmd.MachineId}");
        }
        foreach (var lbl in spec.Labels)
        {
            if (lbl.MachineId != "*" && !ids.Contains(lbl.MachineId))
                throw new InvalidOperationException($"Label 參考了不存在的 MachineId: {lbl.MachineId}");
        }
    }
}
