namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// 控制項模板描述器：包含多個預設控制項的組合，可一次拖放到畫布
/// </summary>
public sealed class TemplateDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public string Description { get; init; } = "";
    public string Icon { get; init; } = "📋";

    /// <summary>模板包含的控制項定義（帶相對位置）</summary>
    public required List<DesignerItemDefinition> Items { get; init; }

    /// <summary>建議畫布最小寬度（僅供參考）</summary>
    public int? RecommendedWidth { get; init; }

    /// <summary>建議畫布最小高度（僅供參考）</summary>
    public int? RecommendedHeight { get; init; }

    /// <summary>是否為使用者自建模板</summary>
    public bool IsUserTemplate { get; init; }

    /// <summary>
    /// 將模板項目複製並偏移到指定位置
    /// </summary>
    public List<DesignerItemDefinition> CreateInstances(double baseX, double baseY)
    {
        // 計算模板內容的原點偏移量
        double minX = Items.Count > 0 ? Items.Min(i => i.X) : 0;
        double minY = Items.Count > 0 ? Items.Min(i => i.Y) : 0;

        return Items.Select(item =>
        {
            var clone = item.Clone();
            clone.X = baseX + (item.X - minX);
            clone.Y = baseY + (item.Y - minY);
            return clone;
        }).ToList();
    }
}
