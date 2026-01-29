using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Templates.Controls;

namespace Stackdose.UI.Templates.Pages
{
    /// <summary>
    /// BasePage.xaml ªº¤¬°ÊÅÞ¿è
    /// </summary>
    public partial class BasePage : UserControl
    {
        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(BasePage), 
                new PropertyMetadata("Page Title"));

        public static readonly DependencyProperty ContentAreaProperty =
            DependencyProperty.Register(nameof(ContentArea), typeof(object), typeof(BasePage), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty LogoutRequestedProperty =
            DependencyProperty.Register(nameof(LogoutRequested), typeof(RoutedEventHandler), typeof(BasePage), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty SwitchUserRequestedProperty =
            DependencyProperty.Register(nameof(SwitchUserRequested), typeof(RoutedEventHandler), typeof(BasePage), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty MinimizeRequestedProperty =
            DependencyProperty.Register(nameof(MinimizeRequested), typeof(RoutedEventHandler), typeof(BasePage), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty CloseRequestedProperty =
            DependencyProperty.Register(nameof(CloseRequested), typeof(RoutedEventHandler), typeof(BasePage), 
                new PropertyMetadata(null));

        public static readonly DependencyProperty NavigationRequestedProperty =
            DependencyProperty.Register(nameof(NavigationRequested), typeof(EventHandler<NavigationItem>), typeof(BasePage), 
                new PropertyMetadata(null));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        public object ContentArea
        {
            get => GetValue(ContentAreaProperty);
            set => SetValue(ContentAreaProperty, value);
        }

        public RoutedEventHandler LogoutRequested
        {
            get => (RoutedEventHandler)GetValue(LogoutRequestedProperty);
            set => SetValue(LogoutRequestedProperty, value);
        }

        public RoutedEventHandler SwitchUserRequested
        {
            get => (RoutedEventHandler)GetValue(SwitchUserRequestedProperty);
            set => SetValue(SwitchUserRequestedProperty, value);
        }

        public RoutedEventHandler MinimizeRequested
        {
            get => (RoutedEventHandler)GetValue(MinimizeRequestedProperty);
            set => SetValue(MinimizeRequestedProperty, value);
        }

        public RoutedEventHandler CloseRequested
        {
            get => (RoutedEventHandler)GetValue(CloseRequestedProperty);
            set => SetValue(CloseRequestedProperty, value);
        }

        public EventHandler<NavigationItem> NavigationRequested
        {
            get => (EventHandler<NavigationItem>)GetValue(NavigationRequestedProperty);
            set => SetValue(NavigationRequestedProperty, value);
        }

        public BasePage()
        {
            InitializeComponent();
        }

        private void AppHeader_LogoutClicked(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, e);
        }

        private void AppHeader_SwitchUserClicked(object sender, RoutedEventArgs e)
        {
            SwitchUserRequested?.Invoke(this, e);
        }

        private void AppHeader_MinimizeClicked(object sender, RoutedEventArgs e)
        {
            MinimizeRequested?.Invoke(this, e);
        }

        private void AppHeader_CloseClicked(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, e);
        }

        private void LeftNavigation_NavigationRequested(object sender, NavigationItem e)
        {
            NavigationRequested?.Invoke(this, e);
        }
    }
}
