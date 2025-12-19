using System.Windows;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Controls;

namespace Wpf.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 🔑 一行程式切換權限（改這裡即可）
            SecurityContext.QuickLogin(AccessLevel.Engineer);  // Guest / Operator / Instructor / Supervisor / Engineer
        }

        // 🔥 測試：手動觸發 PlcLabel 主題更新
        private void TestThemeUpdate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("===== 手動觸發 PlcLabel 主題更新 =====");
            PlcLabelContext.NotifyThemeChanged();
            
            // 檢查資源
            var plcBg = Application.Current.TryFindResource("Plc.Bg.Main") as System.Windows.Media.SolidColorBrush;
            System.Diagnostics.Debug.WriteLine($"Plc.Bg.Main = {plcBg?.Color}");
            
            CyberMessageBox.Show(
                $"已觸發 PlcLabel 主題更新\nPlc.Bg.Main = {plcBg?.Color}",
                "測試",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}