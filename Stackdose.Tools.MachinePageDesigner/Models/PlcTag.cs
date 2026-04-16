using System.Text.Json.Serialization;

namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// PLC 標籤定義：地址 → 名稱對照。
/// 設計師在 PropertyPanel 的地址欄可從下拉選取，減少打錯的機率。
/// </summary>
public sealed class PlcTag
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>工程單位（選填），如 ℃、rpm、bar</summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "";

    /// <summary>ComboBox 選取後填入的文字</summary>
    public override string ToString() => Address;
}
