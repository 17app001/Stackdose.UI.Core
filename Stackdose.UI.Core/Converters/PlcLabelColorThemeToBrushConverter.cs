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
                    PlcLabelColorTheme.DarkBlue => new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E)), // #1E1E2E
                    _ => Application.Current.TryFindResource("Plc.Text.Label") as Brush ?? Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
