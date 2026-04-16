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

    // ── 自由畫布空間屬性 ────────────────────────────────────────
    [JsonPropertyName("x")]
    public double X { get; set; } = 0;

    [JsonPropertyName("y")]
    public double Y { get; set; } = 0;

    [JsonPropertyName("width")]
    public double Width { get; set; } = 180;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 120;

    [JsonPropertyName("locked")]
    public bool IsLocked { get; set; } = false;

    [JsonPropertyName("props")]
    public Dictionary<string, object?> Props { get; set; } = [];

    /// <summary>
    /// 深複製，產生新 Id，可指定位移
    /// </summary>
    public DesignerItemDefinition Clone(double offsetX = 0, double offsetY = 0) => new()
    {
        Id       = Guid.NewGuid().ToString("N")[..8],
        Type     = Type,
        Order    = Order,
        X        = X + offsetX,
        Y        = Y + offsetY,
        Width    = Width,
        Height   = Height,
        IsLocked = false,
        Props    = new Dictionary<string, object?>(Props),
    };
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

    /// <summary>
    /// 從 Props 取出物件陣列（支援 JsonElement 反序列化 + 原生 List）
    /// </summary>
    public static List<Dictionary<string, object?>> GetObjectList(
        this Dictionary<string, object?> props, string key)
    {
        if (!props.TryGetValue(key, out var val) || val is null)
            return [];

        // 原生 List（設計器執行時直接存的）
        if (val is List<object?> rawList)
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var item in rawList)
            {
                if (item is Dictionary<string, object?> dict)
                    result.Add(dict);
            }
            return result;
        }

        // JSON 反序列化後為 JsonElement
        if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var elem in je.EnumerateArray())
            {
                if (elem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in elem.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ValueKind switch
                        {
                            System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                            System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? i : (object)prop.Value.GetDouble(),
                            System.Text.Json.JsonValueKind.True   => true,
                            System.Text.Json.JsonValueKind.False  => false,
                            _ => prop.Value.ToString()
                        };
                    }
                    result.Add(dict);
                }
            }
            return result;
        }

        return [];
    }
}
