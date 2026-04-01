using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Services;

/// <summary>
/// Load / Save .machinedesign.json
/// </summary>
public static class DesignFileService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static DesignDocument Load(string filePath)
    {
        var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<DesignDocument>(json, _options) ?? new DesignDocument();
    }

    public static void Save(DesignDocument doc, string filePath)
    {
        doc.Meta.ModifiedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(doc, _options);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
    }

    public static DesignDocument CreateNew(string title = "Machine Page", string machineId = "M1")
    {
        return new DesignDocument
        {
            Meta = new DesignMeta
            {
                Title = title,
                MachineId = machineId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        };
    }
}
