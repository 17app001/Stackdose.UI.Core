using System;
using System.Windows;
using System.Windows.Controls;
using Stackdose.Abstractions.Hardware; // 引用 IPlcManager
using Stackdose.UI.Core.Helpers;       // 引用 Context

namespace Stackdose.UI.Core.Controls
{
    public enum PlcDataType
    {
        Bit,    // 顯示 ON/OFF
        Word,   // 16-bit 整數
        DWord,  // 32-bit 整數
        Float   // 32-bit 浮點數
    }

    // 🔥 新增：事件參數，包裝了「處理後的數值」與「顯示文字」
    public class PlcValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 實際數值 (例如 double 的 35.5, 或 bool 的 true)
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// 顯示的文字 (例如 "35.5", "ON")
        /// </summary>
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
        // 🔥 新增：數值變更事件
        public event EventHandler<PlcValueChangedEventArgs>? ValueChanged;

        public PlcLabel()
        {
            InitializeComponent();
            this.Loaded += PlcLabel_Loaded;
            this.Unloaded += PlcLabel_Unloaded;
        }

        #region Dependency Properties

        // 1. 標題 (Label)
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcLabel), new PropertyMetadata("Label"));
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // 2. PLC 位址 (Address)
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcLabel), new PropertyMetadata("D0"));
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        // 3. 數值 (Value)
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcLabel), new PropertyMetadata("-"));
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // 4. 預設顯示文字
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue", typeof(string), typeof(PlcLabel), new PropertyMetadata("0000"));
        public string DefaultValue
        {
            get { return (string)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        // 5. 資料型態 (DataType)
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register("DataType", typeof(PlcDataType), typeof(PlcLabel), new PropertyMetadata(PlcDataType.Word));
        public PlcDataType DataType
        {
            get { return (PlcDataType)GetValue(DataTypeProperty); }
            set { SetValue(DataTypeProperty, value); }
        }

        // 6. Bit 指定
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

        // 🔥 8. 新增：除數 (預設 1)
        public static readonly DependencyProperty DivisorProperty =
            DependencyProperty.Register("Divisor", typeof(double), typeof(PlcLabel), new PropertyMetadata(1.0));
        public double Divisor
        {
            get { return (double)GetValue(DivisorProperty); }
            set { SetValue(DivisorProperty, value); }
        }

        // 🔥 9. 新增：顯示格式 (預設 "F1" 代表一位小數，如 35.0)
        // 如果 Divisor 不為 1 或 DataType 為 Float，會套用此格式
        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register("StringFormat", typeof(string), typeof(PlcLabel), new PropertyMetadata("F1"));
        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        #endregion

        // ... (中間的自動綁定與事件邏輯保持不變，為節省篇幅略過，請保留原本的程式碼) ...

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
        /// 更新 UI 顯示值並觸發事件
        /// </summary>
        public void UpdateValue(object rawValue)
        {
            string newValueStr = "-";
            object? actualValue = null; // 這是要傳給事件的「真實數值」(double/bool)

            if (rawValue != null)
            {
                if (DataType == PlcDataType.Bit)
                {
                    bool bVal = false;
                    if (rawValue is bool b) bVal = b;
                    else bVal = rawValue.ToString() == "1" || rawValue.ToString().ToLower() == "true";

                    newValueStr = bVal ? "ON" : "OFF";
                    actualValue = bVal; // 事件拿到的是 bool
                }
                else
                {
                    // 數值處理 (除法)
                    if (double.TryParse(rawValue.ToString(), out double dVal))
                    {
                        double finalVal = dVal / Divisor;
                        actualValue = finalVal; // 事件拿到的是 double (已經除過的)

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

            // 只有數值改變時才更新 UI 並觸發事件
            if (Value != newValueStr)
            {
                Value = newValueStr;
                // 🔥 觸發事件，把真正的數值傳出去
                ValueChanged?.Invoke(this, new PlcValueChangedEventArgs(actualValue, newValueStr));
            }
        }
    }
}