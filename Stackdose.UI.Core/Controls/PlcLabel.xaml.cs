using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PLC 數據類型枚舉
    /// </summary>
    /// <remarks>
    /// 定義 PlcLabel 支援的 PLC 數據類型
    /// </remarks>
    public enum PlcDataType
    {
        /// <summary>位元（顯示 ON/OFF）</summary>
        Bit,
        /// <summary>16-bit 整數</summary>
        Word,
        /// <summary>32-bit 整數</summary>
        DWord,
        /// <summary>32-bit 浮點數</summary>
        Float
    }

    /// <summary>
    /// PLC 數值變更事件參數
    /// </summary>
    /// <remarks>
    /// 當 PlcLabel 的值發生變化時，會透過此事件參數傳遞新值和顯示文字
    /// </remarks>
    public class PlcValueChangedEventArgs : EventArgs
    {
        /// <summary>原始數值</summary>
        public object? Value { get; }
        
        /// <summary>格式化後的顯示文字</summary>
        public string DisplayText { get; }
        
        /// <summary>
        /// 建構函數
        /// </summary>
        /// <param name="value">原始數值</param>
        /// <param name="displayText">顯示文字</param>
        public PlcValueChangedEventArgs(object? value, string displayText)
        {
            Value = value;
            DisplayText = displayText;
        }
    }

    /// <summary>
    /// PLC 數據顯示標籤控制項
    /// </summary>
    /// <remarks>
    /// <para>提供工業級 PLC 數據顯示功能，支援：</para>
    /// <list type="bullet">
    /// <item>自動連接 PLC 並即時更新數據</item>
    /// <item>多種數據類型支援（Bit/Word/DWord/Float）</item>
    /// <item>可自訂顏色主題（Dark/Light 自動適應）</item>
    /// <item>支援數值格式化（小數點、除數）</item>
    /// <item>提供矩形/圓形底框樣式</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法：
    /// <code>
    /// &lt;Custom:PlcLabel Label="溫度" Address="D100" /&gt;
    /// </code>
    /// 進階用法：
    /// <code>
    /// &lt;Custom:PlcLabel 
    ///     Label="溫度" 
    ///     Address="D100" 
    ///     Divisor="10"
    ///     StringFormat="F1"
    ///     LabelForeground="Warning"
    ///     FrameShape="Circle" /&gt;
    /// </code>
    /// </example>
    public partial class PlcLabel : UserControl
    {
        #region Private Fields

        /// <summary>已綁定的 PlcStatus 實例</summary>
        private PlcStatus? _boundStatus;

        /// <summary>快取的主題檢測結果（避免重複檢查）</summary>
        private bool? _cachedLightThemeResult;

        #endregion

        #region Events

        /// <summary>
        /// 數值變更事件
        /// </summary>
        /// <remarks>
        /// 當 PLC 數據更新時觸發，可用於自訂邏輯處理
        /// </remarks>
        public event EventHandler<PlcValueChangedEventArgs>? ValueChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        public PlcLabel()
        {
            InitializeComponent();
            this.Loaded += PlcLabel_Loaded;
            this.Unloaded += PlcLabel_Unloaded;
        }

        #endregion

        #region Theme Management

        /// <summary>
        /// 主題變化時重新應用底框顏色
        /// </summary>
        /// <remarks>
        /// 由 PlcLabelContext 透過 CyberFrame 的主題切換事件觸發
        /// </remarks>
        public void OnThemeChanged()
        {
            // 清除快取，強制重新檢測主題
            _cachedLightThemeResult = null;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[PlcLabel] 主題已變化，重新應用顏色");
            #endif
            
            UpdateFrameBackground();
        }

        /// <summary>
        /// 更新底框背景顏色
        /// </summary>
        /// <remarks>
        /// 僅當 FrameBackground 設為 DarkBlue 時才會進行主題適應
        /// </remarks>
        private void UpdateFrameBackground()
        {
            if (FrameBorder == null) return;

            // 只有 DarkBlue 主題才需要動態切換
            if (FrameBackground == PlcLabelColorTheme.DarkBlue)
            {
                bool isLightMode = IsLightTheme();
                
                FrameBorder.Background = new SolidColorBrush(
                    isLightMode 
                        ? Color.FromRgb(0xF5, 0xF5, 0xF5)  // Light: #F5F5F5 淺灰
                        : Color.FromRgb(0x1E, 0x1E, 0x2E)); // Dark: #1E1E2E 深藍
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlcLabel] 底框顏色已更新為 {(isLightMode ? "Light" : "Dark")} 模式");
                #endif
            }
        }

        /// <summary>
        /// 判斷當前是否為 Light 主題
        /// </summary>
        /// <returns>true 為 Light 模式，false 為 Dark 模式</returns>
        /// <remarks>
        /// 透過檢查 Plc.Bg.Main 資源的顏色來判斷主題
        /// 結果會被快取以提升效能
        /// </remarks>
        private bool IsLightTheme()
        {
            // 使用快取避免重複檢查（主題切換時會清除快取）
            if (_cachedLightThemeResult.HasValue)
            {
                return _cachedLightThemeResult.Value;
            }

            bool isLight = false;
            
            try
            {
                if (Application.Current?.TryFindResource("Plc.Bg.Main") is SolidColorBrush bgBrush)
                {
                    var bgColor = bgBrush.Color;
                    // RGB 值都大於 200 判定為淺色主題
                    isLight = bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcLabel] 主題檢測: {(isLight ? "Light" : "Dark")}, RGB({bgColor.R}, {bgColor.G}, {bgColor.B})");
                    #endif
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlcLabel] 主題檢測失敗: {ex.Message}");
                #endif
            }

            // 快取結果
            _cachedLightThemeResult = isLight;
            return isLight;
        }

        #endregion

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
            DependencyProperty.Register("DefaultValue", typeof(string), typeof(PlcLabel), new PropertyMetadata("00000"));
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

        // 🔥 12. 新增：是否顯示邊框和背景
        public static readonly DependencyProperty ShowFrameProperty =
            DependencyProperty.Register("ShowFrame", typeof(bool), typeof(PlcLabel), new PropertyMetadata(true));

        public bool ShowFrame
        {
            get { return (bool)GetValue(ShowFrameProperty); }
            set { SetValue(ShowFrameProperty, value); }
        }

        // 🔥 13. 新增：標籤文字大小
        public static readonly DependencyProperty LabelFontSizeProperty =
            DependencyProperty.Register("LabelFontSize", typeof(double), typeof(PlcLabel), new PropertyMetadata(12.0));

        public double LabelFontSize
        {
            get { return (double)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        // 🔥 14. 新增：數值文字大小
        public static readonly DependencyProperty ValueFontSizeProperty =
            DependencyProperty.Register("ValueFontSize", typeof(double), typeof(PlcLabel), new PropertyMetadata(20.0));

        public double ValueFontSize
        {
            get { return (double)GetValue(ValueFontSizeProperty); }
            set { SetValue(ValueFontSizeProperty, value); }
        }

        // 🔥 15. 新增：標籤對齊方式
        public static readonly DependencyProperty LabelAlignmentProperty =
            DependencyProperty.Register("LabelAlignment", typeof(HorizontalAlignment), typeof(PlcLabel), new PropertyMetadata(HorizontalAlignment.Left));

        public HorizontalAlignment LabelAlignment
        {
            get { return (HorizontalAlignment)GetValue(LabelAlignmentProperty); }
            set { SetValue(LabelAlignmentProperty, value); }
        }

        // 🔥 16. 新增：標籤顏色主題
        public static readonly DependencyProperty LabelForegroundProperty =
            DependencyProperty.Register("LabelForeground", typeof(PlcLabelColorTheme), typeof(PlcLabel), new PropertyMetadata(PlcLabelColorTheme.Default));

        public PlcLabelColorTheme LabelForeground
        {
            get { return (PlcLabelColorTheme)GetValue(LabelForegroundProperty); }
            set { SetValue(LabelForegroundProperty, value); }
        }

        // 🔥 17. 新增：數值顏色主題
        public static readonly DependencyProperty ValueForegroundProperty =
            DependencyProperty.Register("ValueForeground", typeof(PlcLabelColorTheme), typeof(PlcLabel), new PropertyMetadata(PlcLabelColorTheme.NeonBlue));

        public PlcLabelColorTheme ValueForeground
        {
            get { return (PlcLabelColorTheme)GetValue(ValueForegroundProperty); }
            set { SetValue(ValueForegroundProperty, value); }
        }

        // 🔥 18. 新增：數值對齊方式
        public static readonly DependencyProperty ValueAlignmentProperty =
            DependencyProperty.Register("ValueAlignment", typeof(HorizontalAlignment), typeof(PlcLabel), new PropertyMetadata(HorizontalAlignment.Right));

        public HorizontalAlignment ValueAlignment
        {
            get { return (HorizontalAlignment)GetValue(ValueAlignmentProperty); }
            set { SetValue(ValueAlignmentProperty, value); }
        }

        // 🔥 19. 新增：是否顯示位址
        public static readonly DependencyProperty ShowAddressProperty =
            DependencyProperty.Register("ShowAddress", typeof(bool), typeof(PlcLabel), new PropertyMetadata(true));

        public bool ShowAddress
        {
            get { return (bool)GetValue(ShowAddressProperty); }
            set { SetValue(ShowAddressProperty, value); }
        }

        // 🔥 20. 新增：底框形狀
        public static readonly DependencyProperty FrameShapeProperty =
            DependencyProperty.Register("FrameShape", typeof(PlcLabelFrameShape), typeof(PlcLabel), new PropertyMetadata(PlcLabelFrameShape.Rectangle));

        public PlcLabelFrameShape FrameShape
        {
            get { return (PlcLabelFrameShape)GetValue(FrameShapeProperty); }
            set { SetValue(FrameShapeProperty, value); }
        }

        // 🔥 21. 新增：底框背景顏色主題
        public static readonly DependencyProperty FrameBackgroundProperty =
            DependencyProperty.Register("FrameBackground", typeof(PlcLabelColorTheme), typeof(PlcLabel), new PropertyMetadata(PlcLabelColorTheme.DarkBlue));

        public PlcLabelColorTheme FrameBackground
        {
            get { return (PlcLabelColorTheme)GetValue(FrameBackgroundProperty); }
            set { SetValue(FrameBackgroundProperty, value); }
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
            // 🔥 註冊到 PlcLabelContext（用於自動監控）
            PlcLabelContext.Register(this);

            // 🔥 初始化底框顏色
            UpdateFrameBackground();
            
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
            // 🔥 註銷 PlcLabelContext
            PlcLabelContext.Unregister(this);
            
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
                case PlcDataType.Word: 
                    result = manager.ReadWord(Address); 
                    break;
                case PlcDataType.DWord: 
                    result = manager.ReadDWord(Address);
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcLabel] DWord Read: {Label} ({Address}) = {result}");
                    #endif
                    break;
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

            #if DEBUG
            if (DataType == PlcDataType.DWord)
            {
                System.Diagnostics.Debug.WriteLine($"[PlcLabel] UpdateValue: {Label} ({Address}) rawValue={rawValue} (Type: {rawValue?.GetType().Name})");
            }
            #endif

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
                        
                        #if DEBUG
                        if (DataType == PlcDataType.DWord)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PlcLabel] DWord Formatted: {Label} = {newValueStr} (原始:{dVal}, 除數:{Divisor})");
                        }
                        #endif
                    }
                    else
                    {
                        newValueStr = rawValue.ToString() ?? "-";
                        actualValue = rawValue;
                        
                        #if DEBUG
                        if (DataType == PlcDataType.DWord)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PlcLabel] DWord Parse Failed: {Label} rawValue={rawValue}");
                        }
                        #endif
                    }
                }
            }

            // 只有數值改變時才執行後續動作
            if (Value != newValueStr)
            {
                string oldValueStr = Value; // 紀錄舊值
                Value = newValueStr;

                // 1. 觕發事件
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