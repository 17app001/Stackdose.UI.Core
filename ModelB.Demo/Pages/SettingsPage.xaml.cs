using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace ModelB.Demo.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void SavePlcSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement PLC settings save logic
            ComplianceContext.LogSystem("PLC settings saved", LogLevel.Success);
            MessageBox.Show("PLC settings saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement preferences save logic
            ComplianceContext.LogSystem("System preferences saved", LogLevel.Success);
            MessageBox.Show("System preferences saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
