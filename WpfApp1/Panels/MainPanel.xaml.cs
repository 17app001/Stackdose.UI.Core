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

            // ?? PrintHeadPanel �{�b�|�۰ʱ��y Resources �ؿ�
            // ���A�ݭn��ʳ]�w PrintHeadConfigs
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[MainPanel] Initialized - PrintHeadPanel will auto-load configurations");
            #endif
        }
    }
}
