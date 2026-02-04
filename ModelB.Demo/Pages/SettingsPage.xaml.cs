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

        /// <summary>
        /// Return to Desktop button click handler - Logout and close application
        /// </summary>
        private void ReturnToDesktop_Click(object sender, RoutedEventArgs e)
        {
            // 顯示確認對話框
            var result = CyberMessageBox.Show(
                "確定要登出並關閉程式嗎？\n\n這將會登出目前使用者並關閉應用程式，讓您可以回到 Windows 桌面。",
                "確認登出並關閉程式",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // 記錄操作
                ComplianceContext.LogSystem(
                    $"[SETTINGS] 使用者 '{SecurityContext.CurrentSession.CurrentUserName}' 登出並關閉程式",
                    LogLevel.Warning,
                    showInUi: true
                );

                // ?? 關鍵修改：先刷新日誌，然後直接關閉程式，不要呼叫 Logout()
                // 這樣就不會觸發 LogoutOccurred 事件，也就不會顯示登入視窗
                
                // 記錄到 Audit Trail
                var user = SecurityContext.CurrentSession.CurrentUser;
                if (user != null)
                {
                    ComplianceContext.LogAuditTrail(
                        "User Logout (Exit to Desktop)",
                        user.UserId,
                        $"Logged In (Level {(int)user.AccessLevel} - {user.AccessLevel})",
                        "Logged Out",
                        "Exit to Desktop from Settings",
                        showInUi: true
                    );
                }

                // 刷新所有待寫入的日誌
                ComplianceContext.FlushLogs();

                // ?? 直接關閉應用程式，不呼叫 SecurityContext.Logout()
                Application.Current.Shutdown();
            }
            catch (System.Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[SETTINGS] 登出並關閉程式時發生錯誤: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
                
                CyberMessageBox.Show(
                    $"執行時發生錯誤:\n\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
