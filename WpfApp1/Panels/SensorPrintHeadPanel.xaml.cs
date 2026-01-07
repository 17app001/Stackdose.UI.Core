using System.Windows.Controls;

namespace WpfApp1.Panels
{
    /// <summary>
    /// Sensor and PrintHead monitoring panel
    /// </summary>
    /// <remarks>
    /// Provides:
    /// <list type="bullet">
    /// <item>Sensor status monitoring (SensorViewer)</item>
    /// <item>PrintHead status display (PrintHeadStatus)</item>
    /// </list>
    /// </remarks>
    public partial class SensorPrintHeadPanel : UserControl
    {
        public SensorPrintHeadPanel()
        {
            InitializeComponent();
        }
    }
}
