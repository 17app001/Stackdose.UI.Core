using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Controls;

namespace ModelB.Demo.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void SavePlcSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement PLC settings save logic
            ComplianceContext.LogSystem("PLC settings saved", LogLevel.Success);
            // ?? 改用 CyberMessageBox
            CyberMessageBox.Show("PLC settings saved successfully!\nPLC 設定已成功儲存！", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement preferences save logic
            ComplianceContext.LogSystem("System preferences saved", LogLevel.Success);
            // ?? 改用 CyberMessageBox
            CyberMessageBox.Show("System preferences saved successfully!\n系統偏好設定已成功儲存！", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
