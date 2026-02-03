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
    public partial class PlcText : UserControl
    {
        public PlcText()
        {
            InitializeComponent();
            Loaded += PlcText_Loaded;
        }

        #region Dependency Properties

        /// <summary>
        /// Label 文字
        /// </summary>
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

        /// <summary>
        /// PLC Address (例如: "D100")
        /// </summary>
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

        /// <summary>
        /// 當前值
        /// </summary>
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

        /// <summary>
        /// PLC Manager 實例（可選）
        /// 如果不設定，會自動從 PlcContext 取得
        /// </summary>
        public static readonly DependencyProperty PlcManagerProperty =
            DependencyProperty.Register(
                nameof(PlcManager),
                typeof(IPlcManager),
                typeof(PlcText),
                new PropertyMetadata(null));

        public IPlcManager? PlcManager
        {
            get => (IPlcManager?)GetValue(PlcManagerProperty);
            set => SetValue(PlcManagerProperty, value);
        }

        /// <summary>
        /// ValueApplied 事件 - 當使用者按下 Apply 按鈕時觸發
        /// </summary>
        public event EventHandler<ValueAppliedEventArgs>? ValueApplied;

        #endregion

        #region Private Methods

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            // 確保 Value 不為 null
            return baseValue ?? "0";
        }

        private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcText plcText && !string.IsNullOrEmpty(e.NewValue?.ToString()))
            {
                plcText.ReadFromPlc();
            }
        }

        private void PlcText_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Address))
            {
                ReadFromPlc();
            }
        }

        /// <summary>
        /// 從 PLC 讀取初始值
        /// </summary>
        private void ReadFromPlc()
        {
            try
            {
                var manager = PlcManager ?? PlcContext.GlobalStatus?.CurrentManager;
                if (manager == null || !manager.IsConnected || string.IsNullOrEmpty(Address))
                {
                    return;
                }

                // 嘗試讀取值 - 使用 ReadWord
                short? readValue = manager.ReadWord(Address);
                if (readValue.HasValue)
                {
                    Value = readValue.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlcText] ReadFromPlc Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply Button Click Handler
        /// </summary>
        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 驗證輸入
                if (!int.TryParse(Value, out int intValue))
                {
                    CyberMessageBox.Show(
                        $"Invalid value for {Label}. Please enter a valid number.",
                        "Invalid Input",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 寫入 PLC
                bool writeSuccess = false;
                var manager = PlcManager ?? PlcContext.GlobalStatus?.CurrentManager;
                
                if (manager != null && manager.IsConnected && !string.IsNullOrEmpty(Address))
                {
                    // 使用 WriteAsync 方法，格式: "D100=123"
                    string writeCommand = $"{Address}={intValue}";
                    writeSuccess = await manager.WriteAsync(writeCommand);
                }

                // 觸發事件
                var args = new ValueAppliedEventArgs(Address, intValue, writeSuccess);
                ValueApplied?.Invoke(this, args);

                // 記錄日誌
                if (writeSuccess)
                {
                    ComplianceContext.LogSystem(
                        $"[PlcText] {Label} ({Address}) set to {intValue}",
                        LogLevel.Success,
                        showInUi: true);

                    // 顯示成功提示（可選）
                    // CyberMessageBox.Show($"{Label} updated to {intValue}", "Success", 
                    //     MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ComplianceContext.LogSystem(
                        $"[PlcText] Failed to write {Label} ({Address})",
                        LogLevel.Warning,
                        showInUi: true);

                    CyberMessageBox.Show(
                        $"Failed to write {Label} to PLC.\nPlease check PLC connection.",
                        "Write Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlcText] ApplyButton_Click Error: {ex.Message}");
                
                CyberMessageBox.Show(
                    $"Error applying value:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
