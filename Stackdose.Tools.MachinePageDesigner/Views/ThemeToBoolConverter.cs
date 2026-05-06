using System;
using System.Globalization;
using System.Windows.Data;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>
/// 將 "Light" 主題映射為 True (Checked)，"Dark" 映射為 False (Unchecked)。
/// </summary>
public sealed class ThemeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s && s == "Light";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "Light" : "Dark";
    }
}
