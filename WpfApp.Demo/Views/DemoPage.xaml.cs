using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Templates.Controls;

namespace WpfApp.Demo.Views
{
    public partial class DemoPage : UserControl
    {
        public DemoPage()
        {
            InitializeComponent();
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Logout clicked!", "Demo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to close the application?",
                "Close Application",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Close();
                }
            }
        }

        private void OnNavigate(object sender, NavigationItem e)
        {
            MessageBox.Show($"Navigate to: {e.Title}", "Demo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Demo button clicked!", "Demo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
