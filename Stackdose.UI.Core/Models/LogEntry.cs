// 放在 Stackdose.UI.Core/Models/LogEntry.cs
using System;
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
        public SolidColorBrush Color => Level switch
        {
            LogLevel.Error => Brushes.Red,
            LogLevel.Warning => Brushes.Orange,
            LogLevel.Success => Brushes.LightGreen,
            _ => Brushes.LightGray
        };

        public string TimeStr => Timestamp.ToString("HH:mm:ss.f"); // 顯示到毫秒
    }
}