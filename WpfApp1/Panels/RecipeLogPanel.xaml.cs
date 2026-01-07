using System.Windows.Controls;

namespace WpfApp1.Panels
{
    /// <summary>
    /// Recipe management and log viewer panel
    /// </summary>
    /// <remarks>
    /// Provides:
    /// <list type="bullet">
    /// <item>Recipe management (RecipeLoader)</item>
    /// <item>Real-time system logs (LiveLogViewer)</item>
    /// </list>
    /// </remarks>
    public partial class RecipeLogPanel : UserControl
    {
        public RecipeLogPanel()
        {
            InitializeComponent();
        }
    }
}
