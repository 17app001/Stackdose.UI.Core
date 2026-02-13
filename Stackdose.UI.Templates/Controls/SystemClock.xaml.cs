using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Stackdose.UI.Templates.Controls;

public partial class SystemClock : UserControl
{
    private readonly DispatcherTimer _timer;

    public static readonly DependencyProperty TimeFontSizeProperty =
        DependencyProperty.Register(nameof(TimeFontSize), typeof(double), typeof(SystemClock), new PropertyMetadata(28d));

    public static readonly DependencyProperty DateFontSizeProperty =
        DependencyProperty.Register(nameof(DateFontSize), typeof(double), typeof(SystemClock), new PropertyMetadata(12d));

    public static readonly DependencyProperty DateFormatProperty =
        DependencyProperty.Register(nameof(DateFormat), typeof(string), typeof(SystemClock), new PropertyMetadata("yyyy-MM-dd ddd"));

    public static readonly DependencyProperty TimeFormatProperty =
        DependencyProperty.Register(nameof(TimeFormat), typeof(string), typeof(SystemClock), new PropertyMetadata("HH:mm:ss"));

    public double TimeFontSize
    {
        get => (double)GetValue(TimeFontSizeProperty);
        set => SetValue(TimeFontSizeProperty, value);
    }

    public double DateFontSize
    {
        get => (double)GetValue(DateFontSizeProperty);
        set => SetValue(DateFontSizeProperty, value);
    }

    public string DateFormat
    {
        get => (string)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public string TimeFormat
    {
        get => (string)GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    public SystemClock()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => RefreshTime();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshTime();
        _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void RefreshTime()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString(TimeFormat);
        DateText.Text = now.ToString(DateFormat);
    }
}
