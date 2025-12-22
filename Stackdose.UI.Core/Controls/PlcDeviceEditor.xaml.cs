using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PLC Device Editor - 用於手動讀取和寫入 PLC 裝置數值
    /// 支援：Bit (M/X/Y)、Word (D/R)、Word Bit (D100.5 或 R2002,0)
    /// </summary>
    public partial class PlcDeviceEditor : UserControl
    {
        public PlcDeviceEditor()
        {
            InitializeComponent();
            
            // 訂閱權限變更事件
            SecurityContext.AccessLevelChanged += OnAccessLevelChanged;
            
            // 初始化權限狀態
            UpdateAuthorization();
            
            // 控制項卸載時取消訂閱
            this.Unloaded += (s, e) => SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
            
            // ? 控制項載入時，自動註冊監控地址
            this.Loaded += OnControlLoaded;
        }

        /// <summary>
        /// 控制項載入時，自動註冊監控地址
        /// </summary>
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            // 如果有設定 Address，自動註冊到監控服務
            if (!string.IsNullOrWhiteSpace(Address))
            {
                RegisterMonitorAddress();
            }
        }

        /// <summary>
        /// 註冊監控地址（根據 DWord 模式決定註冊數量）
        /// </summary>
        private void RegisterMonitorAddress()
        {
            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager?.Monitor == null) return;

            string addr = Address?.Trim().ToUpper() ?? "";
            if (string.IsNullOrEmpty(addr)) return;

            // 解析地址
            var match = System.Text.RegularExpressions.Regex.Match(addr, @"^([DR])(\d+)$");
            if (!match.Success) return; // 只支援 D/R 裝置

            // DWord 模式需要註冊兩個連續暫存器
            int length = IsDWordMode ? 2 : 1;

            manager.Monitor.Register(addr, length);

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlcDeviceEditor] Auto-registered: {addr}:{length}");
            #endif
        }

        #region Dependency Properties

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcDeviceEditor), new PropertyMetadata("Device Editor"));
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcDeviceEditor), new PropertyMetadata(""));
        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcDeviceEditor), new PropertyMetadata(""));
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ReasonProperty =
            DependencyProperty.Register("Reason", typeof(string), typeof(PlcDeviceEditor), new PropertyMetadata("Manual Operation"));
        public string Reason
        {
            get => (string)GetValue(ReasonProperty);
            set => SetValue(ReasonProperty, value);
        }

        public static readonly DependencyProperty EnableAuditTrailProperty =
            DependencyProperty.Register("EnableAuditTrail", typeof(bool), typeof(PlcDeviceEditor), new PropertyMetadata(true));
        public bool EnableAuditTrail
        {
            get => (bool)GetValue(EnableAuditTrailProperty);
            set => SetValue(EnableAuditTrailProperty, value);
        }

        public static readonly DependencyProperty RequiredLevelProperty =
            DependencyProperty.Register("RequiredLevel", typeof(AccessLevel), typeof(PlcDeviceEditor),
                new PropertyMetadata(AccessLevel.Supervisor, OnRequiredLevelChanged));
        public AccessLevel RequiredLevel
        {
            get => (AccessLevel)GetValue(RequiredLevelProperty);
            set => SetValue(RequiredLevelProperty, value);
        }

        public static readonly DependencyProperty IsAuthorizedProperty =
            DependencyProperty.Register("IsAuthorized", typeof(bool), typeof(PlcDeviceEditor), new PropertyMetadata(false));
        public bool IsAuthorized
        {
            get => (bool)GetValue(IsAuthorizedProperty);
            private set => SetValue(IsAuthorizedProperty, value);
        }

        #endregion

        #region 權限控制

        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateAuthorization);
        }

        private static void OnRequiredLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcDeviceEditor editor)
            {
                bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(editor);
                if (!isDesignMode)
                {
                    editor.UpdateAuthorization();
                }
                else
                {
                    editor.IsAuthorized = true;
                }
            }
        }

        private void UpdateAuthorization()
        {
            bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            
            if (isDesignMode)
            {
                IsAuthorized = true;
            }
            else
            {
                IsAuthorized = SecurityContext.HasAccess(RequiredLevel);
            }

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            // UI 狀態透過 XAML 綁定控制
        }

        #endregion

        #region DataType Selection

        /// <summary>
        /// DWord CheckBox 勾選事件
        /// </summary>
        private void ChkDWord_Checked(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[PlcDeviceEditor] DataType: DWord (32-bit)");
            #endif
            
            // ? 重新註冊監控地址（DWord 需要 2 個暫存器）
            RegisterMonitorAddress();
        }

        /// <summary>
        /// DWord CheckBox 取消勾選事件
        /// </summary>
        private void ChkDWord_Unchecked(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[PlcDeviceEditor] DataType: Word (16-bit)");
            #endif
            
            // ? 重新註冊監控地址（Word 只需要 1 個暫存器）
            RegisterMonitorAddress();
        }

        /// <summary>
        /// 判斷當前是否為 DWord 模式
        /// </summary>
        private bool IsDWordMode => ChkDWord?.IsChecked == true;

        #endregion

        #region Read/Write 操作

        private async void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAuthorized)
            {
                string opName = !string.IsNullOrEmpty(Label) ? $"讀取 {Label}" : "讀取 PLC";
                SecurityContext.CheckAccess(RequiredLevel, opName);
                await ShowFeedback(false);
                return;
            }

            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager == null || !manager.IsConnected)
            {
                await ShowFeedback(false);
                return;
            }

            string addr = Address?.Trim().ToUpper() ?? "";
            string reason = string.IsNullOrWhiteSpace(Reason) ? "Manual Read" : Reason.Trim();

            if (string.IsNullOrEmpty(addr))
            {
                await ShowFeedback(false);
                return;
            }

            try
            {
                var wordBitMatch = Regex.Match(addr, @"^([DRW][0-9]+)[.,]([0-9A-Fa-f]+)$");
                bool isPureBit = Regex.IsMatch(addr, @"^[MXY][0-9]+$");

                long readValue;  // 改為 long 以支援 DWord
                bool readSuccess = false;

                // ?? DWord 讀取模式
                if (IsDWordMode && !isPureBit && !wordBitMatch.Success)
                {
                    var dwordValue = manager.ReadDWord(addr);
                    if (dwordValue.HasValue)
                    {
                        readValue = dwordValue.Value;
                        readSuccess = true;
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[PlcDeviceEditor] DWord Read: {addr} = {readValue}");
                        #endif
                    }
                    else
                    {
                        await ShowFeedback(false);
                        return;
                    }
                }
                // Word Bit 模式
                else if (wordBitMatch.Success)
                {
                    string wordAddr = wordBitMatch.Groups[1].Value;
                    string bitIndexStr = wordBitMatch.Groups[2].Value;
                    int bitIndex = Convert.ToInt32(bitIndexStr, 16);
                    
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    int wordValue = await manager.ReadAsync(wordAddr);
                    readValue = (wordValue >> bitIndex) & 1;
                    readSuccess = true;
                }
                // Pure Bit 模式
                else if (isPureBit)
                {
                    readValue = await manager.ReadAsync(addr);
                    readSuccess = true;
                }
                // Word 模式（預設）
                else
                {
                    readValue = await manager.ReadAsync(addr);
                    readSuccess = true;
                }

                if (readSuccess)
                {
                    Value = readValue.ToString();
                    
                    if (EnableAuditTrail)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: Label,
                            address: addr,
                            oldValue: "N/A",
                            newValue: readValue.ToString(),
                            reason: $"{reason} (Read)",
                            showInUi: false
                        );
                    }

                    await ShowFeedback(true);
                }
                else
                {
                    await ShowFeedback(false);
                }
            }
            catch (Exception ex)
            {
                if (EnableAuditTrail)
                {
                    ComplianceContext.LogSystem($"[ERROR] Read failed: {Label}({addr}) - {ex.Message}", LogLevel.Error);
                }
                await ShowFeedback(false);
            }
        }

        private async void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAuthorized)
            {
                string opName = !string.IsNullOrEmpty(Label) ? $"寫入 {Label}" : "寫入 PLC";
                SecurityContext.CheckAccess(RequiredLevel, opName);
                await ShowFeedback(false);
                return;
            }

            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager == null || !manager.IsConnected)
            {
                await ShowFeedback(false);
                return;
            }

            string addr = Address?.Trim().ToUpper() ?? "";
            string valStr = Value?.Trim() ?? "";
            string reason = string.IsNullOrWhiteSpace(Reason) ? "Manual Operation" : Reason.Trim();

            if (string.IsNullOrEmpty(addr) || string.IsNullOrEmpty(valStr))
            {
                await ShowFeedback(false);
                return;
            }

            try
            {
                var wordBitMatch = Regex.Match(addr, @"^([DRW][0-9]+)[.,]([0-9A-Fa-f]+)$");
                bool isPureBit = Regex.IsMatch(addr, @"^[MXY][0-9]+$");

                string oldValue = "";
                bool writeSuccess = false;

                // ?? DWord 寫入模式
                if (IsDWordMode && !isPureBit && !wordBitMatch.Success)
                {
                    if (!uint.TryParse(valStr, out uint numVal))
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    // 讀取當前值
                    try
                    {
                        var currentDWord = manager.ReadDWord(addr);
                        oldValue = currentDWord.HasValue ? currentDWord.Value.ToString() : "Unknown";
                    }
                    catch
                    {
                        oldValue = "Unknown";
                    }

                    // ?? DWord 寫入需要分成兩個 Word
                    // Low Word (D65) = numVal & 0xFFFF
                    // High Word (D66) = (numVal >> 16) & 0xFFFF
                    
                    ushort lowWord = (ushort)(numVal & 0xFFFF);
                    ushort highWord = (ushort)((numVal >> 16) & 0xFFFF);
                    
                    // 解析位址（例如 D65 → D65, D66）
                    var match = Regex.Match(addr, @"^([A-Z]+)(\d+)$");
                    if (!match.Success)
                    {
                        await ShowFeedback(false);
                        return;
                    }
                    
                    string deviceType = match.Groups[1].Value;
                    int baseAddr = int.Parse(match.Groups[2].Value);
                    
                    // 寫入低位 Word
                    bool writeLowSuccess = await manager.WriteAsync($"{deviceType}{baseAddr},{lowWord}");
                    if (!writeLowSuccess)
                    {
                        await ShowFeedback(false);
                        return;
                    }
                    
                    // 寫入高位 Word
                    bool writeHighSuccess = await manager.WriteAsync($"{deviceType}{baseAddr + 1},{highWord}");
                    writeSuccess = writeLowSuccess && writeHighSuccess;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcDeviceEditor] DWord Write: {addr} = {numVal} (Low:{lowWord}, High:{highWord})");
                    #endif
                    
                    if (writeSuccess && EnableAuditTrail)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: Label,
                            address: addr,
                            oldValue: oldValue,
                            newValue: numVal.ToString(),
                            reason: reason
                        );
                    }

                    await ShowFeedback(writeSuccess);
                }
                else if (wordBitMatch.Success)
                {
                    string wordAddr = wordBitMatch.Groups[1].Value;
                    string bitIndexStr = wordBitMatch.Groups[2].Value;
                    int bitIndex = Convert.ToInt32(bitIndexStr, 16);
                    
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    int writeBitVal = ParseBitValue(valStr);
                    if (writeBitVal == -1)
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    int currentWordVal = await manager.ReadAsync(wordAddr);
                    int oldBitVal = (currentWordVal >> bitIndex) & 1;
                    oldValue = oldBitVal.ToString();

                    int newWordVal = writeBitVal == 1 
                        ? currentWordVal | (1 << bitIndex) 
                        : currentWordVal & ~(1 << bitIndex);

                    writeSuccess = await manager.WriteAsync($"{wordAddr},{newWordVal}");
                    
                    if (writeSuccess && EnableAuditTrail)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: Label,
                            address: addr,
                            oldValue: oldValue,
                            newValue: writeBitVal.ToString(),
                            reason: reason
                        );
                    }

                    await ShowFeedback(writeSuccess);
                }
                else if (isPureBit)
                {
                    int writeBitVal = ParseBitValue(valStr);
                    if (writeBitVal == -1)
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    try
                    {
                        int currentBitVal = await manager.ReadAsync(addr);
                        oldValue = currentBitVal.ToString();
                    }
                    catch
                    {
                        oldValue = "Unknown";
                    }

                    writeSuccess = await manager.WriteAsync($"{addr},{writeBitVal}");
                    
                    if (writeSuccess && EnableAuditTrail)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: Label,
                            address: addr,
                            oldValue: oldValue,
                            newValue: writeBitVal.ToString(),
                            reason: reason
                        );
                    }

                    await ShowFeedback(writeSuccess);
                }
                else
                {
                    if (!int.TryParse(valStr, out int numVal))
                    {
                        await ShowFeedback(false);
                        return;
                    }

                    try
                    {
                        int currentVal = await manager.ReadAsync(addr);
                        oldValue = currentVal.ToString();
                    }
                    catch
                    {
                        oldValue = "Unknown";
                    }

                    writeSuccess = await manager.WriteAsync($"{addr},{valStr}");
                    
                    if (writeSuccess && EnableAuditTrail)
                    {
                        ComplianceContext.LogAuditTrail(
                            deviceName: Label,
                            address: addr,
                            oldValue: oldValue,
                            newValue: valStr,
                            reason: reason
                        );
                    }

                    await ShowFeedback(writeSuccess);
                }
            }
            catch (Exception ex)
            {
                if (EnableAuditTrail)
                {
                    ComplianceContext.LogSystem($"[ERROR] Write failed: {Label}({addr}) - {ex.Message}", LogLevel.Error);
                }
                await ShowFeedback(false);
            }
        }

        #endregion

        #region Helper Methods

        private int ParseBitValue(string valStr)
        {
            valStr = valStr.ToLower();
            if (valStr == "0" || valStr == "false" || valStr == "off") return 0;
            if (valStr == "1" || valStr == "true" || valStr == "on") return 1;
            return -1;
        }

        private async Task ShowFeedback(bool success)
        {
            var color = success ? Colors.LimeGreen : Colors.Red;

            TxtValue.BorderBrush = new SolidColorBrush(color);
            TxtValue.BorderThickness = new Thickness(2);

            await Task.Delay(500);

            TxtValue.BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            TxtValue.BorderThickness = new Thickness(1);
        }

        #endregion
    }
}
