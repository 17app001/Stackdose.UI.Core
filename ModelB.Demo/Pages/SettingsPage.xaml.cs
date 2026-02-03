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
            
            // ?? 訂閱 PlcText 控件的 ValueApplied 事件（可選）
            PreDryTimePlcText.ValueApplied += PlcText_ValueApplied;
            Zone1TimePlcText.ValueApplied += PlcText_ValueApplied;
            Zone2TimePlcText.ValueApplied += PlcText_ValueApplied;
            CdaFlowPlcText.ValueApplied += PlcText_ValueApplied;
            CdaVolumePlcText.ValueApplied += PlcText_ValueApplied;
            SwingSpeedPlcText.ValueApplied += PlcText_ValueApplied;
            BatchNoPlcText.ValueApplied += PlcText_ValueApplied;
        }

        /// <summary>
        /// ?? 處理 PlcText 控件的 ValueApplied 事件（可選的額外處理）
        /// </summary>
        private void PlcText_ValueApplied(object? sender, ValueAppliedEventArgs e)
        {
            // 這裡可以做額外的處理，例如更新 UI 或記錄特殊日誌
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] {e.Address} = {e.Value}, WriteSuccess = {e.WriteSuccess}");
            #endif
        }

        private void SavePlcSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement PLC settings save logic
            ComplianceContext.LogSystem("PLC settings saved", LogLevel.Success);
            // 改用 CyberMessageBox
            CyberMessageBox.Show("PLC settings saved successfully!\nPLC 設定已成功儲存！", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement preferences save logic
            ComplianceContext.LogSystem("System preferences saved", LogLevel.Success);
            // 改用 CyberMessageBox
            CyberMessageBox.Show("System preferences saved successfully!\n系統偏好設定已成功儲存！", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
