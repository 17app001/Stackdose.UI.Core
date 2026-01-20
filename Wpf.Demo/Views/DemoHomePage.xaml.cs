using System.Windows;
using Stackdose.UI.Templates.Controls;
using Stackdose.UI.Templates.Pages;

namespace Wpf.Demo.Views
{
    /// <summary>
    /// DemoHomePage.xaml 的互動邏輯
    /// 展示如何使用 BasePage 創建完整的頁面
    /// </summary>
    public partial class DemoHomePage : BasePage
    {
        public DemoHomePage()
        {
            InitializeComponent();
            
            // 設定頁面標題
            PageTitle = "Demo Home Page";
        }

        /// <summary>
        /// 處理登出按鈕點擊事件
        /// </summary>
        private void OnLogout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Logout functionality triggered!\n\nThis is where you would:\n" +
                "1. Clear user session\n" +
                "2. Navigate to login page\n" +
                "3. Clean up resources",
                "Logout",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 處理導航項目點擊事件
        /// </summary>
        private void OnNavigate(object sender, NavigationItem e)
        {
            MessageBox.Show(
                $"Navigation requested!\n\n" +
                $"Title: {e.Title}\n" +
                $"Target: {e.NavigationTarget}\n\n" +
                $"This is where you would navigate to:\n{e.NavigationTarget}",
                "Navigation",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 示範按鈕點擊事件
        /// </summary>
        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Demo button clicked!\n\n" +
                "This demonstrates how to handle events\n" +
                "in your custom content area.",
                "Demo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
