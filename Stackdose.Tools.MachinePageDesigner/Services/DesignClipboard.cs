using System.Collections.Generic;
using System.Linq;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Services;

/// <summary>
/// 全域剪貼簿服務，用於主畫布與子設計器（如 TabPanel）之間共享複製內容。
/// </summary>
public static class DesignClipboard
{
    private static List<DesignerItemDefinition> _data = [];

    public static void SetData(IEnumerable<DesignerItemDefinition> items)
    {
        _data = new List<DesignerItemDefinition>(items);
    }

    public static List<DesignerItemDefinition> GetData()
    {
        return _data.Select(i => i.Clone()).ToList();
    }

    public static bool HasData => _data.Count > 0;
    public static int Count => _data.Count;
}
