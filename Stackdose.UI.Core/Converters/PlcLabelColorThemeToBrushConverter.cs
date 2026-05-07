using Stackdose.UI.Core.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Stackdose.UI.Core.Converters
{
    /// <summary>
    /// PlcLabel 顏色主題 轉 Brush 轉換器
    /// </summary>
    public class PlcLabelColorThemeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlcLabelColorTheme theme)
            {
                // 特殊處理：DarkBlue 會根據主題自動調整
                if (theme == PlcLabelColorTheme.DarkBlue)
                {
                    // 改為直接使用資源引用，讓 XAML 層級處理動態更新更穩定
                    return Application.Current.TryFindResource("Surface.Bg.Card") as Brush 
                           ?? Application.Current.TryFindResource("Plc.Bg.Main") as Brush 
                           ?? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
                }

                bool isLightMode = IsLightTheme();

                // 在 Light 模式下調整 Neon 顏色，使其更易讀（稍微加深）
                if (isLightMode)
                {
                    if (theme == PlcLabelColorTheme.NeonBlue) return new SolidColorBrush(Color.FromRgb(0x00, 0x70, 0xC0)); // 更深的深天藍
                    if (theme == PlcLabelColorTheme.NeonGreen) return new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // 深綠
                    if (theme == PlcLabelColorTheme.NeonRed) return new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); // 深紅
                    if (theme == PlcLabelColorTheme.White) return new SolidColorBrush(Color.FromRgb(0x26, 0x32, 0x38)); // White 在淺色模式下應為深色
                }

                return theme switch
                {
                    PlcLabelColorTheme.Default => Application.Current.TryFindResource("Plc.Text.Label") as Brush ?? new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                    PlcLabelColorTheme.Primary => Application.Current.TryFindResource("Action.Primary") as Brush ?? Brushes.Blue,
                    PlcLabelColorTheme.Success => Application.Current.TryFindResource("Action.Success") as Brush ?? Brushes.Green,
                    PlcLabelColorTheme.Warning => Application.Current.TryFindResource("Action.Warning") as Brush ?? Brushes.Orange,
                    PlcLabelColorTheme.Error => Application.Current.TryFindResource("Action.Error") as Brush ?? Brushes.Red,
                    PlcLabelColorTheme.Info => Application.Current.TryFindResource("Action.Info") as Brush ?? Brushes.Cyan,
                    PlcLabelColorTheme.NeonBlue => Application.Current.TryFindResource("Cyber.NeonBlue") as Brush ?? new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xFF)),
                    PlcLabelColorTheme.NeonRed => Application.Current.TryFindResource("Cyber.NeonRed") as Brush ?? new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0x33)),
                    PlcLabelColorTheme.NeonGreen => Application.Current.TryFindResource("Cyber.NeonGreen") as Brush ?? new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88)),
                    PlcLabelColorTheme.White => Application.Current.TryFindResource("Cyber.Text.Bright") as Brush ?? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                    PlcLabelColorTheme.Gray => Application.Current.TryFindResource("Text.Tertiary") as Brush ?? new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                    _ => Application.Current.TryFindResource("Plc.Text.Label") as Brush ?? new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判斷當前是否為 Light 主題（使用懶惰檢測回退）
        /// </summary>
        private bool IsLightTheme()
        {
            try
            {
                // 方法1: 檢查 Plc.Bg.Main
                var plcBgBrush = Application.Current.TryFindResource("Plc.Bg.Main") as SolidColorBrush;
                if (plcBgBrush != null)
                {
                    var bgColor = plcBgBrush.Color;
                    if (bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200)
                    {
                        return true; // 背景很亮 = Light 模式
                    }
                }

                // 方法2: 檢查 Cyber.Bg.Dark
                var cyberBgBrush = Application.Current.TryFindResource("Cyber.Bg.Dark") as SolidColorBrush;
                if (cyberBgBrush != null)
                {
                    var bgColor = cyberBgBrush.Color;
                    if (bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200)
                    {
                        return true; // 背景很亮 = Light 模式
                    }
                }

                // 方法3: 檢查 Cyber.Text.Main
                var textBrush = Application.Current.TryFindResource("Cyber.Text.Main") as SolidColorBrush;
                if (textBrush != null)
                {
                    var textColor = textBrush.Color;
                    // 如果文字是深色（Dark 模式下文字是亮色）
                    if (textColor.R < 100 && textColor.G < 100 && textColor.B < 100)
                    {
                        return true; // 深色文字 = Light 模式
                    }
                }
            }
            catch
            {
                // 發生錯誤時預設為 Dark 模式
            }

            return false; // 預設 Dark 模式
        }
    }
}
