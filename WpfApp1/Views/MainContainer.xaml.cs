using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Templates.Controls;

namespace WpfApp1.Views
{
    /// <summary>
    /// MainContainer.xaml 的互動邏輯
    /// 主容器頁面，包含固定的 Header、Navigation、BottomBar
    /// 中間區域根據選擇的功能動態切換
    /// </summary>
    public partial class MainContainer : UserControl
    {
        public MainContainer()
        {
            InitializeComponent();

            // 預設顯示主控制面板
            NavigateToMainPanel();
        }

        /// <summary>
        /// 處理 Header 登出按鈕點擊事件
        /// </summary>
        private void OnLogout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Logout clicked!", "UBI System", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 處理最小化按鈕事件
        /// </summary>
        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        /// <summary>
        /// 處理關閉按鈕事件
        /// </summary>
        private void OnClose(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to close the UBI System?",
                "Close Application",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Close();
                }
            }
        }

        /// <summary>
        /// 處理導航項目點擊事件
        /// 根據選擇的功能切換中間內容區域
        /// </summary>
        private void OnNavigate(object sender, NavigationItem e)
        {
            switch (e.NavigationTarget)
            {
                case "MainPanel":
                    NavigateToMainPanel();
                    break;
                case "SystemTestPanel":
                    NavigateToSystemTestPanel();
                    break;
                case "MachinePage":
                    NavigateToMachinePage();
                    break;
                default:
                    NavigateToMainPanel();
                    break;
            }
        }

        /// <summary>
        /// 導航到主控制面板
        /// </summary>
        private void NavigateToMainPanel()
        {
            // 這裡可以創建或載入 MainPanel
            // 目前先用一個簡單的占位頁面
            var mainPanel = new MainPanelPage();
            ContentArea.Content = mainPanel;
            UpdatePageTitle("Main Control Panel");
        }

        /// <summary>
        /// 導航到系統測試面板
        /// </summary>
        private void NavigateToSystemTestPanel()
        {
            // 這裡可以創建或載入 SystemTestPanel
            // 目前先用一個簡單的占位頁面
            var systemTestPanel = new SystemTestPanelPage();
            ContentArea.Content = systemTestPanel;
            UpdatePageTitle("System Test Panel");
        }

        /// <summary>
        /// 導航到機器頁面
        /// </summary>
        private void NavigateToMachinePage()
        {
            var machinePage = new MachinePage();
            ContentArea.Content = machinePage;
            UpdatePageTitle("3D Printer Control");
        }

        /// <summary>
        /// 更新頁面標題
        /// </summary>
        private void UpdatePageTitle(string title)
        {
            AppHeaderControl.PageTitle = title;
        }
    }
}
