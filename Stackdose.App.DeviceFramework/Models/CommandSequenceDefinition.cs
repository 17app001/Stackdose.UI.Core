using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// 指令序列定義：多步驟 PLC 指令的 JSON DSL
/// </summary>
public sealed class CommandSequenceDefinition
{
    [JsonPropertyName("steps")]
    public List<SequenceStep> Steps { get; set; } = [];

    [JsonPropertyName("rollback")]
    public List<SequenceStep>? Rollback { get; set; }

    [JsonPropertyName("onError")]
    public string OnError { get; set; } = "stop"; // "stop" | "rollback" | "continue"
}

/// <summary>
/// 序列步驟（多型基底，用 type 欄位判斷子類型）
/// </summary>
[JsonDerivedType(typeof(WriteStep), "write")]
[JsonDerivedType(typeof(ReadStep), "read")]
[JsonDerivedType(typeof(WaitStep), "wait")]
[JsonDerivedType(typeof(ConditionalStep), "conditional")]
[JsonDerivedType(typeof(ReadWaitStep), "readWait")]
public abstract class SequenceStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

/// <summary>寫入 PLC 位址</summary>
public sealed class WriteStep : SequenceStep
{
    [JsonPropertyName("type")]
    public override string Type => "write";

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "1";
}

/// <summary>讀取 PLC 位址並存入變數</summary>
public sealed class ReadStep : SequenceStep
{
    [JsonPropertyName("type")]
    public override string Type => "read";

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("variable")]
    public string Variable { get; set; } = "";

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "word"; // "word" | "bit"
}

/// <summary>延遲等待</summary>
public sealed class WaitStep : SequenceStep
{
    [JsonPropertyName("type")]
    public override string Type => "wait";

    [JsonPropertyName("delayMs")]
    public int DelayMs { get; set; } = 500;
}

/// <summary>條件分支：依運算式結果執行不同步驟</summary>
public sealed class ConditionalStep : SequenceStep
{
    [JsonPropertyName("type")]
    public override string Type => "conditional";

    /// <summary>條件運算式，例如 "${temp} > 150"</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = "";

    [JsonPropertyName("onTrue")]
    public List<SequenceStep> OnTrue { get; set; } = [];

    [JsonPropertyName("onFalse")]
    public List<SequenceStep>? OnFalse { get; set; }
}

/// <summary>輪詢讀取直到條件成立或超時</summary>
public sealed class ReadWaitStep : SequenceStep
{
    [JsonPropertyName("type")]
    public override string Type => "readWait";

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    /// <summary>條件運算式，例如 "${address} > 0"</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = "";

    [JsonPropertyName("maxTimeoutMs")]
    public int MaxTimeoutMs { get; set; } = 5000;

    [JsonPropertyName("pollIntervalMs")]
    public int PollIntervalMs { get; set; } = 200;
}

/// <summary>
/// 序列步驟的 JSON 多型序列化支援
/// </summary>
public static class SequenceStepSerializer
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new SequenceStepConverter() },
    };

    public static CommandSequenceDefinition? Deserialize(string json)
        => JsonSerializer.Deserialize<CommandSequenceDefinition>(json, _opts);

    public static string Serialize(CommandSequenceDefinition def)
        => JsonSerializer.Serialize(def, _opts);

    /// <summary>
    /// 自訂轉換器：根據 "type" 欄位反序列化為正確的子類型
    /// </summary>
    private sealed class SequenceStepConverter : JsonConverter<SequenceStep>
    {
        public override SequenceStep? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var tp)
                ? tp.GetString() ?? "write"
                : "write";

            var rawJson = root.GetRawText();

            // 使用不帶自訂轉換器的選項避免遞迴
            var plainOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            return type switch
            {
                "write"       => JsonSerializer.Deserialize<WriteStep>(rawJson, plainOpts),
                "read"        => JsonSerializer.Deserialize<ReadStep>(rawJson, plainOpts),
                "wait"        => JsonSerializer.Deserialize<WaitStep>(rawJson, plainOpts),
                "conditional" => DeserializeConditional(root, plainOpts),
                "readWait"    => JsonSerializer.Deserialize<ReadWaitStep>(rawJson, plainOpts),
                _             => JsonSerializer.Deserialize<WriteStep>(rawJson, plainOpts),
            };
        }

        private static ConditionalStep? DeserializeConditional(JsonElement root, JsonSerializerOptions plainOpts)
        {
            var step = new ConditionalStep();
            if (root.TryGetProperty("id", out var id)) step.Id = id.GetString() ?? "";
            if (root.TryGetProperty("description", out var desc)) step.Description = desc.GetString() ?? "";
            if (root.TryGetProperty("condition", out var cond)) step.Condition = cond.GetString() ?? "";

            var converter = new SequenceStepConverter();

            if (root.TryGetProperty("onTrue", out var onTrue) && onTrue.ValueKind == JsonValueKind.Array)
            {
                step.OnTrue = [];
                foreach (var elem in onTrue.EnumerateArray())
                {
                    var raw = elem.GetRawText();
                    var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(raw));
                    reader.Read();
                    var sub = converter.Read(ref reader, typeof(SequenceStep), plainOpts);
                    if (sub != null) step.OnTrue.Add(sub);
                }
            }

            if (root.TryGetProperty("onFalse", out var onFalse) && onFalse.ValueKind == JsonValueKind.Array)
            {
                step.OnFalse = [];
                foreach (var elem in onFalse.EnumerateArray())
                {
                    var raw = elem.GetRawText();
                    var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(raw));
                    reader.Read();
                    var sub = converter.Read(ref reader, typeof(SequenceStep), plainOpts);
                    if (sub != null) step.OnFalse.Add(sub);
                }
            }

            return step;
        }

        public override void Write(Utf8JsonWriter writer, SequenceStep value, JsonSerializerOptions options)
        {
            var plainOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            JsonSerializer.Serialize(writer, value, value.GetType(), plainOpts);
        }
    }
}
