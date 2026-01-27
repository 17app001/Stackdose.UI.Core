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
    /// LeftNavigation.xaml 的互動邏輯
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
                new NavigationItem { Title = "首頁總覽", Subtitle = "系統即時看板", NavigationTarget = "HomePage" },
                new NavigationItem { Title = "設備控制", Subtitle = "機台與生產管理", NavigationTarget = "MachinePage" },
                new NavigationItem { Title = "運行日誌", Subtitle = "系統活動紀錄", NavigationTarget = "LogViewerPage" },
                new NavigationItem { Title = "帳戶管理", Subtitle = "權限與存取控制", NavigationTarget = "UserManagementPage" },
                new NavigationItem { Title = "參數設定", Subtitle = "系統配置與偏好", NavigationTarget = "SettingsPage" }
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
