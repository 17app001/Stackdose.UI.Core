using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

// ── B4 Behavior JSON Schema ─────────────────────────────────────────────────
// 純資料模型，對應 .machinedesign.json 中控制項的 "events" 陣列。
// 執行邏輯由 B5 BehaviorEngine 實作。
//
// 範例 JSON：
// "events": [
//   {
//     "on": "valueChanged",
//     "when": { "op": ">", "value": 100 },
//     "do": [
//       { "action": "SetProp", "target": "self", "prop": "background", "value": "Red" },
//       { "action": "LogAudit", "message": "溫度超標: {value}" },
//       { "action": "ShowDialog", "title": "警告", "message": "溫度超過 100°C！" }
//     ]
//   }
// ]

/// <summary>
/// 單一行為事件定義：監聽 <see cref="On"/> 觸發，滿足 <see cref="When"/> 條件後依序執行 <see cref="Do"/>。
/// </summary>
public sealed class BehaviorEvent
{
    /// <summary>
    /// 事件觸發來源。<br/>
    /// 已定義值：<c>valueChanged</c>（PLC 數值改變）、<c>click</c>（按鈕點擊）、
    /// <c>connected</c>（PLC 連線建立）、<c>disconnected</c>（連線中斷）。
    /// </summary>
    [JsonPropertyName("on")]
    public string On { get; set; } = "valueChanged";

    /// <summary>
    /// 觸發條件（可省略）。省略時只要事件發生即執行 <see cref="Do"/>。
    /// </summary>
    [JsonPropertyName("when")]
    public BehaviorCondition? When { get; set; }

    /// <summary>條件成立時依序執行的動作清單。</summary>
    [JsonPropertyName("do")]
    public List<BehaviorAction> Do { get; set; } = [];
}

/// <summary>
/// 數值比較條件。<br/>
/// 支援 <c>&gt;</c>、<c>&gt;=</c>、<c>&lt;</c>、<c>&lt;=</c>、<c>==</c>、<c>!=</c>。
/// </summary>
public sealed class BehaviorCondition
{
    /// <summary>比較運算子：<c>&gt;</c> / <c>&gt;=</c> / <c>&lt;</c> / <c>&lt;=</c> / <c>==</c> / <c>!=</c></summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = "==";

    /// <summary>與控制項當前數值比較的參考值。</summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }
}

/// <summary>
/// 單一行為動作。<see cref="Action"/> 決定類型，其餘欄位依類型使用。
/// </summary>
public sealed class BehaviorAction
{
    // ── 動作類型 ───────────────────────────────────────────────────────────
    // SetProp    修改控制項屬性（target + prop + value）
    // WritePlc   寫 PLC 暫存器（target = 位址, value = 數值字串）
    // LogAudit   寫入稽核日誌（message）
    // ShowDialog 顯示對話視窗（title + message）
    // Navigate   切換頁面（page）
    // SetStatus  設定機器狀態（value = 狀態名稱）

    /// <summary>動作類型識別名稱（大小寫不拘）。</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>
    /// 目標控制項 id 或 PLC 位址。<br/>
    /// 特殊值 <c>"self"</c> 代表觸發事件的控制項本身。
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>SetProp 動作：要修改的屬性名稱（對應 props 字典 key）。</summary>
    [JsonPropertyName("prop")]
    public string? Prop { get; set; }

    /// <summary>SetProp / WritePlc / SetStatus：要設定的值（字串表示）。支援 <c>{value}</c> 佔位符。</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>LogAudit / ShowDialog：訊息內文。支援 <c>{value}</c> 佔位符。</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>ShowDialog：對話框標題。</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Navigate：目標頁面識別名稱。</summary>
    [JsonPropertyName("page")]
    public string? Page { get; set; }
}
