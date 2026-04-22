using Stackdose.UI.Core.Models;
using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 單一控制項定義（type + props）
/// </summary>
public sealed class DesignerItemDefinition : IControlWithBehaviors
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
    /// 行為事件清單（B4 新增）。<br/>
    /// 每個事件定義：觸發來源 <c>on</c>、條件 <c>when</c>、動作 <c>do</c>。<br/>
    /// 省略時為空清單，舊 JSON 完全向後相容。
    /// </summary>
    [JsonPropertyName("events")]
    public List<BehaviorEvent> Events { get; set; } = [];

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
        Events   = Events.Select(e => new BehaviorEvent
        {
            On   = e.On,
            When = e.When is null ? null : new BehaviorCondition { Op = e.When.Op, Value = e.When.Value },
            Do   = e.Do.Select(a => new BehaviorAction
            {
                Action  = a.Action,
                Target  = a.Target,
                Prop    = a.Prop,
                Value   = a.Value,
                Message = a.Message,
                Title   = a.Title,
                Page    = a.Page,
            }).ToList(),
        }).ToList(),
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
