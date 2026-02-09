using System.ComponentModel;
using System.Windows;

namespace Stackdose.UI.Core.Controls
{
    internal static class ControlRuntime
    {
        internal static bool IsDesignMode(DependencyObject target)
        {
            return DesignerProperties.GetIsInDesignMode(target);
        }

        internal static void ShowInfo(string message)
        {
            CyberMessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        internal static void ShowWarning(string message)
        {
            CyberMessageBox.Show(message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        internal static void ShowError(string message)
        {
            CyberMessageBox.Show(message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
