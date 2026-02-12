using Stackdose.UI.Templates.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Stackdose.UI.Templates.Shell
{
    public partial class MainContainer : UserControl
    {
        public event EventHandler<string>? NavigationRequested;
        public event EventHandler? LogoutRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler? MinimizeRequested;

        public MainContainer()
        {
            InitializeComponent();
        }

        private void Header_DragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                Window.GetWindow(this)?.DragMove();
            }
        }

        public void SetContent(object content, string title)
        {
            ContentArea.Content = content;
            AppHeaderControl.PageTitle = title;
        }

        public void SetShellMode(bool isMultiMachineMode)
        {
            LeftNavigationControl.IsMultiMachineMode = isMultiMachineMode;
            AppHeaderControl.ShowMachineBadge = isMultiMachineMode;

            if (!isMultiMachineMode)
            {
                AppHeaderControl.MachineDisplayName = string.Empty;
            }
        }

        public void SetCurrentMachineDisplayName(string machineName)
        {
            AppHeaderControl.MachineDisplayName = machineName;
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
    }
}
