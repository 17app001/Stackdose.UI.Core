using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers; // 引用 Context
using System;
using System.Linq;
using System.Text.RegularExpressions; // 用於判斷位址格式
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcText : UserControl
    {
        public PlcText()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // 位址 (例如 "D100")
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(PlcText), new PropertyMetadata(""));
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
            var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager == null || !manager.IsConnected)
            {
                MessageBox.Show("PLC 未連線！", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. 驗證輸入
            string addr = Address?.Trim().ToUpper() ?? "";
            string valStr = Value?.Trim() ?? "";

            if (string.IsNullOrEmpty(addr) || string.IsNullOrEmpty(valStr))
            {
                return; // 空值不處理
            }

            try
            {
                // 3. 智慧判斷邏輯

                // 判斷位址類型 (M/X/Y 為 Bit, D/R 為 Word)
                bool isBitDevice = Regex.IsMatch(addr, @"^[MXY][0-9]+$");
                // 簡單 Regex，可根據實際 PLC (如三菱) 調整，例如 M, X, Y, S, L, B 等

                if (isBitDevice)
                {
                    // === Bit 模式 ===
                    // 只能輸入 0 或 1
                    if (valStr != "0" && valStr != "1")
                    {
                        // 嘗試支援 true/false
                        if (valStr.ToLower() == "true" || valStr.ToLower() == "on") valStr = "1";
                        else if (valStr.ToLower() == "false" || valStr.ToLower() == "off") valStr = "0";
                        else
                        {
                            MessageBox.Show($"位址 {addr} 為 Bit 型態，數值只能輸入 0 或 1。", "格式錯誤");
                            return;
                        }
                    }
                }
                else
                {
                    // === Word/DWord 模式 ===
                    if (!int.TryParse(valStr, out int numVal))
                    {
                        MessageBox.Show("請輸入有效的整數數值。", "格式錯誤");
                        return;
                    }

                    // 邏輯：大於 32767 (Short 最大值) 視為 DWord
                    // 雖然底層 WriteAsync 是傳字串，但我們在這裡做檢查可以確保使用者知道自己在寫什麼
                    // 如果您有特定的底層指令區分 Word/DWord，可以在這裡處理
                    // 這裡我們主要做的是「防呆」和「確認」

                    if (numVal > 32767 || numVal < -32768)
                    {
                        // 數值超過 16-bit 範圍，這是一個 32-bit 寫入
                        // 在 UI 上或許可以給個提示，或者就直接通過
                        // System.Diagnostics.Debug.WriteLine("Detect DWord Write");
                    }
                }

                // 4. 執行寫入
                // 組合命令字串，例如 "D100,12345" 或 "M0,1"
                string command = $"{addr},{valStr}";

                bool success = await manager.WriteAsync(command);

                if (success)
                {
                    // 成功視覺回饋 (閃爍綠框)
                    TxtValue.BorderBrush = new SolidColorBrush(Colors.LimeGreen);
                    await System.Threading.Tasks.Task.Delay(500);
                    TxtValue.BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                }
                else
                {
                    MessageBox.Show("寫入失敗！", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"發生錯誤: {ex.Message}", "異常");
            }
        }
    }
}
