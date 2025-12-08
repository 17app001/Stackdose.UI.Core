using System;
using System.Windows;
using System.Windows.Controls;
using Stackdose.Abstractions.Hardware; // 引用 IPlcManager
using Stackdose.UI.Core.Helpers;       // 引用 Context

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 定義 PLC 讀取的資料型態
    /// </summary>
    public enum PlcDataType
    {
        Bit,    // 顯示 ON/OFF
        Word,   // 16-bit 整數
        DWord,  // 32-bit 整數
        Float   // 32-bit 浮點數
    }

    public partial class PlcLabel : UserControl
    {
        // 用來記錄目前綁定的 Status，以便取消訂閱防止記憶體洩漏
        private PlcStatus? _boundStatus;

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

        // 3. 數值 (Value) - 這是實際顯示在畫面上的值
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcLabel), new PropertyMetadata("-"));
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // 4. 預設顯示文字 (DefaultValue)
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

        // 6. Bit 指定 (0~15) - 當讀取 Word 但只想顯示其中某個 Bit 時使用
        public static readonly DependencyProperty BitIndexProperty =
            DependencyProperty.Register("BitIndex", typeof(int), typeof(PlcLabel), new PropertyMetadata(-1));
        public int BitIndex
        {
            get { return (int)GetValue(BitIndexProperty); }
            set { SetValue(BitIndexProperty, value); }
        }

        // 7. 綁定目標 PLC (優先權最高)
        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register("TargetStatus", typeof(PlcStatus), typeof(PlcLabel),
                new PropertyMetadata(null, OnTargetStatusChanged));

        public PlcStatus TargetStatus
        {
            get { return (PlcStatus)GetValue(TargetStatusProperty); }
            set { SetValue(TargetStatusProperty, value); }
        }

        #endregion

        // 當使用者手動綁定 TargetStatus 時觸發
        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcLabel label)
            {
                if (e.NewValue is PlcStatus newStatus)
                    label.BindToStatus(newStatus);
                else
                    label.TryResolveContextStatus(); // 如果被設為 null，嘗試改用自動繼承
            }
        }

        private void PlcLabel_Loaded(object sender, RoutedEventArgs e)
        {
            // 如果使用者沒有手動設定 TargetStatus，就自動去抓環境設定
            if (TargetStatus == null)
            {
                TryResolveContextStatus();
            }
        }

        /// <summary>
        /// 嘗試解析 PLC 來源 (懶人模式核心邏輯)
        /// </summary>
        private void TryResolveContextStatus()
        {
            // 優先順序：
            // 1. 父容器繼承 (PlcContext.GetStatus)
            // 2. 全域靜態變數 (PlcContext.GlobalStatus)
            var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;

            if (contextStatus != null)
            {
                BindToStatus(contextStatus);
            }
        }

        private void BindToStatus(PlcStatus? newStatus)
        {
            if (_boundStatus == newStatus) return;

            // 1. 取消舊的訂閱
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
            }

            _boundStatus = newStatus;

            // 2. 建立新的訂閱
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated += OnScanUpdated;

                // 若已連線，立即刷新一次，避免等待下一次掃描週期才顯示
                if (_boundStatus.CurrentManager != null)
                {
                    OnScanUpdated(_boundStatus.CurrentManager);
                }
            }
        }

        private void PlcLabel_Unloaded(object sender, RoutedEventArgs e)
        {
            // 離開畫面時一定要取消訂閱
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
                _boundStatus = null;
            }
        }

        // 這是從背景執行緒呼叫的回呼函式
        private void OnScanUpdated(IPlcManager manager)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;

                // 切回 UI 執行緒進行更新
                Dispatcher.Invoke(() =>
                {
                    if (!Dispatcher.HasShutdownStarted) RefreshFrom(manager);
                });
            }
            catch { }
        }

        /// <summary>
        /// 核心讀取邏輯：根據設定從 Manager 撈取數據
        /// </summary>
        public void RefreshFrom(IPlcManager manager)
        {
            if (manager == null) return;
            object? result = null;

            switch (DataType)
            {
                case PlcDataType.Bit:
                    // 如果有指定 BitIndex (0~15)，則讀取 Word 並拆解 Bit
                    if (BitIndex >= 0 && BitIndex <= 15)
                    {
                        var wordVal = manager.ReadWord(Address);
                        if (wordVal.HasValue)
                            result = ((wordVal.Value >> BitIndex) & 1) == 1;
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

            if (result == null) return;
            UpdateValue(result);
        }

        /// <summary>
        /// 更新 UI 顯示值 (僅當數值改變時才寫入屬性，節省效能)
        /// </summary>
        public void UpdateValue(object rawValue)
        {
            string newValue = "-";

            // 如果 rawValue 是 null (例如斷線或還沒讀到)，保持為 "-"
            // 這會觸發 XAML 的 DataTrigger 顯示 DefaultValue
            if (rawValue != null)
            {
                switch (DataType)
                {
                    case PlcDataType.Bit:
                        newValue = (rawValue is bool b && b) ? "ON" : "OFF";
                        break;

                    case PlcDataType.Float:
                        newValue = (double.TryParse(rawValue.ToString(), out double d))
                            ? d.ToString("F2") : rawValue.ToString();
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