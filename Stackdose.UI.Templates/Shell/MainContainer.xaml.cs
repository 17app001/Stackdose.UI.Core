using Stackdose.UI.Templates.Controls;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Stackdose.UI.Templates.Shell
{
    public partial class MainContainer : UserControl
    {
        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(MainContainer), new PropertyMetadata("Home Overview"));

        public static readonly DependencyProperty IsShellModeProperty =
            DependencyProperty.Register(nameof(IsShellMode), typeof(bool), typeof(MainContainer), new PropertyMetadata(false));

        public static readonly DependencyProperty CurrentMachineDisplayNameProperty =
            DependencyProperty.Register(nameof(CurrentMachineDisplayName), typeof(string), typeof(MainContainer), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HeaderDeviceNameProperty =
            DependencyProperty.Register(nameof(HeaderDeviceName), typeof(string), typeof(MainContainer), new PropertyMetadata("MODEL-B"));

        public static readonly DependencyProperty MachineOptionsProperty =
            DependencyProperty.Register(nameof(MachineOptions), typeof(IEnumerable), typeof(MainContainer), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedMachineIdProperty =
            DependencyProperty.Register(nameof(SelectedMachineId), typeof(string), typeof(MainContainer), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShellContentProperty =
            DependencyProperty.Register(nameof(ShellContent), typeof(object), typeof(MainContainer), new PropertyMetadata(null));

        public event EventHandler<string>? NavigationRequested;
        public event EventHandler? LogoutRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? MinimizeRequested;
        public event EventHandler<string>? MachineSelectionRequested;

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public bool IsShellMode
        {
            get => (bool)GetValue(IsShellModeProperty);
            set => SetValue(IsShellModeProperty, value);
        }

        public string CurrentMachineDisplayName
        {
            get => (string)GetValue(CurrentMachineDisplayNameProperty);
            set => SetValue(CurrentMachineDisplayNameProperty, value);
        }

        public string HeaderDeviceName
        {
            get => (string)GetValue(HeaderDeviceNameProperty);
            set => SetValue(HeaderDeviceNameProperty, value);
        }

        public IEnumerable? MachineOptions
        {
            get => (IEnumerable?)GetValue(MachineOptionsProperty);
            set => SetValue(MachineOptionsProperty, value);
        }

        public string SelectedMachineId
        {
            get => (string)GetValue(SelectedMachineIdProperty);
            set => SetValue(SelectedMachineIdProperty, value);
        }

        public object? ShellContent
        {
            get => GetValue(ShellContentProperty);
            set => SetValue(ShellContentProperty, value);
        }

        public MainContainer()
        {
            InitializeComponent();
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

        public void SetContent(object content, string title)
        {
            ShellContent = content;
            PageTitle = title;
        }

        public void SetShellMode(bool isMultiMachineMode)
        {
            IsShellMode = isMultiMachineMode;

            if (!isMultiMachineMode)
            {
                CurrentMachineDisplayName = string.Empty;
            }
        }

        public void SetCurrentMachineDisplayName(string machineName)
        {
            CurrentMachineDisplayName = machineName;
        }

        public void SetMachineSelection(IEnumerable options, string selectedMachineId)
        {
            MachineOptions = options;
            SelectedMachineId = selectedMachineId;
        }

        public void SelectNavigationTarget(string target)
        {
            LeftNavigationControl?.SelectNavigationTarget(target);
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
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
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnNavigate(object sender, NavigationItem e)
        {
            NavigationRequested?.Invoke(this, e.NavigationTarget);
        }

        private void OnMachineSelectionChanged(object? sender, string machineId)
        {
            if (string.IsNullOrWhiteSpace(machineId))
            {
                return;
            }

            MachineSelectionRequested?.Invoke(this, machineId);
        }
    }
}
