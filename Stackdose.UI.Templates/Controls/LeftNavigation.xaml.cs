using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Controls
{
    public class NavigationItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isEnabled = true;

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string NavigationTarget { get; set; } = string.Empty;
        public AccessLevel RequiredLevel { get; set; } = AccessLevel.Operator;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateEnabledState()
        {
            var session = SecurityContext.CurrentSession;
            IsEnabled = session.CurrentLevel >= RequiredLevel;
        }
    }

    public partial class LeftNavigation : UserControl
    {
        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(ObservableCollection<NavigationItem>),
                typeof(LeftNavigation), new PropertyMetadata(null));

        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => (ObservableCollection<NavigationItem>)GetValue(NavigationItemsProperty);
            set => SetValue(NavigationItemsProperty, value);
        }

        public event EventHandler<NavigationItem>? NavigationRequested;

        public LeftNavigation()
        {
            InitializeComponent();

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Title = "System Overview", NavigationTarget = "MainPage", RequiredLevel = AccessLevel.Operator },
                new NavigationItem { Title = "Device Control", NavigationTarget = "DeviceControlPage", RequiredLevel = AccessLevel.Operator },
                new NavigationItem { Title = "Log Viewer", NavigationTarget = "LogViewerPage", RequiredLevel = AccessLevel.Instructor },
                new NavigationItem { Title = "User Management", NavigationTarget = "UserManagementPage", RequiredLevel = AccessLevel.Admin },
                new NavigationItem { Title = "Settings", NavigationTarget = "SettingsPage", RequiredLevel = AccessLevel.SuperAdmin }
            };

            Loaded += LeftNavigation_Loaded;
            Unloaded += LeftNavigation_Unloaded;
        }

        private void LeftNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;
            UpdateNavigationItemsState();
        }

        private void LeftNavigation_Unloaded(object sender, RoutedEventArgs e)
        {
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;
        }

        private void OnLoginSuccess(object? sender, UserAccount user)
        {
            Dispatcher.BeginInvoke(UpdateNavigationItemsState);
        }

        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateNavigationItemsState);
        }

        private void UpdateNavigationItemsState()
        {
            if (NavigationItems == null)
            {
                return;
            }

            foreach (var item in NavigationItems)
            {
                item.UpdateEnabledState();
            }
        }

        private void NavigationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not NavigationItem item)
            {
                return;
            }

            if (!item.IsEnabled)
            {
                CyberMessageBox.Show(
                    $"Access Denied\n\nYou don't have permission to access \"{item.Title}\"\nRequired: {item.RequiredLevel} or higher",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            foreach (var navItem in NavigationItems)
            {
                navItem.IsSelected = false;
            }

            item.IsSelected = true;
            NavigationRequested?.Invoke(this, item);
        }
    }
}
