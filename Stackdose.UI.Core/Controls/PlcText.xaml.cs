using System;
using System.Windows;
using System.Windows.Controls;
using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PlcText - 可編輯的 PLC 參數控件
    /// 顯示 Label + TextBox + Apply 按鈕
    /// </summary>
    /// <remarks>
    /// ✅ 已優化（使用 PlcContext）
    /// - 移除手動 PlcManager 管理
    /// - 統一使用 PlcContext.GlobalStatus
    /// - 簡化代碼結構
    /// 
    /// <para>功能完整性：</para>
    /// <list type="bullet">
    /// <item>✅ PLC 讀取/寫入</item>
    /// <item>✅ Audit Trail 記錄</item>
    /// <item>✅ Compliance Context 整合</item>
    /// <item>✅ 自動連線管理</item>
    /// </list>
    /// </remarks>
    public partial class PlcText : UserControl
    {
        #region Private Fields

        /// <summary>追蹤訂閱的 PlcStatus 實例</summary>
        private PlcStatus? _subscribedStatus;
        
        /// <summary>記錄舊值，用於 Audit Trail</summary>
        private string _previousValue = "0";

        #endregion

        #region Constructor

        public PlcText()
        {
            InitializeComponent();
            Loaded += PlcText_Loaded;
            Unloaded += PlcText_Unloaded;
        }

        #endregion

        #region Dependency Properties

        /// <summary>Label 文字</summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(PlcText),
                new PropertyMetadata("Parameter"));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>PLC Address (例如: "D100")</summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(
                nameof(Address),
                typeof(string),
                typeof(PlcText),
                new PropertyMetadata(string.Empty, OnAddressChanged));

        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        /// <summary>當前值</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(PlcText),
                new PropertyMetadata("0", null, CoerceValue));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>是否顯示成功訊息（預設 true）</summary>
        public static readonly DependencyProperty ShowSuccessMessageProperty =
            DependencyProperty.Register(
                nameof(ShowSuccessMessage),
                typeof(bool),
                typeof(PlcText),
                new PropertyMetadata(true));

        public bool ShowSuccessMessage
        {
            get => (bool)GetValue(ShowSuccessMessageProperty);
            set => SetValue(ShowSuccessMessageProperty, value);
        }

        /// <summary>是否啟用 Audit Trail 記錄（預設 true）</summary>
        public static readonly DependencyProperty EnableAuditTrailProperty =
            DependencyProperty.Register(
                nameof(EnableAuditTrail),
                typeof(bool),
                typeof(PlcText),
                new PropertyMetadata(true));

        public bool EnableAuditTrail
        {
            get => (bool)GetValue(EnableAuditTrailProperty);
            set => SetValue(EnableAuditTrailProperty, value);
        }

        /// <summary>ValueApplied 事件 - 當使用者按下 Apply 按鈕時觸發</summary>
        public event EventHandler<ValueAppliedEventArgs>? ValueApplied;

        #endregion

        #region Lifecycle

        private void PlcText_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PlcText] Loaded: {Label} ({Address})");
            
            SubscribeToGlobalStatus();
            
            if (!string.IsNullOrEmpty(Address))
            {
                ReadFromPlc();
            }
        }

        private void PlcText_Unloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PlcText] Unloaded: {Label} ({Address})");
            UnsubscribeFromStatus();
        }

        #endregion

        #region PLC Management

        private void SubscribeToGlobalStatus()
        {
            UnsubscribeFromStatus();
            
            var globalStatus = PlcContext.GlobalStatus;
            if (globalStatus != null)
            {
                _subscribedStatus = globalStatus;
                _subscribedStatus.ConnectionEstablished += OnPlcConnectionEstablished;
                _subscribedStatus.ScanUpdated += OnScanUpdated;
                
                System.Diagnostics.Debug.WriteLine($"[PlcText] Subscribed to PlcStatus: {Label} ({Address}), IsConnected={globalStatus.CurrentManager?.IsConnected}");
            }
        }

        private void UnsubscribeFromStatus()
        {
            if (_subscribedStatus != null)
            {
                _subscribedStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
                _subscribedStatus.ScanUpdated -= OnScanUpdated;
                _subscribedStatus = null;
            }
        }

        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            SafeInvoke(() =>
            {
                if (!string.IsNullOrEmpty(Address))
                {
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Connection established, reading value: {Label} ({Address})");
                    ReadFromPlc();
                }
            });
        }

        private void OnScanUpdated(IPlcManager manager)
        {
            // PlcText 通常不需要每次掃描都更新
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 安全執行 UI 操作（自動切換到 UI 執行緒）
        /// </summary>
        private void SafeInvoke(Action action)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;
                
                if (Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlcText] SafeInvoke error: {ex.Message}");
            }
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            return baseValue ?? "0";
        }

        private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcText plcText && !string.IsNullOrEmpty(e.NewValue?.ToString()))
            {
                plcText.ReadFromPlc();
            }
        }

        /// <summary>
        /// 從 PLC 讀取數據
        /// </summary>
        private void ReadFromPlc()
        {
            SafeInvoke(() =>
            {
                var manager = PlcContext.GlobalStatus?.CurrentManager;
                
                System.Diagnostics.Debug.WriteLine($"[PlcText] ReadFromPlc: {Label} ({Address}) - Manager={manager != null}, IsConnected={manager?.IsConnected}");
                
                if (manager == null || !manager.IsConnected || string.IsNullOrEmpty(Address))
                {
                    return;
                }

                short? readValue = manager.ReadWord(Address);
                if (readValue.HasValue)
                {
                    Value = readValue.Value.ToString();
                    _previousValue = Value;
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Read success: {Label} ({Address}) = {Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Read returned null: {Label} ({Address})");
                }
            });
        }

        #endregion

        #region Apply Button Handler

        /// <summary>
        /// Apply Button Click Handler
        /// </summary>
        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 驗證輸入
                if (!int.TryParse(Value, out int intValue))
                {
                    CyberMessageBox.Show(
                        $"Invalid value for {Label}. Please enter a valid number.",
                        "Invalid Input",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 2. 記錄舊值（用於 Audit Trail）
                string oldValue = _previousValue;
                string newValue = intValue.ToString();

                // 3. 寫入 PLC
                bool writeSuccess = false;
                var manager = PlcContext.GlobalStatus?.CurrentManager;
                
                System.Diagnostics.Debug.WriteLine($"[PlcText] ApplyButton_Click: {Label} ({Address}) = {intValue}, Manager={manager != null}, IsConnected={manager?.IsConnected}");
                
                if (manager != null && manager.IsConnected && !string.IsNullOrEmpty(Address))
                {
                    string writeCommand = $"{Address},{intValue}";
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Write command: {writeCommand}");
                    
                    writeSuccess = await manager.WriteAsync(writeCommand);
                    
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Write result: {writeSuccess}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PlcText] Cannot write - PLC not connected");
                }

                // 4. 觸發事件
                var args = new ValueAppliedEventArgs(Address, intValue, writeSuccess);
                ValueApplied?.Invoke(this, args);

                // 5. 記錄日誌和顯示訊息
                if (writeSuccess)
                {
                    HandleWriteSuccess(oldValue, newValue, intValue);
                }
                else
                {
                    HandleWriteFailure(oldValue, newValue);
                }
            }
            catch (Exception ex)
            {
                HandleWriteException(ex);
            }
        }

        /// <summary>
        /// 處理寫入成功
        /// </summary>
        private void HandleWriteSuccess(string oldValue, string newValue, int intValue)
        {
            // 更新 _previousValue
            _previousValue = newValue;

            // 記錄 System Log
            ComplianceContext.LogSystem(
                $"[PlcText] {Label} ({Address}) set to {intValue}",
                LogLevel.Success,
                showInUi: true);

            // 記錄 Audit Trail（FDA 21 CFR Part 11 合規）
            if (EnableAuditTrail)
            {
                string userId = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
                string batchId = ProcessContext.BatchNumber > 0 ? ProcessContext.BatchNumber.ToString() : "";

                ComplianceContext.LogAuditTrail(
                    deviceName: Label,
                    address: Address,
                    oldValue: oldValue,
                    newValue: newValue,
                    reason: "Parameter Change",
                    parameter: $"User: {userId}",
                    batchId: batchId,
                    showInUi: true
                );

                // 立即刷新，確保 Audit Trail 寫入資料庫
                ComplianceContext.FlushLogs();
            }

            // 顯示成功訊息
            if (ShowSuccessMessage)
            {
                CyberMessageBox.Show(
                    $"{Label} successfully updated to {intValue}",
                    "Write Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 處理寫入失敗
        /// </summary>
        private void HandleWriteFailure(string oldValue, string newValue)
        {
            // 記錄失敗
            ComplianceContext.LogSystem(
                $"[PlcText] Failed to write {Label} ({Address}) - PLC not connected or write failed",
                LogLevel.Warning,
                showInUi: true);

            // 記錄 Audit Trail（失敗也要記錄）
            if (EnableAuditTrail)
            {
                string userId = SecurityContext.CurrentSession?.CurrentUserName ?? "Unknown";
                string batchId = ProcessContext.BatchNumber > 0 ? ProcessContext.BatchNumber.ToString() : "";

                ComplianceContext.LogAuditTrail(
                    deviceName: Label,
                    address: Address,
                    oldValue: oldValue,
                    newValue: $"{newValue} (FAILED)",
                    reason: "Parameter Change Failed",
                    parameter: $"User: {userId}",
                    batchId: batchId,
                    showInUi: true
                );

                // 立即刷新，確保 Audit Trail 寫入資料庫
                ComplianceContext.FlushLogs();
            }

            CyberMessageBox.Show(
                $"Failed to write {Label} to PLC.\nPlease check PLC connection.",
                "Write Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// 處理寫入異常
        /// </summary>
        private void HandleWriteException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PlcText] ApplyButton_Click Error: {ex.Message}");
            
            CyberMessageBox.Show(
                $"Error applying value:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }

    /// <summary>
    /// ValueApplied Event Args
    /// </summary>
    public class ValueAppliedEventArgs : EventArgs
    {
        public string Address { get; }
        public int Value { get; }
        public bool WriteSuccess { get; }

        public ValueAppliedEventArgs(string address, int value, bool writeSuccess)
        {
            Address = address;
            Value = value;
            WriteSuccess = writeSuccess;
        }
    }
}
