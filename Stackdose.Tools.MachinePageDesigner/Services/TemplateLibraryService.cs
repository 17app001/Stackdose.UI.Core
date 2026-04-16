using System.IO;
using System.Text.Json;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 模板庫服務：管理內建模板與使用者自訂模板
/// </summary>
public static class TemplateLibraryService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static string UserTemplateDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Stackdose", "MachinePageDesigner", "Templates");

    // ── Built-in Templates ──────────────────────────────────────────

    public static IReadOnlyList<TemplateDescriptor> BuiltInTemplates { get; } = CreateBuiltInTemplates();

    private static List<TemplateDescriptor> CreateBuiltInTemplates() =>
    [
        // ── Monitoring 類 ──
        new()
        {
            Id = "tpl-temp-monitor",
            Name = "溫度監控組",
            Category = "Monitoring",
            Description = "標題 + 當前溫度 + 上下限，適合單一溫控顯示",
            Icon = "🌡️",
            Items =
            [
                new() { Type = "StaticLabel", X = 0, Y = 0, Width = 200, Height = 32,
                    Props = new() { ["staticText"] = "溫度監控", ["staticFontSize"] = 16.0, ["staticFontWeight"] = "Bold", ["staticTextAlign"] = "Left", ["staticForeground"] = "#E2E2F0" } },
                new() { Type = "PlcLabel", X = 0, Y = 40, Width = 200, Height = 120,
                    Props = new() { ["label"] = "當前溫度", ["address"] = "D100", ["defaultValue"] = "0", ["valueFontSize"] = 28.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 10.0, ["stringFormat"] = "F1" } },
                new() { Type = "PlcLabel", X = 0, Y = 170, Width = 95, Height = 90,
                    Props = new() { ["label"] = "下限", ["address"] = "D101", ["defaultValue"] = "0", ["valueFontSize"] = 16.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 10.0, ["stringFormat"] = "F1" } },
                new() { Type = "PlcLabel", X = 105, Y = 170, Width = 95, Height = 90,
                    Props = new() { ["label"] = "上限", ["address"] = "D102", ["defaultValue"] = "0", ["valueFontSize"] = 16.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 10.0, ["stringFormat"] = "F1" } },
            ]
        },
        new()
        {
            Id = "tpl-pressure-monitor",
            Name = "壓力監控組",
            Category = "Monitoring",
            Description = "壓力值 + 狀態燈號，適合氣壓/液壓監控",
            Icon = "🔵",
            Items =
            [
                new() { Type = "StaticLabel", X = 0, Y = 0, Width = 200, Height = 32,
                    Props = new() { ["staticText"] = "壓力監控", ["staticFontSize"] = 16.0, ["staticFontWeight"] = "Bold", ["staticTextAlign"] = "Left", ["staticForeground"] = "#E2E2F0" } },
                new() { Type = "PlcLabel", X = 0, Y = 40, Width = 200, Height = 120,
                    Props = new() { ["label"] = "壓力值", ["address"] = "D200", ["defaultValue"] = "0", ["valueFontSize"] = 28.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 100.0, ["stringFormat"] = "F2" } },
                new() { Type = "PlcStatusIndicator", X = 0, Y = 170, Width = 200, Height = 50,
                    Props = new() { ["label"] = "壓力正常", ["displayAddress"] = "M200" } },
            ]
        },
        new()
        {
            Id = "tpl-counter-display",
            Name = "計數器顯示",
            Category = "Monitoring",
            Description = "生產計數 + 目標數 + 不良數，適合產線計數",
            Icon = "🔢",
            Items =
            [
                new() { Type = "StaticLabel", X = 0, Y = 0, Width = 320, Height = 32,
                    Props = new() { ["staticText"] = "生產計數", ["staticFontSize"] = 16.0, ["staticFontWeight"] = "Bold", ["staticTextAlign"] = "Left", ["staticForeground"] = "#E2E2F0" } },
                new() { Type = "PlcLabel", X = 0, Y = 40, Width = 100, Height = 100,
                    Props = new() { ["label"] = "完成數", ["address"] = "D300", ["defaultValue"] = "0", ["valueFontSize"] = 22.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 1.0, ["stringFormat"] = "F0" } },
                new() { Type = "PlcLabel", X = 110, Y = 40, Width = 100, Height = 100,
                    Props = new() { ["label"] = "目標數", ["address"] = "D301", ["defaultValue"] = "0", ["valueFontSize"] = 22.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 1.0, ["stringFormat"] = "F0" } },
                new() { Type = "PlcLabel", X = 220, Y = 40, Width = 100, Height = 100,
                    Props = new() { ["label"] = "不良數", ["address"] = "D302", ["defaultValue"] = "0", ["valueFontSize"] = 22.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 1.0, ["stringFormat"] = "F0" } },
            ]
        },

        // ── Control 類 ──
        new()
        {
            Id = "tpl-start-stop",
            Name = "啟動/停止控制",
            Category = "Control",
            Description = "運轉狀態燈 + 啟動按鈕 + 停止按鈕",
            Icon = "⏯️",
            Items =
            [
                new() { Type = "PlcStatusIndicator", X = 0, Y = 0, Width = 220, Height = 50,
                    Props = new() { ["label"] = "運轉狀態", ["displayAddress"] = "M0" } },
                new() { Type = "SecuredButton", X = 0, Y = 60, Width = 105, Height = 50,
                    Props = new() { ["label"] = "啟動", ["commandAddress"] = "M10", ["requiredLevel"] = "Operator", ["theme"] = "Success", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 300.0 } },
                new() { Type = "SecuredButton", X = 115, Y = 60, Width = 105, Height = 50,
                    Props = new() { ["label"] = "停止", ["commandAddress"] = "M11", ["requiredLevel"] = "Operator", ["theme"] = "Danger", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 300.0 } },
            ]
        },
        new()
        {
            Id = "tpl-emergency-stop",
            Name = "緊急停止",
            Category = "Control",
            Description = "大型緊急停止按鈕 + 狀態顯示，需 Supervisor 權限",
            Icon = "🛑",
            Items =
            [
                new() { Type = "PlcStatusIndicator", X = 0, Y = 0, Width = 200, Height = 50,
                    Props = new() { ["label"] = "安全狀態", ["displayAddress"] = "M50" } },
                new() { Type = "SecuredButton", X = 0, Y = 60, Width = 200, Height = 60,
                    Props = new() { ["label"] = "緊急停止", ["commandAddress"] = "M100", ["requiredLevel"] = "Supervisor", ["theme"] = "Danger", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 500.0 } },
            ]
        },
        new()
        {
            Id = "tpl-valve-toggle",
            Name = "閥門切換控制",
            Category = "Control",
            Description = "閥門狀態 + Toggle 按鈕，一鍵開關",
            Icon = "🔧",
            Items =
            [
                new() { Type = "PlcStatusIndicator", X = 0, Y = 0, Width = 180, Height = 50,
                    Props = new() { ["label"] = "閥門狀態", ["displayAddress"] = "M30" } },
                new() { Type = "SecuredButton", X = 0, Y = 60, Width = 180, Height = 50,
                    Props = new() { ["label"] = "切換閥門", ["commandAddress"] = "M30", ["requiredLevel"] = "Operator", ["theme"] = "Primary", ["writeValue"] = "1", ["commandType"] = "toggle", ["pulseMs"] = 300.0 } },
            ]
        },

        // ── Dashboard 類 ──
        new()
        {
            Id = "tpl-machine-overview",
            Name = "設備總覽面板",
            Category = "Dashboard",
            Description = "運轉狀態 + 三組參數 + 啟停按鈕，完整單機面板",
            Icon = "📊",
            Items =
            [
                new() { Type = "Spacer", X = 0, Y = 0, Width = 440, Height = 320,
                    Props = new() { ["title"] = "設備名稱" } },
                new() { Type = "PlcStatusIndicator", X = 10, Y = 40, Width = 420, Height = 50,
                    Props = new() { ["label"] = "運轉狀態", ["displayAddress"] = "M0" } },
                new() { Type = "PlcLabel", X = 10, Y = 100, Width = 130, Height = 100,
                    Props = new() { ["label"] = "溫度", ["address"] = "D100", ["defaultValue"] = "0", ["valueFontSize"] = 20.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 10.0, ["stringFormat"] = "F1" } },
                new() { Type = "PlcLabel", X = 150, Y = 100, Width = 130, Height = 100,
                    Props = new() { ["label"] = "壓力", ["address"] = "D200", ["defaultValue"] = "0", ["valueFontSize"] = 20.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 100.0, ["stringFormat"] = "F2" } },
                new() { Type = "PlcLabel", X = 290, Y = 100, Width = 130, Height = 100,
                    Props = new() { ["label"] = "速度", ["address"] = "D300", ["defaultValue"] = "0", ["valueFontSize"] = 20.0, ["frameShape"] = "Rectangle", ["valueColorTheme"] = "NeonBlue", ["divisor"] = 1.0, ["stringFormat"] = "F0" } },
                new() { Type = "SecuredButton", X = 10, Y = 210, Width = 130, Height = 45,
                    Props = new() { ["label"] = "啟動", ["commandAddress"] = "M10", ["requiredLevel"] = "Operator", ["theme"] = "Success", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 300.0 } },
                new() { Type = "SecuredButton", X = 150, Y = 210, Width = 130, Height = 45,
                    Props = new() { ["label"] = "停止", ["commandAddress"] = "M11", ["requiredLevel"] = "Operator", ["theme"] = "Danger", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 300.0 } },
                new() { Type = "SecuredButton", X = 290, Y = 210, Width = 130, Height = 45,
                    Props = new() { ["label"] = "重置", ["commandAddress"] = "M12", ["requiredLevel"] = "Supervisor", ["theme"] = "Warning", ["writeValue"] = "1", ["commandType"] = "pulse", ["pulseMs"] = 300.0 } },
            ]
        },
        new()
        {
            Id = "tpl-log-alarm-panel",
            Name = "日誌 + 警報面板",
            Category = "Dashboard",
            Description = "LiveLog + AlarmViewer 組合，適合放在頁面底部",
            Icon = "📋",
            Items =
            [
                new() { Type = "LiveLog", X = 0, Y = 0, Width = 420, Height = 200,
                    Props = [] },
                new() { Type = "AlarmViewer", X = 430, Y = 0, Width = 420, Height = 200,
                    Props = new() { ["configFile"] = "" } },
            ]
        },
    ];

    // ── User Templates CRUD ─────────────────────────────────────────

    public static List<TemplateDescriptor> LoadUserTemplates()
    {
        if (!Directory.Exists(UserTemplateDir))
            return [];

        var templates = new List<TemplateDescriptor>();
        foreach (var file in Directory.GetFiles(UserTemplateDir, "*.template.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var tpl = JsonSerializer.Deserialize<TemplateDescriptor>(json, _jsonOpts);
                if (tpl != null)
                    templates.Add(tpl);
            }
            catch { /* skip corrupt files */ }
        }
        return templates;
    }

    public static void SaveUserTemplate(TemplateDescriptor template)
    {
        Directory.CreateDirectory(UserTemplateDir);
        var path = Path.Combine(UserTemplateDir, $"{template.Id}.template.json");
        var json = JsonSerializer.Serialize(template, _jsonOpts);
        File.WriteAllText(path, json);
    }

    public static void DeleteUserTemplate(string templateId)
    {
        var path = Path.Combine(UserTemplateDir, $"{templateId}.template.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    /// <summary>取得所有模板（內建 + 使用者）</summary>
    public static List<TemplateDescriptor> GetAllTemplates()
    {
        var all = new List<TemplateDescriptor>(BuiltInTemplates);
        all.AddRange(LoadUserTemplates());
        return all;
    }
}
