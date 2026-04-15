using Stackdose.App.DesignPlayer.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stackdose.App.DesignPlayer.Services;

public static class PlayerConfigLoader
{
    private static readonly JsonSerializerOptions s_opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly JsonSerializerOptions s_writeOpts = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// 讀取 app-config.json。檔案不存在時回傳預設值（不丟例外），
    /// 方便第一次執行不需手動建檔。
    /// </summary>
    public static PlayerAppConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            return new PlayerAppConfig();

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<PlayerAppConfig>(json, s_opts)
                   ?? new PlayerAppConfig();
        }
        catch
        {
            return new PlayerAppConfig();
        }
    }

    /// <summary>
    /// 將 config 序列化並寫回檔案（目錄不存在時自動建立）。
    /// </summary>
    public static void Save(string configPath, PlayerAppConfig config)
    {
        var dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, s_writeOpts);
        File.WriteAllText(configPath, json);
    }
}
