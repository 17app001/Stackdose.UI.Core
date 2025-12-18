using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers; // 引用 Context
using Stackdose.UI.Core.Models; // 引用 LogLevel

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcText : UserControl
    {
        public PlcText()
        {
            InitializeComponent();
            
            // 🔥 訂閱權限變更事件
            SecurityContext.AccessLevelChanged += OnAccessLevelChanged;
            
            // 🔥 初始化權限狀態
            UpdateAuthorization();
            
            // 🔥 當控制項卸載時取消訂閱
            this.Unloaded += (s, e) => SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
        }

        #region Dependency Properties

        // 標題文字 (例如 "手動測試區")
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcText), new PropertyMetadata("Input Test"));
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // 位址 (例如 "D100")
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcText), new PropertyMetadata(""));
        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        // 數值 (例如 "1234")
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PlcText), new PropertyMetadata(""));
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // 修改原因 (用於審計軌跡)
        public static readonly DependencyProperty ReasonProperty =
            DependencyProperty.Register("Reason", typeof(string), typeof(PlcText), new PropertyMetadata("Manual Operation"));
        public string Reason
        {
            get { return (string)GetValue(ReasonProperty); }
            set { SetValue(ReasonProperty, value); }
        }

        // 是否啟用審計軌跡 (預設：True)
        public static readonly DependencyProperty EnableAuditTrailProperty =
            DependencyProperty.Register("EnableAuditTrail", typeof(bool), typeof(PlcText), new PropertyMetadata(true));
        public bool EnableAuditTrail
        {
            get { return (bool)GetValue(EnableAuditTrailProperty); }
            set { SetValue(EnableAuditTrailProperty, value); }
        }

        // 🔥 新增：所需權限等級（預設：Supervisor）
        public static readonly DependencyProperty RequiredLevelProperty =
            DependencyProperty.Register("RequiredLevel", typeof(AccessLevel), typeof(PlcText),
                new PropertyMetadata(AccessLevel.Supervisor, OnRequiredLevelChanged));
        public AccessLevel RequiredLevel
        {
            get { return (AccessLevel)GetValue(RequiredLevelProperty); }
            set { SetValue(RequiredLevelProperty, value); }
        }

        // 🔥 新增：是否已授權（自動計算）
        public static readonly DependencyProperty IsAuthorizedProperty =
            DependencyProperty.Register("IsAuthorized", typeof(bool), typeof(PlcText),
                new PropertyMetadata(false));
        public bool IsAuthorized
        {
            get { return (bool)GetValue(IsAuthorizedProperty); }
            private set { SetValue(IsAuthorizedProperty, value); }
        }

        #endregion

        #region 權限控制

        /// <summary>
        /// 當權限等級變更時觸發
        /// </summary>
        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateAuthorization);
        }

        /// <summary>
        /// 當所需權限等級變更時觸發
        /// </summary>
        private static void OnRequiredLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcText plcText)
            {
                // 🔥 只在執行時更新權限
                bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(plcText);
                if (!isDesignMode)
                {
                    plcText.UpdateAuthorization();
                }
                else
                {
                    // 設計時：強制啟用
                    plcText.IsAuthorized = true;
                }
            }
        }

        /// <summary>
        /// 更新授權狀態
        /// </summary>
        private void UpdateAuthorization()
        {
            // 🔥 檢查設計模式
            bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            
            if (isDesignMode)
            {
                // 設計時：強制設定為已授權（讓控制項可見）
                IsAuthorized = true;
            }
            else
            {
                // 執行時：實際檢查權限
                IsAuthorized = SecurityContext.HasAccess(RequiredLevel);
            }

            // 🔥 更新 UI 狀態（啟用/禁用）
            UpdateUIState();
        }

        /// <summary>
        /// 更新 UI 狀態
        /// </summary>
        private void UpdateUIState()
        {
            // 在 XAML 中透過綁定控制 IsEnabled
            // 這裡只需要確保 IsAuthorized 屬性正確更新
        }

        #endregion

        private async void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 1. 先檢查權限
            if (!IsAuthorized)
            {
                string opName = !string.IsNullOrEmpty(Label) ? $"寫入 {Label}" : "寫入 PLC";
                SecurityContext.CheckAccess(RequiredLevel, opName);
                
                // 閃紅框提示
                await ShowFeedback(false);
                return;
            }

            // 2. 取得 PLC Manager (懶人模式：自動抓 Context 或 Global)
            // 優先順序：父容器繼承 > 全域變數
            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager == null || !manager.IsConnected)
            {
                // PLC 未連線時，不彈出視窗，改為閃爍紅框提示
                await ShowFeedback(false);
                return;
            }

            // 2. 驗證輸入
            string addr = Address?.Trim().ToUpper() ?? "";
            string valStr = Value?.Trim() ?? "";
            string reason = string.IsNullOrWhiteSpace(Reason) ? "Manual Operation" : Reason.Trim();

            if (string.IsNullOrEmpty(addr) || string.IsNullOrEmpty(valStr))
            {
                await ShowFeedback(false); // 空值也視為錯誤
                return;
            }

            try
            {
                // 3. 智慧判斷邏輯

                // 判斷是否為 Word Bit 模式 (例如 D100.5 或 D100.A)
                // Regex 說明: [DRW]開頭 + 數字 + 小數點 + 數字(或A-F代表hex)
                var wordBitMatch = Regex.Match(addr, @"^([DRW][0-9]+)\.([0-9A-Fa-f]+)$");

                // 判斷是否為純 Bit 裝置 (M, X, Y)
                bool isPureBit = Regex.IsMatch(addr, @"^[MXY][0-9]+$");

                string oldValue = ""; // 用於記錄舊值
                bool writeSuccess = false;

                if (wordBitMatch.Success)
                {
                    // === Word Bit 模式 (讀取 -> 修改 -> 寫入) ===
                    string wordAddr = wordBitMatch.Groups[1].Value; // D100
                    string bitIndexStr = wordBitMatch.Groups[2].Value; // 5 or A

                    // 解析 Bit Index (支援 Hex，例如 A=10)
                    int bitIndex = Convert.ToInt32(bitIndexStr, 16);
                    if (bitIndex < 0 || bitIndex > 15)
                    {
                        await ShowFeedback(false); // 格式錯誤，閃紅框
                        return;
                    }

                    // 解析寫入值 (只允許 0/1)
                    int writeBitVal = ParseBitValue(valStr);
                    if (writeBitVal == -1)
                    {
                        await ShowFeedback(false); // 格式錯誤，閃紅框
                        return;
                    }

                    // 執行 Read-Modify-Write
                    int currentWordVal = await manager.ReadAsync(wordAddr);
                    
                    // 記錄舊值 (該 Bit 的值)
                    int oldBitVal = (currentWordVal >> bitIndex) & 1;
                    oldValue = oldBitVal.ToString();

                    int newWordVal;
                    if (writeBitVal == 1)
                        newWordVal = currentWordVal | (1 << bitIndex); // Set bit (OR 運算)
                    else
                        newWordVal = currentWordVal & ~(1 << bitIndex); // Reset bit (AND NOT 運算)

                    // 寫入回 PLC
                    writeSuccess = await manager.WriteAsync($"{wordAddr},{newWordVal}");
                    
                    // 審計軌跡記錄
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
                    // === 純 Bit 裝置模式 (M0, X10) ===
                    int writeBitVal = ParseBitValue(valStr);
                    if (writeBitVal == -1)
                    {
                        await ShowFeedback(false); // 格式錯誤，閃紅框
                        return;
                    }

                    // 🔥 新增：先讀取舊值
                    try
                    {
                        int currentBitVal = await manager.ReadAsync(addr);
                        oldValue = currentBitVal.ToString();
                    }
                    catch
                    {
                        oldValue = "Unknown"; // 讀取失敗時標記為未知
                    }

                    // 直接寫入 (例如 "M0,1")
                    writeSuccess = await manager.WriteAsync($"{addr},{writeBitVal}");
                    
                    // 審計軌跡記錄
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
                    // === 一般 Word/DWord 模式 (D100) ===
                    if (!int.TryParse(valStr, out int numVal))
                    {
                        await ShowFeedback(false); // 格式錯誤，閃紅框
                        return;
                    }

                    // 🔥 新增：先讀取舊值
                    try
                    {
                        int currentVal = await manager.ReadAsync(addr);
                        oldValue = currentVal.ToString();
                    }
                    catch
                    {
                        oldValue = "Unknown"; // 讀取失敗時標記為未知
                    }

                    // 直接寫入 (例如 "D100,1234")
                    writeSuccess = await manager.WriteAsync($"{addr},{valStr}");
                    
                    // 審計軌跡記錄
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
                // 發生任何異常 (如通訊逾時、格式錯誤)，一律閃紅框，不彈出視窗
                // 🔥 新增：記錄異常到 Compliance 系統
                if (EnableAuditTrail)
                {
                    ComplianceContext.LogSystem($"[ERROR] Write failed: {Label}({addr}) - {ex.Message}", LogLevel.Error);
                }
                await ShowFeedback(false);
            }
        }

        /// <summary>
        /// 解析 Bit 值 (支援 0/1, true/false, on/off)
        /// </summary>
        private int ParseBitValue(string valStr)
        {
            valStr = valStr.ToLower();
            if (valStr == "0" || valStr == "false" || valStr == "off") return 0;
            if (valStr == "1" || valStr == "true" || valStr == "on") return 1;
            return -1; // 無效值
        }

        /// <summary>
        /// 顯示寫入結果的回饋 (成功閃綠框，失敗閃紅框)
        /// </summary>
        private async Task ShowFeedback(bool success)
        {
            // 根據成功失敗決定顏色
            var color = success ? Colors.LimeGreen : Colors.Red;

            TxtValue.BorderBrush = new SolidColorBrush(color);
            TxtValue.BorderThickness = new Thickness(2);

            await Task.Delay(500);

            // 復原
            TxtValue.BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)); // 這是原本的灰色
            TxtValue.BorderThickness = new Thickness(1);
        }
    }
}