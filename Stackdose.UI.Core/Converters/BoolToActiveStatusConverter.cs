using System;
using System.Globalization;
using System.Windows.Data;

namespace Stackdose.UI.Core.Converters
{
    /// <summary>
    /// 布林值到活躍狀態轉換器
    /// </summary>
    public class BoolToActiveStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "活躍" : "停用";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}