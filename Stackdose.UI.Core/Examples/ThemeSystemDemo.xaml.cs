using Stackdose.UI.Core.Helpers.UI;
using System;
using System.Windows;

namespace Stackdose.UI.Core.Examples
{
    /// <summary>
    /// Theme System Demo - Batch B Verification
    /// </summary>
    /// <remarks>
    /// This demo shows how to use the new theme system:
    /// 1. Theme switching with ThemeLoader
    /// 2. Semantic tokens usage
    /// 3. Backward compatibility verification
    /// </remarks>
    public partial class ThemeSystemDemo : Window
    {
        public ThemeSystemDemo()
        {
            InitializeComponent();
            
            // Preload themes for better performance
            ThemeLoader.PreloadThemes();
            
            Title = "Theme System Demo - Batch B";
        }

        private void SwitchToDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            bool success = ThemeLoader.SwitchTheme(ThemeType.Dark);
            
            ShowResult(success, "Dark Theme");
        }

        private void SwitchToLightTheme_Click(object sender, RoutedEventArgs e)
        {
            bool success = ThemeLoader.SwitchTheme(ThemeType.Light);
            
            ShowResult(success, "Light Theme");
        }

        private void ShowResult(bool success, string themeName)
        {
            if (success)
            {
                MessageBox.Show(
                    $"Successfully switched to {themeName}!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Failed to switch to {themeName}.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void VerifyBackwardCompatibility_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Test old keys
                var cyberBgPanel = Application.Current.TryFindResource("Cyber.Bg.Panel");
                var plcTextValue = Application.Current.TryFindResource("Plc.Text.Value");
                var buttonBgPrimary = Application.Current.TryFindResource("Button.Bg.Primary");
                
                // Test new semantic tokens
                var surfaceBgPanel = Application.Current.TryFindResource("Surface.Bg.Panel");
                var textPrimary = Application.Current.TryFindResource("Text.Primary");
                var actionPrimary = Application.Current.TryFindResource("Action.Primary");
                
                bool allFound = cyberBgPanel != null && 
                               plcTextValue != null && 
                               buttonBgPrimary != null &&
                               surfaceBgPanel != null &&
                               textPrimary != null &&
                               actionPrimary != null;
                
                if (allFound)
                {
                    MessageBox.Show(
                        "Backward Compatibility Check: PASSED\n\n" +
                        "All old and new keys found successfully!\n" +
                        "- Old: Cyber.*, Plc.*, Button.* ?\n" +
                        "- New: Surface.*, Text.*, Action.* ?",
                        "Verification Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Backward Compatibility Check: FAILED\n\n" +
                        "Some keys were not found.",
                        "Verification Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Verification Error:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
