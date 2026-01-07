using System.Windows.Controls;

namespace WpfApp1.Panels
{
    /// <summary>
    /// Main control panel for industrial operations
    /// </summary>
    public partial class MainPanel : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MainPanel()
        {
            InitializeComponent();

            // ?? PrintHeadPanel 現在會自動掃描 Resources 目錄
            // 不再需要手動設定 PrintHeadConfigs
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[MainPanel] Initialized - PrintHeadPanel will auto-load configurations");
            #endif
        }
    }
}
