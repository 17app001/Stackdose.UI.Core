using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers; // 引用 Context

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcText : UserControl
    {
        public PlcText()
        {
            InitializeComponent();
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

        #endregion

        private async void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            // 1. 取得 PLC Manager (懶人模式：自動抓 Context 或 Global)
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
                    int newWordVal;

                    if (writeBitVal == 1)
                        newWordVal = currentWordVal | (1 << bitIndex); // Set bit (OR 運算)
                    else
                        newWordVal = currentWordVal & ~(1 << bitIndex); // Reset bit (AND NOT 運算)

                    // 寫入回 PLC
                    bool ok = await manager.WriteAsync($"{wordAddr},{newWordVal}");
                    await ShowFeedback(ok);
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

                    // 直接寫入 (例如 "M0,1")
                    bool ok = await manager.WriteAsync($"{addr},{writeBitVal}");
                    await ShowFeedback(ok);
                }
                else
                {
                    // === 一般 Word/DWord 模式 (D100) ===
                    if (!int.TryParse(valStr, out int numVal))
                    {
                        await ShowFeedback(false); // 格式錯誤，閃紅框
                        return;
                    }

                    // 直接寫入 (例如 "D100,1234")
                    bool ok = await manager.WriteAsync($"{addr},{valStr}");
                    await ShowFeedback(ok);
                }
            }
            catch (Exception)
            {
                // 發生任何異常 (如通訊逾時、格式錯誤)，一律閃紅框，不彈出視窗
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