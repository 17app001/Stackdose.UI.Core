using System.IO;
using System.Text;
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

        // 輸出 Tags 使用報告（只在有定義 Tags 時產生）
        if (doc.Tags.Count > 0)
            ExportTagsReport(doc, dir ?? ".", Path.GetFileNameWithoutExtension(filePath));
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

    /// <summary>
    /// 輸出 Tags 使用報告：列出已定義 Tags 與設計稿中使用的地址，並標記未對應的地址。
    /// 輸出到 {designName}.tags-report.txt，供工程師上線前驗證 PLC 配線。
    /// </summary>
    private static void ExportTagsReport(DesignDocument doc, string outputDir, string designName)
    {
        var allItems = doc.Pages?.SelectMany(p => p.CanvasItems) ?? [];

        // 收集設計稿所有 PLC 地址（PlcLabel/PlcText address、PlcStatusIndicator displayAddress、SecuredButton commandAddress）
        var usedAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in allItems)
        {
            AddAddress(usedAddresses, item.Props.GetString("address"));
            AddAddress(usedAddresses, item.Props.GetString("displayAddress"));
            AddAddress(usedAddresses, item.Props.GetString("commandAddress"));
        }

        var tagDict = doc.Tags.ToDictionary(t => t.Address, t => t, StringComparer.OrdinalIgnoreCase);
        var undefined = usedAddresses.Where(a => !tagDict.ContainsKey(a)).OrderBy(a => a).ToList();
        var definedUsed = doc.Tags.Count(t => usedAddresses.Contains(t.Address));

        var sb = new StringBuilder();
        sb.AppendLine("# PLC Tags 使用報告");
        sb.AppendLine($"# 設計稿：{doc.Meta.Title}  ({designName}.machinedesign.json)");
        sb.AppendLine($"# 機台 ID：{doc.Meta.MachineId}");
        sb.AppendLine($"# 產生時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# 頁面數：{doc.Pages?.Count ?? 0}");
        sb.AppendLine();

        sb.AppendLine($"## 已定義 Tags（{doc.Tags.Count} 筆）");
        sb.AppendLine($"{"地址",-12} {"名稱",-24} {"單位",-8} 使用中");
        sb.AppendLine(new string('-', 58));
        foreach (var tag in doc.Tags.OrderBy(t => t.Address))
        {
            var inUse = usedAddresses.Contains(tag.Address) ? "✓" : "—";
            sb.AppendLine($"{tag.Address,-12} {tag.Name,-24} {tag.Unit,-8} {inUse}");
        }
        sb.AppendLine();

        if (undefined.Count > 0)
        {
            sb.AppendLine($"## ⚠ 使用中但未定義 Tag 的地址（{undefined.Count} 筆）");
            sb.AppendLine("   請確認這些地址是否正確，或在 Tags 清單補充定義：");
            foreach (var addr in undefined)
                sb.AppendLine($"   {addr}");
            sb.AppendLine();
        }

        sb.AppendLine("## 統計");
        sb.AppendLine($"   定義 Tags：{doc.Tags.Count} 筆（使用中 {definedUsed}，未使用 {doc.Tags.Count - definedUsed}）");
        sb.AppendLine($"   設計稿 PLC 地址：{usedAddresses.Count} 個（已定義 {usedAddresses.Count - undefined.Count}，未定義 {undefined.Count}）");

        var reportPath = Path.Combine(outputDir, $"{designName}.tags-report.txt");
        File.WriteAllText(reportPath, sb.ToString(), System.Text.Encoding.UTF8);
    }

    private static void AddAddress(HashSet<string> set, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) set.Add(value.Trim());
    }
}
