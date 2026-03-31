using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Stackdose.Tools.ProjectGeneratorUI;

/// <summary>bool → Visibility (True=Visible, False=Collapsed)</summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public static readonly BooleanToVisibilityConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility.Visible;
}

/// <summary>bool → short text for panel summary (returns empty string when false)</summary>
public sealed class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter Maintenance = new("Maintenance ");
    public static readonly BoolToTextConverter Settings    = new("Settings ");
    public static readonly BoolToTextConverter PlcEditor   = new("PlcDeviceEditor ");

    private readonly string _text;
    private BoolToTextConverter(string text) => _text = text;

    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is true ? _text : string.Empty;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        throw new NotImplementedException();
}

/// <summary>string == parameter → true/false (for RadioButton ↔ string LayoutMode binding)</summary>
public sealed class StringEqualityConverter : IValueConverter
{
    public static readonly StringEqualityConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v?.ToString() == p?.ToString();
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is true ? p : Binding.DoNothing;
}

/// <summary>bool → inverted bool (for IsEnabled binding)</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public static readonly InvertBoolConverter Instance = new();
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is bool b ? !b : v;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is bool b ? !b : v;
}
