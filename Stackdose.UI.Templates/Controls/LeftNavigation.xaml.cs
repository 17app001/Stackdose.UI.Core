using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Controls
{
    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string NavigationTarget { get; set; } = string.Empty;
    }

    /// <summary>
    /// LeftNavigation.xaml ªº¤¬°ÊÅÞ¿è
    /// </summary>
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
            
            // Default navigation items
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem { Title = "Home Overview", Subtitle = "Home Dashboard", NavigationTarget = "HomePage" },
                new NavigationItem { Title = "Process Management", Subtitle = "Process Management", NavigationTarget = "MachinePage" },
                new NavigationItem { Title = "Log Viewer", Subtitle = "System Logs", NavigationTarget = "LogViewerPage" },
                new NavigationItem { Title = "User Management", Subtitle = "Access Control", NavigationTarget = "UserManagementPage" },
                new NavigationItem { Title = "System Settings", Subtitle = "Configuration", NavigationTarget = "SettingsPage" }
            };
        }

        private void NavigationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is NavigationItem item)
            {
                // Deselect all items
                foreach (var navItem in NavigationItems)
                {
                    navItem.IsSelected = false;
                }
                
                // Select clicked item
                item.IsSelected = true;
                
                // Trigger navigation event
                NavigationRequested?.Invoke(this, item);
            }
        }
    }
}
