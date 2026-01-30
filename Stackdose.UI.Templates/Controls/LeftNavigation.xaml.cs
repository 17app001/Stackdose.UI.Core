using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Templates.Controls
{
    public class NavigationItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isEnabled = true;

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string NavigationTarget { get; set; } = string.Empty;
        
        /// <summary>
        /// 需要的最低權限等級
        /// </summary>
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

        /// <summary>
        /// 根據當前使用者權限判斷是否啟用
        /// </summary>
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

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 更新啟用狀態（根據當前登入使用者權限）
        /// </summary>
        public void UpdateEnabledState()
        {
            var session = SecurityContext.CurrentSession;
            IsEnabled = session.CurrentLevel >= RequiredLevel;
        }
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

            // Default navigation items with permission requirements
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem 
                { 
                    Title = "系統總覽", 
                    Subtitle = "系統即時狀態", 
                    NavigationTarget = "MainPage",
                    RequiredLevel = AccessLevel.Operator  // L1+
                },
                new NavigationItem 
                { 
                    Title = "設備控制", 
                    Subtitle = "設備參數管理", 
                    NavigationTarget = "DeviceControlPage",
                    RequiredLevel = AccessLevel.Operator  // L1+
                },
                new NavigationItem 
                { 
                    Title = "紀錄檢視", 
                    Subtitle = "系統操作紀錄", 
                    NavigationTarget = "LogViewerPage",
                    RequiredLevel = AccessLevel.Instructor  // L2+ (指導員以上)
                },
                new NavigationItem 
                { 
                    Title = "帳號管理", 
                    Subtitle = "權限與用戶設定", 
                    NavigationTarget = "UserManagementPage",
                    RequiredLevel = AccessLevel.Admin  // L4+ (管理員以上)
                },
                new NavigationItem 
                { 
                    Title = "參數設定", 
                    Subtitle = "系統配置與調校", 
                    NavigationTarget = "SettingsPage",
                    RequiredLevel = AccessLevel.SuperAdmin  // L5 (僅超級管理員)
                }
            };

            // 訂閱登入/登出事件
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;

            // 初始化權限狀態
            UpdateNavigationItemsState();
        }

        /// <summary>
        /// 當登入成功時
        /// </summary>
        private void OnLoginSuccess(object? sender, UserAccount user)
        {
            Dispatcher.BeginInvoke(() => UpdateNavigationItemsState());
        }

        /// <summary>
        /// 當登出時
        /// </summary>
        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => UpdateNavigationItemsState());
        }

        /// <summary>
        /// 更新所有導航項目的啟用狀態
        /// </summary>
        private void UpdateNavigationItemsState()
        {
            if (NavigationItems == null) return;

            foreach (var item in NavigationItems)
            {
                item.UpdateEnabledState();
            }

            #if DEBUG
            var session = SecurityContext.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"[LeftNavigation] Updated navigation items for user: {session.CurrentUserName} (Level: {session.CurrentLevel})");
            #endif
        }

        private void NavigationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is NavigationItem item)
            {
                // 檢查是否啟用
                if (!item.IsEnabled)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LeftNavigation] Navigation blocked: {item.Title} (requires {item.RequiredLevel})");
                    #endif
                    
                    // 顯示權限不足提示
                    Stackdose.UI.Core.Controls.CyberMessageBox.Show(
                        $"您沒有權限存取「{item.Title}」功能\n需要 {item.RequiredLevel} 或更高權限",
                        "權限不足",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

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
