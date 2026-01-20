using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Examples;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Models;

namespace WpfApp1.Panels
{
    /// <summary>
    /// System testing panel (for development)
    /// </summary>
    /// <remarks>
    /// Provides:
    /// <list type="bullet">
    /// <item>Process control buttons</item>
    /// <item>MessageBox thread safety testing</item>
    /// <item>Theme management testing</item>
    /// </list>
    /// </remarks>
    public partial class SystemTestPanel : UserControl
    {
        public SystemTestPanel()
        {
            InitializeComponent();
        }

        #region Process Control

        private void StartProcess_Click(object sender, RoutedEventArgs e)
        {
            // Get MainWindow to access its method
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Call MainWindow's StartProcess_Click method
                mainWindow.StartProcess_Click(sender, e);
            }
        }

        #endregion

        #region Theme Testing

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ToggleTheme_Click(sender, e);
            }
        }

        private void ShowThemeStats_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ShowThemeStats_Click(sender, e);
            }
        }

        private void OpenThemeDemo_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.OpenThemeDemo_Click(sender, e);
            }
        }

        private void PrintRegistered_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.PrintRegistered_Click(sender, e);
            }
        }

        private void CleanupTheme_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.CleanupTheme_Click(sender, e);
            }
        }

        #endregion
    }
}
