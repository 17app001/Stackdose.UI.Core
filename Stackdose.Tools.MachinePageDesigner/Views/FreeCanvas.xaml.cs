using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.ViewModels;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class FreeCanvas : UserControl
{
    public FreeCanvas()
    {
        InitializeComponent();
    }

    private MainViewModel? MainVm => Tag as MainViewModel;

    // ── Toolbox Drop ─────────────────────────────────────────────────────

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("ToolboxItem")
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("ToolboxItem") is not ToolboxItemDescriptor desc) return;
        if (MainVm == null) return;

        var pos = e.GetPosition(designCanvas);
        var def = desc.CreateDefinition();

        // Centre item on drop point, clamp to canvas bounds
        def.X = Math.Max(0, Math.Min(pos.X - def.Width / 2, MainVm.Canvas.CanvasWidth - def.Width));
        def.Y = Math.Max(0, Math.Min(pos.Y - def.Height / 2, MainVm.Canvas.CanvasHeight - def.Height));

        var vm = new DesignerItemViewModel(def);
        MainVm.ExecuteCanvasAddItem(vm);
        MainVm.Canvas.SelectedItem = vm;
        e.Handled = true;
    }

    // ── Background Click → Deselect ──────────────────────────────────────

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.Source, designCanvas))
            MainVm?.Canvas.ClearSelection();
    }
}
