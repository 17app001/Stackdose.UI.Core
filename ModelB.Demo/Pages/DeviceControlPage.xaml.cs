using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Controls;

namespace ModelB.Demo.Pages
{
    /// <summary>
    /// Interaction logic for DeviceControlPage.xaml
    /// </summary>
    public partial class DeviceControlPage : UserControl
    {
        #region Private Fields

        private string _currentBatchNumber = string.Empty;
        private ProcessState _currentProcessState = ProcessState.Idle;
        private bool _isDeviceInitialized = false;
        private System.Windows.Threading.DispatcherTimer? _initializationTimer;

        #endregion

        #region Constructor

        public DeviceControlPage()
        {
            InitializeComponent();
            this.Loaded += DeviceControlPage_Loaded;
        }

        #endregion

        #region Event Handlers

        private void DeviceControlPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize components if needed
            ComplianceContext.LogSystem("Device Control Page loaded", LogLevel.Info);
            
            // Debug: Check current user permissions
            var session = SecurityContext.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current User: {session.CurrentUserName}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current Level: {session.CurrentLevel}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] IsLoggedIn: {session.IsLoggedIn}");
            
            // Quick login as Admin if not logged in
            if (!session.IsLoggedIn || session.CurrentLevel < Stackdose.UI.Core.Models.AccessLevel.Operator)
            {
                System.Diagnostics.Debug.WriteLine("[DeviceControlPage] Auto login as Admin");
                SecurityContext.QuickLogin(Stackdose.UI.Core.Models.AccessLevel.Admin);
                
                // Force refresh button states
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[DeviceControlPage] Force refresh button states...");
                    RefreshButtonStates();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            
            // Update initial UI state - disable all buttons until device init
            DisableAllButtonsUntilInit();
            UpdateDeviceStatusDisplay();
        }

        /// <summary>
        /// 在設備初始化完成前禁用所有按鍵（除了 DeviceInit 按鈕）
        /// </summary>
        private void DisableAllButtonsUntilInit()
        {
            if (!_isDeviceInitialized)
            {
                // 只允許 DeviceInit 按鈕
                BtnDeviceInit.IsEnabled = true;
                BtnStartProcess.IsEnabled = false;
                BtnPauseProcess.IsEnabled = false;
                BtnCancelProcess.IsEnabled = false;
                BtnErrorReset.IsEnabled = false;
                
                ComplianceContext.LogSystem(
                    "設備尚未初始化，所有功能按鈕已禁用。請先執行 Device Init。",
                    LogLevel.Warning,
                    showInUi: true
                );
            }
        }

        /// <summary>
        /// Force refresh all SecuredButton authorization states
        /// </summary>
        private void RefreshButtonStates()
        {
            var buttons = new[] { BtnDeviceInit, BtnStartProcess, BtnPauseProcess, BtnCancelProcess, BtnErrorReset };
            
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    // Trigger UpdateAuthorization via reflection
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
            
            // 再次確保未初始化時禁用按鈕
            DisableAllButtonsUntilInit();
            
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Button states refreshed. Current User: {SecurityContext.CurrentSession.CurrentUserName}, Level: {SecurityContext.CurrentSession.CurrentLevel}");
        }

        /// <summary>
        /// 更新設備狀態顯示
        /// </summary>
        private void UpdateDeviceStatusDisplay()
        {
            if (DeviceStatusText == null) return;

            string statusText = "Unknown";
            var statusColor = System.Windows.Media.Brushes.Gray;

            if (!_isDeviceInitialized)
            {
                statusText = "Uninitialized";
                statusColor = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x75, 0x75, 0x75)); // Gray
            }
            else
            {
                switch (_currentProcessState)
                {
                    case ProcessState.Idle:
                        statusText = "Ready";
                        statusColor = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
                        break;
                    case ProcessState.Initializing:
                        statusText = "Initializing...";
                        statusColor = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0xFF, 0xB7, 0x4D)); // Amber
                        break;
                    case ProcessState.Running:
                        statusText = "Running";
                        statusColor = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x00, 0xBC, 0xD4)); // Cyan
                        break;
                    case ProcessState.Paused:
                        statusText = "Paused";
                        statusColor = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00)); // Orange
                        break;
                    case ProcessState.Stopped:
                        statusText = "Stopped";
                        statusColor = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36)); // Red
                        break;
                }
            }

            DeviceStatusText.Text = statusText;
            DeviceStatusText.Foreground = statusColor;
        }

        #endregion

        #region Button Event Handlers

        private void BtnDeviceInit_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // Disable all control buttons during initialization
            BtnDeviceInit.IsEnabled = false;
            BtnStartProcess.IsEnabled = false;
            BtnPauseProcess.IsEnabled = false;
            BtnCancelProcess.IsEnabled = false;
            BtnErrorReset.IsEnabled = false;
            
            // Set to Initializing state
            _currentProcessState = ProcessState.Initializing;
            ProcessStatus.ProcessState = ProcessState.Initializing;
            ProcessStatus.BatchNumber = "Device Init";
            UpdateDeviceStatusDisplay();
            
            // Log to OperationLogs table
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Device Init",
                category: "Device Control",
                beforeState: _isDeviceInitialized ? "Ready" : "Uninitialized",
                afterState: "Initializing",
                message: "設備初始化開始 - 5 秒倒數計時",
                batchId: "",
                showInUi: true
            );
            
            ComplianceContext.LogSystem(
                "設備初始化開始 - 請等待 5 秒...",
                LogLevel.Warning,
                showInUi: true
            );
            
            // Start 5-second initialization timer
            _initializationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _initializationTimer.Tick += InitializationTimer_Tick;
            _initializationTimer.Start();
            
            // TODO: Implement actual device initialization logic
            // Example: Write to PLC to start initialization sequence
            // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M100", true);
        }

        private void InitializationTimer_Tick(object? sender, EventArgs e)
        {
            // Stop timer
            _initializationTimer?.Stop();
            _initializationTimer = null;
            
            // Mark device as initialized
            _isDeviceInitialized = true;
            _currentProcessState = ProcessState.Idle;
            
            // Hide process indicator
            ProcessStatus.ProcessState = ProcessState.Idle;
            
            // Update status display
            UpdateDeviceStatusDisplay();
            
            // Enable buttons after initialization
            BtnDeviceInit.IsEnabled = true;
            BtnStartProcess.IsEnabled = true;
            BtnErrorReset.IsEnabled = true;
            // Pause and Cancel remain disabled until process starts
            BtnPauseProcess.IsEnabled = false;
            BtnCancelProcess.IsEnabled = false;
            
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // Log completion
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Device Init",
                category: "Device Control",
                beforeState: "Initializing",
                afterState: "Ready",
                message: "設備初始化成功完成",
                batchId: "",
                showInUi: true
            );
            
            ComplianceContext.LogSystem(
                "設備初始化完成 - 設備已就緒可以開始生產",
                LogLevel.Success,
                showInUi: true
            );
            
            // ?? 改用 CyberMessageBox
            CyberMessageBox.Show(
                "設備初始化完成！\n設備已就緒，可以開始生產。\n\nDevice initialization completed!\nDevice is now ready to start production.",
                "Device Init Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            // Check if device is initialized
            if (!_isDeviceInitialized)
            {
                // ?? 改用 CyberMessageBox
                CyberMessageBox.Show(
                    "設備尚未初始化！\n請先執行設備初始化。\n\nDevice is not initialized!\nPlease initialize the device first.",
                    "Device Not Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // Step 1: Show batch input dialog
            var dialog = new BatchInputDialog($"BATCH-{DateTime.Now:yyyyMMdd}-001");
            dialog.Owner = Window.GetWindow(this);
            
            if (dialog.ShowDialog() == true)
            {
                _currentBatchNumber = dialog.BatchNumber;
                
                // Step 2: Update process state to Running
                _currentProcessState = ProcessState.Running;
                ProcessStatus.ProcessState = ProcessState.Running;
                ProcessStatus.BatchNumber = _currentBatchNumber;
                UpdateDeviceStatusDisplay();
                
                // Step 3: Enable/Disable buttons
                BtnStartProcess.IsEnabled = false;
                BtnPauseProcess.IsEnabled = true;
                BtnCancelProcess.IsEnabled = true;
                
                // Step 4: Log to OperationLogs table
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Start Process",
                    category: "Process Control",
                    beforeState: "Ready",
                    afterState: "Running",
                    message: $"生產製程開始，批號：{_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                ComplianceContext.LogSystem(
                    $"生產開始 - 批號：{_currentBatchNumber}",
                    LogLevel.Success,
                    showInUi: true
                );
                
                // TODO: Implement start process logic
                // Example: Write to PLC to start production sequence
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M101", true);
                // PlcContext.GlobalStatus?.CurrentManager?.WriteWord("D103", batchIdNumeric);
            }
            else
            {
                ComplianceContext.LogSystem(
                    "生產開始已取消 - 使用者取消批號輸入",
                    LogLevel.Warning,
                    showInUi: true
                );
            }
        }

        private void BtnPauseProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            if (_currentProcessState == ProcessState.Running)
            {
                // Pause the process
                // ?? 改用 CyberMessageBox
                var result = CyberMessageBox.Show(
                    "確定要暫停當前製程嗎？\n\nAre you sure you want to pause the current process?",
                    "Pause Process",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _currentProcessState = ProcessState.Paused;
                    ProcessStatus.ProcessState = ProcessState.Paused;
                    UpdateDeviceStatusDisplay();
                    
                    // Update button text
                    BtnPauseProcess.Content = "Resume Process";
                    
                    // Log operation
                    ComplianceContext.LogOperation(
                        userId: username,
                        commandName: "Pause Process",
                        category: "Process Control",
                        beforeState: "Running",
                        afterState: "Paused",
                        message: $"製程已暫停 - 批號：{_currentBatchNumber}",
                        batchId: _currentBatchNumber,
                        showInUi: true
                    );
                    
                    // ?? 改用 CyberMessageBox
                    CyberMessageBox.Show("製程已暫停\n\nProcess paused",
                        "Pause Process",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    // TODO: Implement pause logic
                    // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M102", true);
                }
            }
            else if (_currentProcessState == ProcessState.Paused)
            {
                // Resume the process
                _currentProcessState = ProcessState.Running;
                ProcessStatus.ProcessState = ProcessState.Running;
                UpdateDeviceStatusDisplay();
                
                // Update button text
                BtnPauseProcess.Content = "Pause Process";
                
                // Log operation
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Resume Process",
                    category: "Process Control",
                    beforeState: "Paused",
                    afterState: "Running",
                    message: $"製程已恢復 - 批號：{_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                // ?? 改用 CyberMessageBox
                CyberMessageBox.Show("製程已恢復\n\nProcess resumed",
                    "Resume Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // TODO: Implement resume logic
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M102", false);
            }
        }

        private void BtnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // ?? 改用 CyberMessageBox
            var result = CyberMessageBox.Show(
                "確定要取消當前製程嗎？\n此操作無法復原！\n\nAre you sure you want to cancel the current process?\nThis action cannot be undone!",
                "Cancel Process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Stop the process
                _currentProcessState = ProcessState.Stopped;
                ProcessStatus.ProcessState = ProcessState.Stopped;
                UpdateDeviceStatusDisplay();
                
                // Reset buttons
                BtnStartProcess.IsEnabled = true;
                BtnPauseProcess.IsEnabled = false;
                BtnPauseProcess.Content = "Pause Process";
                BtnCancelProcess.IsEnabled = false;
                
                // Log operation
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Cancel Process",
                    category: "Process Control",
                    beforeState: _currentProcessState == ProcessState.Paused ? "Paused" : "Running",
                    afterState: "Cancelled",
                    message: $"製程已取消（無法復原） - 批號：{_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                ComplianceContext.LogSystem(
                    $"生產已取消 - 批號：{_currentBatchNumber}",
                    LogLevel.Error,
                    showInUi: true
                );
                
                // Clear batch number
                _currentBatchNumber = string.Empty;
                
                // Delay status update to Idle
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    _currentProcessState = ProcessState.Idle;
                    UpdateDeviceStatusDisplay();
                };
                timer.Start();
                
                // ?? 改用 CyberMessageBox
                CyberMessageBox.Show("製程已取消\n\nProcess cancelled",
                    "Cancel Process",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // TODO: Implement cancel logic
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M103", true);
            }
        }

        private void BtnErrorReset_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // ?? 改用 CyberMessageBox
            var result = CyberMessageBox.Show(
                "確定要重置所有錯誤狀態嗎？\n\nAre you sure you want to reset all error states?",
                "Error Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Log operation
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Error Reset",
                    category: "Error Handling",
                    beforeState: "Error State",
                    afterState: "Normal",
                    message: "所有錯誤狀態已重置",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                // ?? 改用 CyberMessageBox
                CyberMessageBox.Show("所有錯誤狀態已重置\n\nAll error states reset",
                    "Error Reset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // TODO: Implement error reset logic
                // PlcContext.GlobalStatus?.CurrentManager?.WriteBit("M104", true);
            }
        }

        #endregion
    }
}
