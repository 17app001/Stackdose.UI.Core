namespace Stackdose.App.ShellShared.Behaviors;

/// <summary>
/// 存入 WPF 控制項 Tag 屬性的執行期中繼資料。
/// RuntimeControlFactory 建立控制項時設定；BehaviorEngine 讀取。
/// </summary>
public sealed class ControlRuntimeTag
{
    /// <summary>對應 DesignerItemDefinition.Id。</summary>
    public required string Id { get; init; }

    /// <summary>
    /// prop 名稱（小寫）→ 值字串 setter。<br/>
    /// RuntimeControlFactory 針對每個控制項類型預先注入可修改的 props，
    /// 例如 "background" → <c>v => label.Background = ParseBrush(v)</c>。
    /// SetPropHandler 透過此字典執行 SetProp 動作。
    /// </summary>
    public Dictionary<string, Action<string>> PropSetters { get; init; } = [];
}
