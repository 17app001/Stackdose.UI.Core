using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;

namespace Stackdose.Tools.MachinePageDesigner.ViewModels;

/// <summary>
/// 代表設計器中的一個頁面頁籤（包含獨立 Canvas + UndoRedo）
/// </summary>
public sealed class PageTabViewModel : ObservableObject
{
    private string _name;
    private bool _isActive;
    private bool _isEditing;

    public string PageId { get; }

    public string Name
    {
        get => _name;
        set
        {
            var v = string.IsNullOrWhiteSpace(value) ? "Page" : value.Trim();
            Set(ref _name, v);
        }
    }

    /// <summary>此頁籤是否為目前選取頁面</summary>
    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    /// <summary>是否正在內嵌重新命名</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => Set(ref _isEditing, value);
    }

    public DesignCanvasViewModel Canvas { get; } = new();
    public UndoRedoService UndoRedo { get; } = new();

    public PageTabViewModel(DesignPage page)
    {
        PageId = page.PageId;
        _name  = page.Name;
        Canvas.LoadFromPage(page);
    }

    /// <summary>將目前狀態匯出為 DesignPage（供 Save 使用）</summary>
    public DesignPage Export()
    {
        var page = new DesignPage
        {
            PageId = PageId,
            Name   = Name,
        };
        Canvas.ExportToPage(page);
        return page;
    }
}
