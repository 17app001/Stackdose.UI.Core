using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Views
{
    /// <summary>
    /// MainPanelPage.xaml 的互動邏輯
    /// 主控制面板頁面，顯示系統狀態和控制按鈕
    /// </summary>
    public partial class MainPanelPage : UserControl
    {
        public MainPanelPage()
        {
            InitializeComponent();
        }

        private void StartProcess_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Process started!", "UBI System", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
