using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Stackdose.App.DesignPlayer.Controls;

public partial class DashboardTopBar : UserControl
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    public DashboardTopBar()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        Loaded   += (_, _) => { ClockText.Text = DateTime.Now.ToString("HH:mm:ss"); _clock.Start(); };
        Unloaded += (_, _) => _clock.Stop();
    }

    public string DeviceName
    {
        get => DeviceNameText.Text;
        set => DeviceNameText.Text = value;
    }

    private void OnDragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;

    private void OnClose(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)?.Close();
}
