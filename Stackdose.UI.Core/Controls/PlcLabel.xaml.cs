using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers; // 引用 Context 與 合規引擎
using System;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Core.Controls
{
    public enum PlcDataType
    {
        Bit,    // 顯示 ON/OFF
        Word,   // 16-bit 整數
        DWord,  // 32-bit 整數
        Float   // 32-bit 浮點數
    }

    /// <summary>
    /// 數值變更事件參數
    /// </summary>
    public class PlcValueChangedEventArgs : EventArgs
    {
        public object? Value { get; }
        public string DisplayText { get; }
        public PlcValueChangedEventArgs(object? value, string displayText)
        {
            Value = value;
            DisplayText = displayText;
        }
    }

    public partial class PlcLabel : UserControl
    {
        private PlcStatus? _boundStatus;

        public event EventHandler<PlcValueChangedEventArgs>? ValueChanged;

        public PlcLabel()
        {
            InitializeComponent();
            this.Loaded += PlcLabel_Loaded;
            this.Unloaded += PlcLabel_Unloaded;
        }

        #region Dependency Properties

        // 1. 標題
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcLabel), new PropertyMetadata("Label"));
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // 2. PLC 位址
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcLabel), new PropertyMetadata("D0"));
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        // 3. 顯示數值
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcLabel), new PropertyMetadata("-"));
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // 4. 預設文字 (當無數據時顯示)
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue", typeof(string), typeof(PlcLabel), new PropertyMetadata("0000"));
        public string DefaultValue
        {
            get { return (string)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        // 5. 資料型態
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register("DataType", typeof(PlcDataType), typeof(PlcLabel), new PropertyMetadata(PlcDataType.Word));
        public PlcDataType DataType
        {
            get { return (PlcDataType)GetValue(DataTypeProperty); }
            set { SetValue(DataTypeProperty, value); }
        }

        // 6. Bit 指定 (讀取 Word 中的特定 Bit, 0~15)
        public static readonly DependencyProperty BitIndexProperty =
            DependencyProperty.Register("BitIndex", typeof(int), typeof(PlcLabel), new PropertyMetadata(-1));
        public int BitIndex
        {
            get { return (int)GetValue(BitIndexProperty); }
            set { SetValue(BitIndexProperty, value); }
        }

        // 7. 綁定目標 PLC
        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register("TargetStatus", typeof(PlcStatus), typeof(PlcLabel),
                new PropertyMetadata(null, OnTargetStatusChanged));
        public PlcStatus TargetStatus
        {
            get { return (PlcStatus)GetValue(TargetStatusProperty); }
            set { SetValue(TargetStatusProperty, value); }
        }

        // 8. 除數
        public static readonly DependencyProperty DivisorProperty =
            DependencyProperty.Register("Divisor", typeof(double), typeof(PlcLabel), new PropertyMetadata(1.0));
        public double Divisor
        {
            get { return (double)GetValue(DivisorProperty); }
            set { SetValue(DivisorProperty, value); }
        }

        // 9. 顯示格式
        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register("StringFormat", typeof(string), typeof(PlcLabel), new PropertyMetadata("F1"));
        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        // 10. 是否啟用合規數據紀錄 (Data Logging)
        public static readonly DependencyProperty EnableDataLogProperty =
            DependencyProperty.Register("EnableDataLog", typeof(bool), typeof(PlcLabel), new PropertyMetadata(false));
        public bool EnableDataLog
        {
            get { return (bool)GetValue(EnableDataLogProperty); }
            set { SetValue(EnableDataLogProperty, value); }
        }

        // 🔥 11. 新增：是否啟用審計軌跡 (Audit Trail for Read Value Changes)
        public static readonly DependencyProperty EnableAuditTrailProperty =
            DependencyProperty.Register("EnableAuditTrail", typeof(bool), typeof(PlcLabel), new PropertyMetadata(false));
        public bool EnableAuditTrail
        {
            get { return (bool)GetValue(EnableAuditTrailProperty); }
            set { SetValue(EnableAuditTrailProperty, value); }
        }

      
        public static readonly DependencyProperty ShowLogProperty =
            DependencyProperty.Register("ShowLog", typeof(bool), typeof(PlcLabel), new PropertyMetadata(true));

        public bool ShowLog
        {
            get { return (bool)GetValue(ShowLogProperty); }
            set { SetValue(ShowLogProperty, value); }
        }

        #endregion

        // ... (自動綁定與事件邏輯) ...
        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcLabel label)
            {
                if (e.NewValue is PlcStatus newStatus) label.BindToStatus(newStatus);
                else label.TryResolveContextStatus();
            }
        }

        private void PlcLabel_Loaded(object sender, RoutedEventArgs e)
        {
            if (TargetStatus == null) TryResolveContextStatus();
        }

        private void TryResolveContextStatus()
        {
            var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            if (contextStatus != null) BindToStatus(contextStatus);
        }

        private void BindToStatus(PlcStatus? newStatus)
        {
            if (_boundStatus == newStatus) return;
            if (_boundStatus != null) _boundStatus.ScanUpdated -= OnScanUpdated;
            _boundStatus = newStatus;
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated += OnScanUpdated;
                if (_boundStatus.CurrentManager != null) OnScanUpdated(_boundStatus.CurrentManager);
            }
        }

        private void PlcLabel_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_boundStatus != null) { _boundStatus.ScanUpdated -= OnScanUpdated; _boundStatus = null; }
        }

        private void OnScanUpdated(IPlcManager manager)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;
                Dispatcher.Invoke(() => { if (!Dispatcher.HasShutdownStarted) RefreshFrom(manager); });
            }
            catch { }
        }

        public void RefreshFrom(IPlcManager manager)
        {
            if (manager == null) return;
            object? result = null;

            switch (DataType)
            {
                case PlcDataType.Bit:
                    if (BitIndex >= 0 && BitIndex <= 15)
                    {
                        var wordVal = manager.ReadWord(Address);
                        if (wordVal.HasValue) result = ((wordVal.Value >> BitIndex) & 1) == 1;
                    }
                    else result = manager.ReadBit(Address);
                    break;
                case PlcDataType.Word: result = manager.ReadWord(Address); break;
                case PlcDataType.DWord: result = manager.ReadDWord(Address); break;
                case PlcDataType.Float:
                    var dwordVal = manager.ReadDWord(Address);
                    if (dwordVal.HasValue) result = BitConverter.ToSingle(BitConverter.GetBytes(dwordVal.Value), 0);
                    break;
            }
            UpdateValue(result);
        }

        /// <summary>
        /// 更新數值、格式化、觸發事件與紀錄 Log
        /// </summary>
        public void UpdateValue(object rawValue)
        {
            string newValueStr = "-";
            object? actualValue = null;

            if (rawValue != null)
            {
                if (DataType == PlcDataType.Bit)
                {
                    bool bVal = false;
                    if (rawValue is bool b) bVal = b;
                    else bVal = rawValue.ToString() == "1" || rawValue.ToString().ToLower() == "true";

                    newValueStr = bVal ? "ON" : "OFF";
                    actualValue = bVal;
                }
                else
                {
                    // 數值運算 (除法 + 格式化)
                    if (double.TryParse(rawValue.ToString(), out double dVal))
                    {
                        double finalVal = dVal / Divisor;
                        actualValue = finalVal;

                        if (DataType == PlcDataType.Float || Divisor != 1.0)
                            newValueStr = finalVal.ToString(StringFormat);
                        else
                            newValueStr = finalVal.ToString();
                    }
                    else
                    {
                        newValueStr = rawValue.ToString() ?? "-";
                        actualValue = rawValue;
                    }
                }
            }

            // 只有數值改變時才執行後續動作
            if (Value != newValueStr)
            {
                string oldValueStr = Value; // 紀錄舊值
                Value = newValueStr;

                // 1. 觸發事件
                ValueChanged?.Invoke(this, new PlcValueChangedEventArgs(actualValue, newValueStr));

                // 🔥 1.5. 自動通知 PlcLabelContext（統一管理中心）
                PlcLabelContext.NotifyValueChanged(this, actualValue ?? newValueStr);

                // 2. 自動合規紀錄 - Data History (生產履歷)
                if (EnableDataLog && newValueStr != "-" && !string.IsNullOrEmpty(Label))
                {
                    //ComplianceContext.LogDataHistory(Label, Address, newValueStr);
                    // 捕獲變數以避免閉包問題
                    string logLabel = Label;
                    string logAddr = Address;
                    string logVal = newValueStr;

                    Task.Run(() =>
                    {
                     
                        ComplianceContext.LogDataHistory(logLabel, logAddr, logVal);
                    });
                }

                // 3. 🔥 自動合規紀錄 - Audit Trail (關鍵狀態變動追蹤)
                // 只有在 EnableAuditTrail 為 True，且數值真正有意義地改變時才紀錄
                if (EnableAuditTrail && newValueStr != "-" && oldValueStr != "-" && !string.IsNullOrEmpty(Label) && oldValueStr != newValueStr)
                {
                    string logLabel = Label;
                    string logAddr = Address;
                    string oldVal = oldValueStr;
                    string logVal = newValueStr;
                    bool showInUi = ShowLog; // 🔥 這裡讀取新的屬性
                    Task.Run(() =>
                    {
                       
                        // 由於這是自動讀取，我們將 Reason 標記為系統自動追蹤
                        ComplianceContext.LogAuditTrail(
                        logLabel,
                        logAddr,
                        oldVal,
                        logVal,
                        "System Auto-Read Change",
                        showInUi
                    );
                    });
                }
            }
        }
    }
}