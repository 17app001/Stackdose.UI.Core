using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>Bool true → Collapsed，false → Visible</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>
/// Attached property：當 IsFocused 設為 true 時，呼叫 Keyboard.Focus 讓控制項獲得焦點。
/// 主要用於頁籤重命名時讓 TextBox 自動獲得焦點。
/// </summary>
public static class FocusHelper
{
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused", typeof(bool), typeof(FocusHelper),
            new PropertyMetadata(false, OnIsFocusedChanged));

    public static bool GetIsFocused(DependencyObject obj)
        => (bool)obj.GetValue(IsFocusedProperty);

    public static void SetIsFocused(DependencyObject obj, bool value)
        => obj.SetValue(IsFocusedProperty, value);

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is true)
        {
            element.Dispatcher.BeginInvoke(() =>
            {
                element.Focus();
                if (element is System.Windows.Controls.TextBox tb)
                    tb.SelectAll();
            }, DispatcherPriority.Input);
        }
    }
}
