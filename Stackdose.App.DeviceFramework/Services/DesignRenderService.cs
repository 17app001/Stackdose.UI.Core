using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stackdose.App.DeviceFramework.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// Reads .machinedesign.json produced by MachinePageDesigner and
/// converts zone items into DeviceLabelInfo dictionaries that the
/// existing DeviceContext / DevicePageViewModel pipeline can consume.
/// </summary>
public static class DesignRenderService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Apply design file data to an existing DeviceContext.
    /// Overwrites Labels / StatusLabels with design-file definitions
    /// when the corresponding zone exists in the design document.
    /// </summary>
    /// <returns>true if design file was applied successfully.</returns>
    public static bool ApplyDesignFile(DeviceContext context, string designFilePath)
    {
        if (string.IsNullOrWhiteSpace(designFilePath))
            return false;

        if (!File.Exists(designFilePath))
            return false;

        DesignDocumentDto? doc;
        try
        {
            var json = File.ReadAllText(designFilePath, System.Text.Encoding.UTF8);
            doc = JsonSerializer.Deserialize<DesignDocumentDto>(json, _jsonOptions);
        }
        catch
        {
            return false;
        }

        if (doc?.Zones is null || doc.Zones.Count == 0)
            return false;

        // liveData zone → context.Labels
        if (doc.Zones.TryGetValue("liveData", out var liveDataZone) && liveDataZone.Items.Count > 0)
        {
            context.Labels.Clear();
            foreach (var item in liveDataZone.Items.OrderBy(i => i.Order))
            {
                var label = ConvertToLabelInfo(item);
                if (label is not null)
                    context.Labels[label.Value.Key] = label.Value.Value;
            }
        }

        // deviceStatus zone → context.StatusLabels
        if (doc.Zones.TryGetValue("deviceStatus", out var statusZone) && statusZone.Items.Count > 0)
        {
            context.StatusLabels.Clear();
            foreach (var item in statusZone.Items.OrderBy(i => i.Order))
            {
                var label = ConvertToLabelInfo(item);
                if (label is not null)
                    context.StatusLabels[label.Value.Key] = label.Value.Value;
            }
        }

        // Apply zone titles if set
        if (doc.Zones.TryGetValue("liveData", out var ldz) && !string.IsNullOrWhiteSpace(ldz.Title))
            context.LiveDataTitle = ldz.Title;
        if (doc.Zones.TryGetValue("deviceStatus", out var dsz) && !string.IsNullOrWhiteSpace(dsz.Title))
            context.DeviceStatusTitle = dsz.Title;

        return true;
    }

    /// <summary>
    /// Resolve the full path of the design file relative to a config directory.
    /// </summary>
    public static string ResolveDesignFilePath(string machineDesignFile, string configDirectory)
    {
        if (string.IsNullOrWhiteSpace(machineDesignFile))
            return string.Empty;

        if (Path.IsPathRooted(machineDesignFile))
            return machineDesignFile;

        return Path.GetFullPath(Path.Combine(configDirectory, machineDesignFile));
    }

    private static KeyValuePair<string, DeviceLabelInfo>? ConvertToLabelInfo(DesignItemDto item)
    {
        // Only PlcLabel items map to DeviceLabelInfo
        if (!item.Type.Equals("PlcLabel", StringComparison.OrdinalIgnoreCase))
            return null;

        var props = item.Props ?? new Dictionary<string, JsonElement>();
        var labelName = GetString(props, "label", $"Item_{item.Id}");
        var address = GetString(props, "address", "--");

        if (string.IsNullOrWhiteSpace(address) || address == "--")
            return null;

        var info = new DeviceLabelInfo(address)
        {
            DefaultValue = GetString(props, "defaultValue", "0"),
            DataType = GetString(props, "dataType", "Word"),
            Divisor = GetInt(props, "divisor", 1),
            StringFormat = GetString(props, "stringFormat", ""),
            FrameShape = GetString(props, "frameShape", "Rectangle"),
            ValueColorTheme = GetString(props, "valueColorTheme", "NeonBlue"),
        };

        // ValueFontSize is handled by PlcDataGridPanel at the view level, not in DeviceLabelInfo

        return new KeyValuePair<string, DeviceLabelInfo>(labelName, info);
    }

    #region JSON helper methods

    private static string GetString(Dictionary<string, JsonElement> props, string key, string fallback)
    {
        if (props.TryGetValue(key, out var el))
        {
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? fallback;
            if (el.ValueKind != JsonValueKind.Null && el.ValueKind != JsonValueKind.Undefined)
                return el.ToString();
        }
        return fallback;
    }

    private static int GetInt(Dictionary<string, JsonElement> props, string key, int fallback)
    {
        if (props.TryGetValue(key, out var el))
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v))
                return v;
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var v2))
                return v2;
        }
        return fallback;
    }

    #endregion

    #region Minimal DTOs for .machinedesign.json deserialization

    private sealed class DesignDocumentDto
    {
        public string Version { get; set; } = "1.0";
        public Dictionary<string, DesignZoneDto> Zones { get; set; } = new();
    }

    private sealed class DesignZoneDto
    {
        public string Title { get; set; } = "";
        public int Columns { get; set; } = 2;
        public int Rows { get; set; }
        public List<DesignItemDto> Items { get; set; } = [];
    }

    private sealed class DesignItemDto
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "PlcLabel";
        public int Order { get; set; }
        public Dictionary<string, JsonElement> Props { get; set; } = new();
    }

    #endregion
}
