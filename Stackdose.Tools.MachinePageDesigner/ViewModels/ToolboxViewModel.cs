using System.Collections.ObjectModel;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 工具箱 ViewModel：可用控制項清單
/// </summary>
public sealed class ToolboxViewModel : ObservableObject
{
    public ObservableCollection<ToolboxItemDescriptor> Items { get; } =
        new(ToolboxItemDescriptor.All);

    /// <summary>
    /// 依 Category 分群的項目
    /// </summary>
    public IEnumerable<IGrouping<string, ToolboxItemDescriptor>> GroupedItems
        => Items.GroupBy(i => i.Category);
}
