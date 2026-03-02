using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Stackdose.UI.Templates.Shell;

public partial class SinglePageContainer : UserControl
{
    public static readonly DependencyProperty PageTitleProperty =
        DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(SinglePageContainer), new PropertyMetadata("Single Page"));

    public static readonly DependencyProperty HeaderDeviceNameProperty =
        DependencyProperty.Register(nameof(HeaderDeviceName), typeof(string), typeof(SinglePageContainer), new PropertyMetadata("MODEL"));

    public static readonly DependencyProperty ShellContentProperty =
        DependencyProperty.Register(nameof(ShellContent), typeof(object), typeof(SinglePageContainer), new PropertyMetadata(null));

    public static readonly DependencyProperty LogoutCommandProperty =
        DependencyProperty.Register(nameof(LogoutCommand), typeof(ICommand), typeof(SinglePageContainer), new PropertyMetadata(null));

    public static readonly DependencyProperty MinimizeCommandProperty =
        DependencyProperty.Register(nameof(MinimizeCommand), typeof(ICommand), typeof(SinglePageContainer), new PropertyMetadata(null));

    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(SinglePageContainer), new PropertyMetadata(null));

    public event EventHandler? LogoutRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? MinimizeRequested;

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    public string HeaderDeviceName
    {
        get => (string)GetValue(HeaderDeviceNameProperty);
        set => SetValue(HeaderDeviceNameProperty, value);
    }

    public object? ShellContent
    {
        get => GetValue(ShellContentProperty);
        set => SetValue(ShellContentProperty, value);
    }

    public ICommand? LogoutCommand
    {
        get => (ICommand?)GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }

    public ICommand? MinimizeCommand
    {
        get => (ICommand?)GetValue(MinimizeCommandProperty);
        set => SetValue(MinimizeCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public SinglePageContainer()
    {
        InitializeComponent();
    }

    public void SetContent(object content, string title)
    {
        ShellContent = content;
        PageTitle = title;
    }

    private void Header_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (IsFromInteractiveHeaderElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            Window.GetWindow(this)?.DragMove();
        }
    }

    private static bool IsFromInteractiveHeaderElement(DependencyObject? source)
    {
        if (source == null)
        {
            return false;
        }

        DependencyObject? current = source;
        while (current != null)
        {
            if (current is ButtonBase
                || current is ComboBox
                || current is ComboBoxItem
                || current is TextBox
                || current is PasswordBox
                || current is Selector)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void OnLogout(object sender, RoutedEventArgs e)
    {
        if (TryExecuteCommand(LogoutCommand))
        {
            return;
        }

        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        if (TryExecuteCommand(MinimizeCommand))
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
            return;
        }

        MinimizeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        if (TryExecuteCommand(CloseCommand))
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static bool TryExecuteCommand(ICommand? command, object? parameter = null)
    {
        if (command == null || !command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }
}
