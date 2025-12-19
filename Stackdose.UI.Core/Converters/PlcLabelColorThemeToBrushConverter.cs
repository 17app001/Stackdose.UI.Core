using Stackdose.UI.Core.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Stackdose.UI.Core.Converters
{
    /// <summary>
    /// PlcLabel 顏色主題轉 Brush 轉換器
    /// </summary>
    public class PlcLabelColorThemeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlcLabelColorTheme theme)
            {
                // ?? 特殊處理：DarkBlue 根據主題自動調整
                if (theme == PlcLabelColorTheme.DarkBlue)
                {
                    // 多重檢測方式判斷主題
                    bool isLightMode = IsLightTheme();
                    
                    if (isLightMode)
                    {
                        // Light 模式：使用淺灰色
                        return new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)); // #F5F5F5 淺灰
                    }
                    else
                    {
                        // Dark 模式：使用深藍色
                        return new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E)); // #1E1E2E
                    }
                }

                // ?? NeonBlue 作為底框時，Light 模式下改為白色
                if (theme == PlcLabelColorTheme.NeonBlue && targetType == typeof(Brush))
                {
                    bool isLightMode = IsLightTheme();
                    if (isLightMode)
                    {
                        // Light 模式下 NeonBlue 底框改為白色
                        return new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)); // #FFFFFF 白色
                    }
                }
                
                return theme switch
                {
                    PlcLabelColorTheme.Default => Application.Current.TryFindResource("Plc.Text.Label") as Brush ?? Brushes.Gray,
                    PlcLabelColorTheme.Primary => Application.Current.TryFindResource("Button.Bg.Primary") as Brush ?? Brushes.Blue,
                    PlcLabelColorTheme.Success => Application.Current.TryFindResource("Status.Success") as Brush ?? Brushes.Green,
                    PlcLabelColorTheme.Warning => Application.Current.TryFindResource("Status.Warning") as Brush ?? Brushes.Orange,
                    PlcLabelColorTheme.Error => Application.Current.TryFindResource("Status.Error") as Brush ?? Brushes.Red,
                    PlcLabelColorTheme.Info => Application.Current.TryFindResource("Status.Info") as Brush ?? Brushes.Cyan,
                    PlcLabelColorTheme.NeonBlue => Application.Current.TryFindResource("Cyber.NeonBlue") as Brush ?? Brushes.Cyan,
                    PlcLabelColorTheme.White => Application.Current.TryFindResource("Cyber.Text.Bright") as Brush ?? Brushes.White,
                    PlcLabelColorTheme.Gray => Application.Current.TryFindResource("Plc.Text.Gray") as Brush ?? Brushes.Gray,
                    _ => Application.Current.TryFindResource("Plc.Text.Label") as Brush ?? Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判斷當前是否為 Light 主題（多重檢測）
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
                        return true; // 亮色背景 = Light 模式
                    }
                }

                // 方法2: 檢查 Cyber.Bg.Dark
                var cyberBgBrush = Application.Current.TryFindResource("Cyber.Bg.Dark") as SolidColorBrush;
                if (cyberBgBrush != null)
                {
                    var bgColor = cyberBgBrush.Color;
                    if (bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200)
                    {
                        return true; // 亮色背景 = Light 模式
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
