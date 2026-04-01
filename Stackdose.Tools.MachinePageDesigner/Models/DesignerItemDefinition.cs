using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 單一控制項定義（type + props）
/// </summary>
public sealed class DesignerItemDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("type")]
    public string Type { get; set; } = "PlcLabel";

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("props")]
    public Dictionary<string, object?> Props { get; set; } = [];
}

/// <summary>
/// Props 字典擴充方法，安全取值
/// </summary>
public static class PropsExtensions
{
    public static string GetString(this Dictionary<string, object?> props, string key, string fallback = "")
    {
        if (props.TryGetValue(key, out var val) && val is not null)
        {
            // System.Text.Json 會把 string 反序列化為 JsonElement
            if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
                return je.GetString() ?? fallback;
            return val.ToString() ?? fallback;
        }
        return fallback;
    }

    public static double GetDouble(this Dictionary<string, object?> props, string key, double fallback = 0)
    {
        if (props.TryGetValue(key, out var val) && val is not null)
        {
            if (val is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    return je.GetDouble();
                if (je.ValueKind == System.Text.Json.JsonValueKind.String && double.TryParse(je.GetString(), out var d))
                    return d;
            }
            if (val is double d2) return d2;
            if (val is int i) return i;
            if (double.TryParse(val.ToString(), out var d3)) return d3;
        }
        return fallback;
    }

    public static int GetInt(this Dictionary<string, object?> props, string key, int fallback = 0)
    {
        return (int)props.GetDouble(key, fallback);
    }

    public static bool GetBool(this Dictionary<string, object?> props, string key, bool fallback = false)
    {
        if (props.TryGetValue(key, out var val) && val is not null)
        {
            if (val is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                if (je.ValueKind == System.Text.Json.JsonValueKind.False) return false;
            }
            if (val is bool b) return b;
            if (bool.TryParse(val.ToString(), out var b2)) return b2;
        }
        return fallback;
    }
}
