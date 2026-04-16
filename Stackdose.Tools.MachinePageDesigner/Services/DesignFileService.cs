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

    private static readonly JsonSerializerOptions _exportOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        // 匯出嵌入式的 alarms.json / sensors.json
        ExportEmbeddedConfigs(doc, dir ?? ".");
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

    /// <summary>
    /// 掃描所有頁面的 AlarmViewer / SensorViewer，匯出嵌入式 JSON 設定檔
    /// </summary>
    private static void ExportEmbeddedConfigs(DesignDocument doc, string outputDir)
    {
        var allItems = doc.Pages?.SelectMany(p => p.CanvasItems) ?? [];

        // ── Alarms ──
        var alarmEntries = new List<Dictionary<string, object?>>();
        foreach (var item in allItems.Where(i => i.Type == "AlarmViewer"))
        {
            // 只在 configFile 為空時，才從嵌入式項目匯出
            var configFile = item.Props.GetString("configFile");
            if (!string.IsNullOrWhiteSpace(configFile)) continue;

            var embedded = item.Props.GetObjectList("alarmItems");
            alarmEntries.AddRange(embedded);
        }
        if (alarmEntries.Count > 0)
        {
            var alarmsDoc = new { alarms = alarmEntries };
            var alarmsJson = JsonSerializer.Serialize(alarmsDoc, _exportOptions);
            File.WriteAllText(Path.Combine(outputDir, "alarms.json"), alarmsJson, System.Text.Encoding.UTF8);
        }

        // ── Sensors ──
        var sensorEntries = new List<Dictionary<string, object?>>();
        foreach (var item in allItems.Where(i => i.Type == "SensorViewer"))
        {
            var configFile = item.Props.GetString("configFile");
            if (!string.IsNullOrWhiteSpace(configFile)) continue;

            var embedded = item.Props.GetObjectList("sensorItems");
            sensorEntries.AddRange(embedded);
        }
        if (sensorEntries.Count > 0)
        {
            // sensors.json 是扁平陣列格式（不像 alarms 包在物件中）
            var sensorsJson = JsonSerializer.Serialize(sensorEntries, _exportOptions);
            File.WriteAllText(Path.Combine(outputDir, "sensors.json"), sensorsJson, System.Text.Encoding.UTF8);
        }
    }
}
