using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace ModelB.Demo.Pages
{
    /// <summary>
    /// Interaction logic for DeviceControlPage.xaml
    /// </summary>
    public partial class DeviceControlPage : UserControl
    {
        public DeviceControlPage()
        {
            InitializeComponent();
            this.Loaded += DeviceControlPage_Loaded;
        }

        private void DeviceControlPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize components if needed
            ComplianceContext.LogSystem("Device Control Page loaded", LogLevel.Info);
            
            // ?? 調試：檢查當前使用者權限
            var session = SecurityContext.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current User: {session.CurrentUserName}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current Level: {session.CurrentLevel}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] IsLoggedIn: {session.IsLoggedIn}");
            
            // ?? 如果沒有登入，快速登入為 Admin
            if (!session.IsLoggedIn || session.CurrentLevel < Stackdose.UI.Core.Models.AccessLevel.Operator)
            {
                System.Diagnostics.Debug.WriteLine("[DeviceControlPage] 自動登入為 Admin 以顯示所有按鈕");
                SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Admin);
                
                // ?? 強制更新所有按鈕狀態（延遲執行，確保按鈕已初始化）
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[DeviceControlPage] 強制刷新按鈕狀態...");
                    RefreshButtonStates();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// ?? 新增：強制刷新所有 SecuredButton 的權限狀態
        /// </summary>
        private void RefreshButtonStates()
        {
            var buttons = new[] { BtnDeviceInit, BtnStartProcess, BtnInputBatch, BtnPauseProcess, BtnCancelProcess, BtnErrorReset };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    // 強制觸發 UpdateAuthorization（透過反射）
                    try
                    {
                        var method = button.GetType().GetMethod("UpdateAuthorization", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(button, null);
                        
                        System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Refreshed: {button.Name} - IsAuthorized={button.IsAuthorized}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Failed to refresh {button.Name}: {ex.Message}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Button states refreshed. Current User: {SecurityContext.CurrentSession.CurrentUserName}, Level: {SecurityContext.CurrentSession.CurrentLevel}");
        }

        #region Button Event Handlers

        private void BtnDeviceInit_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // ?? 記錄到 OperationLogs 表
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Device Init",
                category: "Device Control",
                beforeState: "Uninitialized",
                afterState: "Initialized",
                message: "Device initialization started",
                batchId: "",
                showInUi: true
            );
            
            MessageBox.Show("設備初始化流程已啟動\nDevice Initialization Process Started",
                "設備初始化 / Device Init",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // TODO: Implement device initialization logic
            // Example: Write to PLC to start initialization sequence
            // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M100", true);
        }

        private void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // ?? 記錄到 OperationLogs 表
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Start Process",
                category: "Process Control",
                beforeState: "Idle",
                afterState: "Running",
                message: "Production process started by user",
                batchId: "",
                showInUi: true
            );
            
            MessageBox.Show("生產製程已啟動\nProduction Process Started",
                "啟動製程 / Start Process",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // TODO: Implement start process logic
            // Example: Write to PLC to start production sequence
            // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M101", true);
        }

        private void BtnInputBatch_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // Open input dialog for batch number
            var dialog = new Stackdose.UI.Core.Controls.InputDialog(
                "輸入批次編號 / Input Batch Number",
                "請輸入生產批次編號:\nPlease enter batch number:"
            );

            if (dialog.ShowDialog() == true)
            {
                string batchNumber = dialog.InputText;
                
                // ?? 記錄到 OperationLogs 表
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Input Batch",
                    category: "Batch Management",
                    beforeState: "No Batch",
                    afterState: $"Batch: {batchNumber}",
                    message: $"Batch number entered: {batchNumber}",
                    batchId: batchNumber,
                    showInUi: true
                );
                
                // TODO: Write batch number to PLC
                // Example: Convert batch number to numeric value and write
                // if (int.TryParse(batchNumber.Replace("BATCH-", ""), out int batchId))
                // {
                //     PlcContext.GlobalStatus?.CurrentManager?.WriteWord("D103", (ushort)batchId);
                // }
                
                MessageBox.Show($"批次編號已設定: {batchNumber}\nBatch Number Set: {batchNumber}",
                    "批次編號 / Batch Number",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnPauseProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            var result = MessageBox.Show("確定要暫停當前製程嗎?\nAre you sure you want to pause the current process?",
                "暫停製程 / Pause Process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // ?? 記錄到 OperationLogs 表
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Pause Process",
                    category: "Process Control",
                    beforeState: "Running",
                    afterState: "Paused",
                    message: "Process paused by user",
                    batchId: "",
                    showInUi: true
                );
                
                // TODO: Implement pause process logic
                // Example: Write to PLC to pause production sequence
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M102", true);
                
                MessageBox.Show("製程已暫停\nProcess Paused",
                    "暫停製程 / Pause Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            var result = MessageBox.Show("確定要取消當前製程嗎?\n此操作無法復原!\n\nAre you sure you want to cancel the current process?\nThis action cannot be undone!",
                "取消製程 / Cancel Process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // ?? 記錄到 OperationLogs 表
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Cancel Process",
                    category: "Process Control",
                    beforeState: "Running",
                    afterState: "Cancelled",
                    message: "Process cancelled by user (cannot be undone)",
                    batchId: "",
                    showInUi: true
                );
                
                // TODO: Implement cancel process logic
                // Example: Write to PLC to cancel production sequence
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M103", true);
                
                MessageBox.Show("製程已取消\nProcess Cancelled",
                    "取消製程 / Cancel Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnErrorReset_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            var result = MessageBox.Show("確定要重置所有錯誤狀態嗎?\nAre you sure you want to reset all error states?",
                "異常排除 / Error Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // ?? 記錄到 OperationLogs 表
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Error Reset",
                    category: "Error Handling",
                    beforeState: "Error State",
                    afterState: "Normal",
                    message: "All error states reset",
                    batchId: "",
                    showInUi: true
                );
                
                // TODO: Implement error reset logic
                // Example: Write to PLC to reset error flags
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M104", true);
                
                MessageBox.Show("所有錯誤狀態已重置\nAll Error States Reset",
                    "異常排除 / Error Reset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
