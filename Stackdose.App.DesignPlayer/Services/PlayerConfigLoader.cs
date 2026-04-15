using Stackdose.App.DesignPlayer.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.DesignPlayer.Services;

public static class PlayerConfigLoader
{
    private static readonly JsonSerializerOptions s_opts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
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
}
