using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Templates.Controls;

namespace WpfApp.Demo.Views
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
            
            // 預設顯示首頁
            NavigateToHome();
        }

        /// <summary>
        /// 處理 Header 登出按鈕點擊事件
        /// </summary>
        private void OnLogout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Logout clicked!", "Demo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                "Are you sure you want to close the application?",
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
                case "HomePage":
                    NavigateToHome();
                    break;
                case "MachinePage":
                    NavigateToMachine();
                    break;
                case "LogViewerPage":
                    NavigateToLogViewer();
                    break;
                case "UserManagementPage":
                    NavigateToUserManagement();
                    break;
                case "SettingsPage":
                    NavigateToSettings();
                    break;
                default:
                    NavigateToHome();
                    break;
            }
        }

        /// <summary>
        /// 導航到首頁
        /// </summary>
        private void NavigateToHome()
        {
            var homePage = new HomePage();
            ContentArea.Content = homePage;
            UpdatePageTitle("Home Overview");
        }

        /// <summary>
        /// 導航到機器頁面
        /// </summary>
        private void NavigateToMachine()
        {
            var machinePage = new MachinePage();
            ContentArea.Content = machinePage;
            UpdatePageTitle("3D Printer Control");
        }

        /// <summary>
        /// 導航到日誌檢視頁面
        /// </summary>
        private void NavigateToLogViewer()
        {
            var logPage = new LogViewerPage();
            ContentArea.Content = logPage;
            UpdatePageTitle("Log Viewer");
        }

        /// <summary>
        /// 導航到使用者管理頁面
        /// </summary>
        private void NavigateToUserManagement()
        {
            var userPage = new UserManagementPage();
            ContentArea.Content = userPage;
            UpdatePageTitle("User Management");
        }

        /// <summary>
        /// 導航到設定頁面
        /// </summary>
        private void NavigateToSettings()
        {
            var settingsPage = new SettingsPage();
            ContentArea.Content = settingsPage;
            UpdatePageTitle("System Settings");
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
