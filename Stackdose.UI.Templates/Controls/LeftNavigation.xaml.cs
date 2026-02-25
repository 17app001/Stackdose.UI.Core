using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private bool _suppressNavigationItemsCallback;
        private bool _useExternalNavigationItems;

        public static readonly DependencyProperty IsMultiMachineModeProperty =
            DependencyProperty.Register(nameof(IsMultiMachineMode), typeof(bool),
                typeof(LeftNavigation), new PropertyMetadata(false, OnIsMultiMachineModeChanged));

        public static readonly DependencyProperty NavigationItemsProperty =
            DependencyProperty.Register(nameof(NavigationItems), typeof(ObservableCollection<NavigationItem>),
                typeof(LeftNavigation), new PropertyMetadata(null, OnNavigationItemsChanged));

        public static readonly DependencyProperty NavigationCommandProperty =
            DependencyProperty.Register(nameof(NavigationCommand), typeof(ICommand),
                typeof(LeftNavigation), new PropertyMetadata(null));

        public bool IsMultiMachineMode
        {
            get => (bool)GetValue(IsMultiMachineModeProperty);
            set => SetValue(IsMultiMachineModeProperty, value);
        }

        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => (ObservableCollection<NavigationItem>)GetValue(NavigationItemsProperty);
            set => SetValue(NavigationItemsProperty, value);
        }

        public ICommand? NavigationCommand
        {
            get => (ICommand?)GetValue(NavigationCommandProperty);
            set => SetValue(NavigationCommandProperty, value);
        }

        public event EventHandler<NavigationItem>? NavigationRequested;

        public LeftNavigation()
        {
            InitializeComponent();

            _suppressNavigationItemsCallback = true;
            NavigationItems = new ObservableCollection<NavigationItem>();
            _suppressNavigationItemsCallback = false;
            RebuildNavigationItems();

            Loaded += LeftNavigation_Loaded;
            Unloaded += LeftNavigation_Unloaded;
        }

        private static void OnIsMultiMachineModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LeftNavigation navigation)
            {
                if (navigation._useExternalNavigationItems)
                {
                    return;
                }

                navigation.RebuildNavigationItems();
            }
        }

        private static void OnNavigationItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LeftNavigation navigation || navigation._suppressNavigationItemsCallback)
            {
                return;
            }

            navigation._useExternalNavigationItems = e.NewValue is ObservableCollection<NavigationItem>;

            if (navigation._useExternalNavigationItems)
            {
                navigation.UpdateNavigationItemsState();
                return;
            }

            navigation.RebuildNavigationItems();
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

        private void RebuildNavigationItems()
        {
            if (_useExternalNavigationItems)
            {
                return;
            }

            var selectedTarget = NavigationItems?.FirstOrDefault(item => item.IsSelected)?.NavigationTarget;

            var newItems = new List<NavigationItem>();

            if (IsMultiMachineMode)
            {
                newItems.Add(new NavigationItem
                {
                    Title = "Machine Overview",
                    NavigationTarget = "MachineOverviewPage",
                    RequiredLevel = AccessLevel.Operator
                });
            }

            newItems.AddRange(
            [
                new NavigationItem { Title = "Machine Detail", NavigationTarget = "MachineDetailPage", RequiredLevel = AccessLevel.Operator },
                new NavigationItem { Title = "Log Viewer", NavigationTarget = "LogViewerPage", RequiredLevel = AccessLevel.Instructor },
                new NavigationItem { Title = "User Management", NavigationTarget = "UserManagementPage", RequiredLevel = AccessLevel.Admin },
                new NavigationItem { Title = "Maintenance Mode", NavigationTarget = "SettingsPage", RequiredLevel = AccessLevel.SuperAdmin }
            ]);

            _suppressNavigationItemsCallback = true;
            NavigationItems = new ObservableCollection<NavigationItem>(newItems);
            _suppressNavigationItemsCallback = false;

            if (!string.IsNullOrWhiteSpace(selectedTarget))
            {
                var selectedItem = NavigationItems.FirstOrDefault(item => item.NavigationTarget == selectedTarget);
                if (selectedItem is not null)
                {
                    selectedItem.IsSelected = true;
                }
            }

            UpdateNavigationItemsState();
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

            if (NavigationCommand != null && NavigationCommand.CanExecute(item.NavigationTarget))
            {
                NavigationCommand.Execute(item.NavigationTarget);
                return;
            }

            NavigationRequested?.Invoke(this, item);
        }

        public void SelectNavigationTarget(string navigationTarget)
        {
            if (NavigationItems == null || string.IsNullOrWhiteSpace(navigationTarget))
            {
                return;
            }

            var targetItem = NavigationItems.FirstOrDefault(item =>
                string.Equals(item.NavigationTarget, navigationTarget, StringComparison.OrdinalIgnoreCase));

            if (targetItem == null)
            {
                return;
            }

            foreach (var navItem in NavigationItems)
            {
                navItem.IsSelected = false;
            }

            targetItem.IsSelected = true;
        }
    }
}
