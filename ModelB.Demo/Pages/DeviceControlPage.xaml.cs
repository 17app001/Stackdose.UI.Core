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
        private System.Windows.Threading.DispatcherTimer? _initializationTimer;

        #endregion

        #region Constructor

        public DeviceControlPage()
        {
            InitializeComponent();
            this.Loaded += DeviceControlPage_Loaded;
            this.Unloaded += DeviceControlPage_Unloaded;
        }

        #endregion

        #region Event Handlers

        private void DeviceControlPage_Loaded(object sender, RoutedEventArgs e)
        {
            // ?? 只在第一次載入時記錄日誌（避免頁面切換時重複記錄）
            // ComplianceContext.LogSystem("Device Control Page loaded", LogLevel.Info);
            
            // ?? 從 ProcessContext 同步狀態（使用全局的 IsDeviceInitialized）
            _currentProcessState = ProcessContext.CurrentState;
            _currentBatchNumber = !string.IsNullOrEmpty(ProcessContext.BatchId) 
                ? ProcessContext.BatchId 
                : (ProcessContext.BatchNumber > 0 ? $"BATCH-{ProcessContext.BatchNumber}" : string.Empty);
            
            // Sync UI
            ProcessStatus.ProcessState = _currentProcessState;
            if (!string.IsNullOrEmpty(_currentBatchNumber))
            {
                ProcessStatus.BatchNumber = _currentBatchNumber;
            }
            UpdateDeviceStatusDisplay();
            UpdateButtonStates();
            
            // Subscribe to ProcessContext
            ProcessContext.StateChanged += OnProcessStateChanged;
            ProcessContext.DeviceInitializedChanged += OnDeviceInitializedChanged;
            
            // Debug: Check current user permissions
            var session = SecurityContext.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current User: {session.CurrentUserName}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] Current Level: {session.CurrentLevel}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] IsLoggedIn: {session.IsLoggedIn}");
            System.Diagnostics.Debug.WriteLine($"[DeviceControlPage] IsDeviceInitialized: {ProcessContext.IsDeviceInitialized}");
            
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

        private void DeviceControlPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // ?? 取消訂閱事件
            ProcessContext.StateChanged -= OnProcessStateChanged;
            ProcessContext.DeviceInitializedChanged -= OnDeviceInitializedChanged;
        }

        private void OnProcessStateChanged(ProcessState state)
        {
            Dispatcher.Invoke(() =>
            {
                _currentProcessState = state;
                ProcessStatus.ProcessState = state;
                UpdateDeviceStatusDisplay();
                UpdateButtonStates();
            });
        }

        /// <summary>
        /// ?? 設備初始化狀態改變時的處理
        /// </summary>
        private void OnDeviceInitializedChanged(bool isInitialized)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDeviceStatusDisplay();
                UpdateButtonStates();
            });
        }

        /// <summary>
        /// 在設備初始化完成前禁用所有按鍵（除了 DeviceInit 按鈕）
        /// </summary>
        private void DisableAllButtonsUntilInit()
        {
            if (!ProcessContext.IsDeviceInitialized)
            {
                // 只允許 DeviceInit 按鈕
                BtnDeviceInit.IsEnabled = true;
                BtnStartProcess.IsEnabled = false;
                BtnPauseProcess.IsEnabled = false;
                BtnCancelProcess.IsEnabled = false;
                BtnErrorReset.IsEnabled = false;
                
                // ?? 移除重複的警告訊息，只在 Debug 輸出
                System.Diagnostics.Debug.WriteLine("[DeviceControlPage] Device not initialized, buttons disabled");
            }
        }

        /// <summary>
        /// Update button enable/disable states based on current process state
        /// </summary>
        private void UpdateButtonStates()
        {
            if (!ProcessContext.IsDeviceInitialized)
            {
                BtnDeviceInit.IsEnabled = true;
                BtnStartProcess.IsEnabled = false;
                BtnPauseProcess.IsEnabled = false;
                BtnCancelProcess.IsEnabled = false;
                BtnErrorReset.IsEnabled = false;
            }
            else
            {
                BtnDeviceInit.IsEnabled = true;
                BtnErrorReset.IsEnabled = true;
                
                switch (_currentProcessState)
                {
                    case ProcessState.Idle:
                        BtnStartProcess.IsEnabled = true;
                        BtnPauseProcess.IsEnabled = false;
                        BtnCancelProcess.IsEnabled = false;
                        BtnPauseProcess.Content = "Pause Process";
                        break;
                        
                    case ProcessState.Running:
                        BtnStartProcess.IsEnabled = false;
                        BtnPauseProcess.IsEnabled = true;
                        BtnCancelProcess.IsEnabled = true;
                        BtnPauseProcess.Content = "Pause Process";
                        break;
                        
                    case ProcessState.Paused:
                        BtnStartProcess.IsEnabled = false;
                        BtnPauseProcess.IsEnabled = true;
                        BtnCancelProcess.IsEnabled = true;
                        BtnPauseProcess.Content = "Resume Process";
                        break;
                        
                    case ProcessState.Stopped:
                        BtnStartProcess.IsEnabled = true;
                        BtnPauseProcess.IsEnabled = false;
                        BtnCancelProcess.IsEnabled = false;
                        BtnPauseProcess.Content = "Pause Process";
                        break;
                }
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

            if (!ProcessContext.IsDeviceInitialized)
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
            
            // Set to Initializing state (update ProcessContext)
            ProcessContext.CurrentState = ProcessState.Initializing;
            ProcessStatus.BatchNumber = "Device Init";
            UpdateDeviceStatusDisplay();
            
            // Log to OperationLogs table
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Device Init",
                category: "Device Control",
                beforeState: ProcessContext.IsDeviceInitialized ? "Ready" : "Uninitialized",
                afterState: "Initializing",
                message: "Device initialization started - 5 second countdown",
                batchId: "",
                showInUi: true
            );
            
            ComplianceContext.LogSystem(
                "Device initialization started - Please wait 5 seconds...",
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
        }

        private void InitializationTimer_Tick(object? sender, EventArgs e)
        {
            // Stop timer
            _initializationTimer?.Stop();
            _initializationTimer = null;
            
            // ?? 使用全局 ProcessContext.IsDeviceInitialized
            ProcessContext.IsDeviceInitialized = true;
            
            // Update ProcessContext
            ProcessContext.CurrentState = ProcessState.Idle;
            
            // Update status display
            UpdateDeviceStatusDisplay();
            UpdateButtonStates();
            
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            // Log completion
            ComplianceContext.LogOperation(
                userId: username,
                commandName: "Device Init",
                category: "Device Control",
                beforeState: "Initializing",
                afterState: "Ready",
                message: "Device initialization completed successfully",
                batchId: "",
                showInUi: true
            );
            
            ComplianceContext.LogSystem(
                "Device initialization complete - Device is now ready to start production",
                LogLevel.Success,
                showInUi: true
            );
            
            CyberMessageBox.Show(
                "Device initialization completed!\nDevice is now ready, you can start production.",
                "Device Init Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            // Check if device is initialized
            if (!ProcessContext.IsDeviceInitialized)
            {
                CyberMessageBox.Show(
                    "Device is not initialized!\nPlease initialize the device first.",
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
                
                // ?? 更新 ProcessContext.BatchId（完整批號字串）
                ProcessContext.BatchId = _currentBatchNumber;
                
                // Parse batch number to int for ProcessContext.BatchNumber
                if (int.TryParse(_currentBatchNumber.Replace("BATCH-", "").Replace("-", ""), out int batchNo))
                {
                    ProcessContext.BatchNumber = batchNo;
                }
                
                // Step 2: Update ProcessContext
                ProcessContext.CurrentState = ProcessState.Running;
                ProcessStatus.BatchNumber = _currentBatchNumber;
                UpdateDeviceStatusDisplay();
                UpdateButtonStates();
                
                // Step 3: Log to OperationLogs table
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Start Process",
                    category: "Process Control",
                    beforeState: "Ready",
                    afterState: "Running",
                    message: $"Production process started, Batch: {_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                ComplianceContext.LogSystem(
                    $"Production started - Batch: {_currentBatchNumber}",
                    LogLevel.Success,
                    showInUi: true
                );
            }
            else
            {
                ComplianceContext.LogSystem(
                    "Production start cancelled - User cancelled batch input",
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
                var result = CyberMessageBox.Show(
                    "Are you sure you want to pause the current process?",
                    "Pause Process",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Update ProcessContext
                    ProcessContext.CurrentState = ProcessState.Paused;
                    UpdateDeviceStatusDisplay();
                    UpdateButtonStates();
                    
                    // Log operation
                    ComplianceContext.LogOperation(
                        userId: username,
                        commandName: "Pause Process",
                        category: "Process Control",
                        beforeState: "Running",
                        afterState: "Paused",
                        message: $"Process paused - Batch: {_currentBatchNumber}",
                        batchId: _currentBatchNumber,
                        showInUi: true
                    );
                    
                    CyberMessageBox.Show("Process paused", "Pause Process",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentProcessState == ProcessState.Paused)
            {
                // Update ProcessContext
                ProcessContext.CurrentState = ProcessState.Running;
                UpdateDeviceStatusDisplay();
                UpdateButtonStates();
                
                // Log operation
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Resume Process",
                    category: "Process Control",
                    beforeState: "Paused",
                    afterState: "Running",
                    message: $"Process resumed - Batch: {_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                CyberMessageBox.Show("Process resumed", "Resume Process",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancelProcess_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
            var result = CyberMessageBox.Show(
                "Are you sure you want to cancel the current process?\nThis action cannot be undone!",
                "Cancel Process",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Update ProcessContext
                ProcessContext.CurrentState = ProcessState.Stopped;
                UpdateDeviceStatusDisplay();
                UpdateButtonStates();
                
                // Log operation
                ComplianceContext.LogOperation(
                    userId: username,
                    commandName: "Cancel Process",
                    category: "Process Control",
                    beforeState: _currentProcessState == ProcessState.Paused ? "Paused" : "Running",
                    afterState: "Cancelled",
                    message: $"Process cancelled (cannot be undone) - Batch: {_currentBatchNumber}",
                    batchId: _currentBatchNumber,
                    showInUi: true
                );
                
                ComplianceContext.LogSystem(
                    $"Production cancelled - Batch: {_currentBatchNumber}",
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
                    ProcessContext.CurrentState = ProcessState.Idle;
                };
                timer.Start();
                
                CyberMessageBox.Show("Process cancelled", "Cancel Process",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnErrorReset_Click(object sender, RoutedEventArgs e)
        {
            string username = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
            
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
                
                CyberMessageBox.Show("所有錯誤狀態已重置\n\nAll error states reset",
                    "Error Reset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
