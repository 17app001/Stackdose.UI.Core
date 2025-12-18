using Stackdose.UI.Core.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Stackdose.UI.Core.Converters
{
    /// <summary>
    /// Button Theme to Brush Converter
    /// Converts ButtonTheme enum to corresponding color brush
    /// </summary>
    public class ButtonThemeToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Is this for hover state? (darker color)
        /// </summary>
        public bool IsHover { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ButtonTheme theme)
                return new SolidColorBrush(Color.FromRgb(0x60, 0x7D, 0x8B));

            var (normal, hover) = theme switch
            {
                ButtonTheme.Normal => ("#607D8B", "#455A64"),      // Blue Grey
                ButtonTheme.Primary => ("#2196F3", "#1976D2"),     // Blue
                ButtonTheme.Success => ("#4CAF50", "#388E3C"),     // Green
                ButtonTheme.Warning => ("#FF9800", "#F57C00"),     // Orange
                ButtonTheme.Error => ("#F44336", "#D32F2F"),       // Red
                ButtonTheme.Info => ("#00BCD4", "#0097A7"),        // Cyan
                _ => ("#607D8B", "#455A64")
            };

            string colorHex = IsHover ? hover : normal;
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
