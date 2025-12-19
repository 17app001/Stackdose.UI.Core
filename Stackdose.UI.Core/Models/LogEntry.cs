// 放在 Stackdose.UI.Core/Models/LogEntry.cs
using System;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.UI.Core.Models
{
    public enum LogLevel { Info, Warning, Error, Success }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public LogLevel Level { get; set; }

        // 方便 UI 顯示顏色的屬性
        public SolidColorBrush Color
        {
            get
            {
                // 🔥 根據主題動態調整顏色
                bool isLightMode = IsLightTheme();
                
                return Level switch
                {
                    LogLevel.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36)), // #F44336 紅色
                    LogLevel.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00)), // #FF9800 橙色
                    LogLevel.Success => isLightMode 
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32)) // #2E7D32 深綠（Light）
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0xBB, 0x6A)), // #66BB6A 淺綠（Dark）
                    _ => isLightMode 
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x42, 0x42, 0x42)) // #424242 深灰（Light）
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xBD, 0xBD, 0xBD))  // #BDBDBD 淺灰（Dark）
                };
            }
        }

        public string TimeStr => Timestamp.ToString("HH:mm:ss.f"); // 顯示到毫秒

        /// <summary>
        /// 判斷當前是否為 Light 主題
        /// </summary>
        private bool IsLightTheme()
        {
            try
            {
                var plcBgBrush = Application.Current?.TryFindResource("Plc.Bg.Main") as SolidColorBrush;
                if (plcBgBrush != null)
                {
                    var bgColor = plcBgBrush.Color;
                    bool isLight = bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LogEntry] IsLightTheme: {isLight}, Plc.Bg.Main={bgColor}");
                    #endif
                    
                    return isLight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogEntry] Error: {ex.Message}");
            }
            return false; // 預設 Dark 模式
        }
    }
}