using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Stackdose.UI.Templates.Controls
{
    public partial class AppHeader : UserControl, INotifyPropertyChanged
    {
        private bool _isFullscreen = true;

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(AppHeader),
                new PropertyMetadata("Page Title"));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public static readonly DependencyProperty ShowMachineBadgeProperty =
            DependencyProperty.Register(nameof(ShowMachineBadge), typeof(bool), typeof(AppHeader),
                new PropertyMetadata(false));

        public bool ShowMachineBadge
        {
            get => (bool)GetValue(ShowMachineBadgeProperty);
            set => SetValue(ShowMachineBadgeProperty, value);
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register(nameof(DeviceName), typeof(string), typeof(AppHeader),
                new PropertyMetadata("MODEL-B"));

        public string DeviceName
        {
            get => (string)GetValue(DeviceNameProperty);
            set => SetValue(DeviceNameProperty, value);
        }

        public static readonly DependencyProperty MachineDisplayNameProperty =
            DependencyProperty.Register(nameof(MachineDisplayName), typeof(string), typeof(AppHeader),
                new PropertyMetadata(string.Empty));

        public string MachineDisplayName
        {
            get => (string)GetValue(MachineDisplayNameProperty);
            set => SetValue(MachineDisplayNameProperty, value);
        }

        private string _userName = "Guest";
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _userId = string.Empty;
        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }

        private string _userRole = "Not Logged In";
        public string UserRole
        {
            get => _userRole;
            set
            {
                _userRole = value;
                OnPropertyChanged();
            }
        }

        public event RoutedEventHandler? LogoutClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? SwitchUserClicked;
        public event RoutedEventHandler? FullscreenClicked;

        public AppHeader()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += AppHeader_Loaded;
            Unloaded += AppHeader_Unloaded;
        }

        private void AppHeader_Loaded(object sender, RoutedEventArgs e)
        {
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;
            UpdateUserInfo();
        }

        private void AppHeader_Unloaded(object sender, RoutedEventArgs e)
        {
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;
        }

        private void OnLoginSuccess(object? sender, UserAccount user)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;

            if (session.IsLoggedIn && session.CurrentUser != null)
            {
                UserName = session.CurrentUser.DisplayName ?? session.CurrentUserName;
                UserId = session.CurrentUser.UserId ?? string.Empty;
                UserRole = session.CurrentLevel.ToString();
                return;
            }

            UserName = "Guest";
            UserId = string.Empty;
            UserRole = "Not Logged In";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = CyberMessageBox.Show(
                    "確定要登出嗎？",
                    "登出確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                SecurityContext.Logout();
                LogoutClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show(
                    $"登出失敗: {ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = CyberMessageBox.Show(
                    "確定要切換帳號嗎？",
                    "切換帳號確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                var dialog = new LoginDialog
                {
                    Owner = Window.GetWindow(this),
                    Title = "切換使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    SwitchUserClicked?.Invoke(this, e);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show(
                    $"切換使用者失敗: {ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window == null)
                {
                    return;
                }

                window.WindowState = WindowState.Minimized;
                MinimizeClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem($"[AppHeader] Minimize failed: {ex.Message}", LogLevel.Error, showInUi: false);
            }
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window == null)
                {
                    return;
                }

                if (_isFullscreen)
                {
                    window.WindowState = WindowState.Normal;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        window.WindowStyle = WindowStyle.SingleBorderWindow;
                        window.ResizeMode = ResizeMode.CanResize;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    _isFullscreen = false;
                    UpdateFullscreenIcon(sender as Button, isFullscreen: false);
                }
                else
                {
                    window.WindowStyle = WindowStyle.None;
                    window.ResizeMode = ResizeMode.NoResize;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        window.WindowState = WindowState.Maximized;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    _isFullscreen = true;
                    UpdateFullscreenIcon(sender as Button, isFullscreen: true);
                }

                FullscreenClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem($"[AppHeader] Fullscreen toggle failed: {ex.Message}", LogLevel.Error, showInUi: false);
                CyberMessageBox.Show(
                    $"Full screen toggle failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void UpdateFullscreenIcon(Button? button, bool isFullscreen)
        {
            if (button?.Content is not Path path)
            {
                return;
            }

            if (isFullscreen)
            {
                path.Data = Geometry.Parse("F0 M2,5 H11 V14 H2 Z M4,7 V12 H9 V7 Z M7,2 H16 V11 H7 Z M9,4 V9 H14 V4 Z");
                button.ToolTip = "Exit Fullscreen";
            }
            else
            {
                path.Data = Geometry.Parse("F0 M3,3 H15 V15 H3 Z M5,5 V13 H13 V5 Z");
                button.ToolTip = "Enter Fullscreen";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = CyberMessageBox.Show(
                    "確定要關閉程式嗎？",
                    "關閉確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                Window.GetWindow(this)?.Close();
                CloseClicked?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem($"[AppHeader] Close failed: {ex.Message}", LogLevel.Error, showInUi: false);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
