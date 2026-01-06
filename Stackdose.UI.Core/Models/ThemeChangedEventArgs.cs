using System;
using System.Windows.Media;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 主題變更事件參數
    /// </summary>
    /// <remarks>
    /// 當應用程式主題切換時，會透過此事件參數傳遞主題資訊
    /// </remarks>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否為淺色主題
        /// </summary>
        public bool IsLightTheme { get; }

        /// <summary>
        /// 主題名稱
        /// </summary>
        public string ThemeName { get; }

        /// <summary>
        /// 主背景色
        /// </summary>
        public Color? BackgroundColor { get; }

        /// <summary>
        /// 主前景色
        /// </summary>
        public Color? ForegroundColor { get; }

        /// <summary>
        /// 主題變更時間
        /// </summary>
        public DateTime ChangedAt { get; }

        /// <summary>
        /// 建構函數
        /// </summary>
        /// <param name="isLightTheme">是否為淺色主題</param>
        /// <param name="themeName">主題名稱</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="foregroundColor">前景色</param>
        public ThemeChangedEventArgs(
            bool isLightTheme, 
            string themeName = "Unknown",
            Color? backgroundColor = null,
            Color? foregroundColor = null)
        {
            IsLightTheme = isLightTheme;
            ThemeName = themeName;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
            ChangedAt = DateTime.Now;
        }

        /// <summary>
        /// 取得主題描述字串
        /// </summary>
        public override string ToString()
        {
            return $"Theme: {ThemeName} ({(IsLightTheme ? "Light" : "Dark")})";
        }
    }

    /// <summary>
    /// 主題類型列舉
    /// </summary>
    public enum ThemeType
    {
        /// <summary>暗色主題（預設）</summary>
        Dark,
        
        /// <summary>亮色主題</summary>
        Light,
        
        /// <summary>高對比度主題</summary>
        HighContrast,
        
        /// <summary>自訂主題</summary>
        Custom
    }
}
