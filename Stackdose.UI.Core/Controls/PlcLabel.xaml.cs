using System;
using System.Windows;
using System.Windows.Controls;
using Stackdose.Abstractions.Hardware; // 引用 IPlcManager

namespace Stackdose.UI.Core.Controls
{
    public enum PlcDataType
    {
        Bit,    // 顯示 ON/OFF
        Word,   // 16-bit 整數
        DWord,  // 32-bit 整數
        Float   // 32-bit 浮點數
    }

    public partial class PlcLabel : UserControl
    {
        // 用來記錄目前綁定的 Status，以便取消訂閱
        private PlcStatus? _boundStatus;

        public PlcLabel()
        {
            InitializeComponent();
            this.Unloaded += PlcLabel_Unloaded;
        }

        #region Dependency Properties

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcLabel), new PropertyMetadata("Label"));
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcLabel), new PropertyMetadata("D0"));
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcLabel), new PropertyMetadata("-"));
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue", typeof(string), typeof(PlcLabel), new PropertyMetadata("0000"));
        public string DefaultValue
        {
            get { return (string)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register("DataType", typeof(PlcDataType), typeof(PlcLabel), new PropertyMetadata(PlcDataType.Word));
        public PlcDataType DataType
        {
            get { return (PlcDataType)GetValue(DataTypeProperty); }
            set { SetValue(DataTypeProperty, value); }
        }

        // 🔥 新增：指定 Bit 位置 (0~15)
        // 當 DataType="Bit" 且此值 >= 0 時，會讀取 Address 的 Word 值並取出指定 Bit
        public static readonly DependencyProperty BitIndexProperty =
            DependencyProperty.Register("BitIndex", typeof(int), typeof(PlcLabel), new PropertyMetadata(-1));
        public int BitIndex
        {
            get { return (int)GetValue(BitIndexProperty); }
            set { SetValue(BitIndexProperty, value); }
        }

        // 綁定目標 PlcStatus
        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register("TargetStatus", typeof(PlcStatus), typeof(PlcLabel),
                new PropertyMetadata(null, OnTargetStatusChanged));

        public PlcStatus TargetStatus
        {
            get { return (PlcStatus)GetValue(TargetStatusProperty); }
            set { SetValue(TargetStatusProperty, value); }
        }

        #endregion

        // 當 TargetStatus 改變時 (例如在 XAML 綁定)
        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcLabel label)
            {
                label.BindToStatus(e.NewValue as PlcStatus);
            }
        }

        private void BindToStatus(PlcStatus? newStatus)
        {
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
            }

            _boundStatus = newStatus;

            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated += OnScanUpdated;
            }
        }

        private void PlcLabel_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
            }
        }

        private void OnScanUpdated(IPlcManager manager)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;

                Dispatcher.Invoke(() =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        RefreshFrom(manager);
                    }
                });
            }
            catch { }
        }

        /// <summary>
        /// 從 Manager 讀取最新數據
        /// </summary>
        public void RefreshFrom(IPlcManager manager)
        {
            if (manager == null) return;
            object? result = null;

            switch (DataType)
            {
                case PlcDataType.Bit:
                    // 如果有指定 BitIndex (0~15)，則讀取 Word 並拆解 Bit
                    // 用於讀取 D100.5 這種 Word 內的 Bit
                    if (BitIndex >= 0 && BitIndex <= 15)
                    {
                        var wordVal = manager.ReadWord(Address);
                        if (wordVal.HasValue)
                        {
                            // 使用位元運算取出第 n 位: (Value >> n) & 1
                            result = ((wordVal.Value >> BitIndex) & 1) == 1;
                        }
                    }
                    else
                    {
                        // 否則當作一般 Bit 裝置 (如 M0, X0) 直接讀取
                        result = manager.ReadBit(Address);
                    }
                    break;

                case PlcDataType.Word:
                    result = manager.ReadWord(Address);
                    break;
                case PlcDataType.DWord:
                    result = manager.ReadDWord(Address);
                    break;
                case PlcDataType.Float:
                    var dwordVal = manager.ReadDWord(Address);
                    if (dwordVal.HasValue)
                        result = BitConverter.ToSingle(BitConverter.GetBytes(dwordVal.Value), 0);
                    break;
            }
            UpdateValue(result);
        }

        /// <summary>
        /// 更新 UI 顯示值 (有變更才更新)
        /// </summary>
        public void UpdateValue(object rawValue)
        {
            string newValue = "-";

            if (rawValue != null)
            {
                switch (DataType)
                {
                    case PlcDataType.Bit:
                        newValue = (rawValue is bool b && b) ? "ON" : "OFF";
                        break;
                    case PlcDataType.Float:
                        newValue = (double.TryParse(rawValue.ToString(), out double d)) ? d.ToString("F2") : rawValue.ToString();
                        break;
                    default:
                        newValue = rawValue.ToString();
                        break;
                }
            }

            if (Value != newValue)
            {
                Value = newValue;
            }
        }
    }
}