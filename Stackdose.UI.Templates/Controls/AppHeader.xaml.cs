using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Controls
{
    /// <summary>
    /// AppHeader.xaml ªº¤¬°ÊÅÞ¿è
    /// </summary>
    public partial class AppHeader : UserControl
    {
        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(AppHeader), 
                new PropertyMetadata("Page Title"));

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(AppHeader), 
                new PropertyMetadata("Admin"));

        public static readonly DependencyProperty UserRoleProperty =
            DependencyProperty.Register(nameof(UserRole), typeof(string), typeof(AppHeader), 
                new PropertyMetadata("Administrator"));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        public string UserRole
        {
            get => (string)GetValue(UserRoleProperty);
            set => SetValue(UserRoleProperty, value);
        }

        public event RoutedEventHandler? LogoutClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? SwitchUserClicked;

        public AppHeader()
        {
            InitializeComponent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutClicked?.Invoke(this, e);
        }

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchUserClicked?.Invoke(this, e);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeClicked?.Invoke(this, e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, e);
        }
    }
}
