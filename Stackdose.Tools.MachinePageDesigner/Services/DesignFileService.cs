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
        var doc = JsonSerializer.Deserialize<DesignDocument>(json, _options) ?? new DesignDocument();
        NormalizePages(doc);
        return doc;
    }

    public static void Save(DesignDocument doc, string filePath)
    {
        doc.Meta.ModifiedAt = DateTime.UtcNow;

        // 同步 Legacy 欄位（DesignRuntime / DesignPlayer 讀第一頁用）
        if (doc.Pages is { Count: > 0 })
        {
            var first = doc.Pages[0];
            doc.CanvasItems  = first.CanvasItems;
            doc.CanvasWidth  = first.CanvasWidth;
            doc.CanvasHeight = first.CanvasHeight;
        }

        var json = JsonSerializer.Serialize(doc, _options);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
    }

    public static DesignDocument CreateNew(string title = "Machine Page", string machineId = "M1")
    {
        var doc = new DesignDocument
        {
            Meta = new DesignMeta
            {
                Title = title,
                MachineId = machineId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }
        };
        NormalizePages(doc);
        return doc;
    }

    /// <summary>
    /// 確保 Pages 一定存在（v1.0 舊檔或新建文件均補上預設第一頁）
    /// </summary>
    private static void NormalizePages(DesignDocument doc)
    {
        if (doc.Pages is { Count: > 0 }) return;

        // v1.0 舊格式或空文件 → 轉換為單頁
        doc.Pages =
        [
            new DesignPage
            {
                PageId      = Guid.NewGuid().ToString("N")[..8],
                Name        = "Main",
                CanvasWidth  = doc.CanvasWidth  > 0 ? doc.CanvasWidth  : 1200,
                CanvasHeight = doc.CanvasHeight > 0 ? doc.CanvasHeight : 750,
                CanvasItems  = doc.CanvasItems ?? [],
            }
        ];
    }
}
