using System;
using System.Linq;
using System.Windows;

namespace Stackdose.UI.Core.Services;

/// <summary>
/// 佈景主題管理員：負責在 Dark (默認) 與 Light 模式之間切換。
/// </summary>
public static class ThemeManager
{
    private const string LightThemeUri = "pack://application:,,,/Stackdose.UI.Core;component/Themes/LightTheme.xaml";

    public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;

    public enum ThemeType { Dark, Light }

    // Cached reference to the top-level light override dict for reliable removal.
    private static ResourceDictionary? _lightOverride;

    public static void ApplyTheme(Application app, ThemeType theme)
    {
        var merged = app.Resources.MergedDictionaries;

        // Remove existing light override (if any) to start clean.
        if (_lightOverride != null)
        {
            merged.Remove(_lightOverride);
            _lightOverride = null;
        }

        if (theme == ThemeType.Light)
        {
            // Add LightTheme.xaml at the END of Application.Resources.MergedDictionaries.
            // WPF searches MergedDictionaries in reverse (last = highest priority),
            // so this reliably overrides all tokens from the nested Colors.xaml inside Theme.xaml.
            // Direct Application.Resources modification guarantees DynamicResource notifications fire.
            _lightOverride = new ResourceDictionary { Source = new Uri(LightThemeUri) };
            merged.Add(_lightOverride);
        }

        CurrentTheme = theme;
    }

    public static void ToggleTheme(Application app)
    {
        ApplyTheme(app, CurrentTheme == ThemeType.Dark ? ThemeType.Light : ThemeType.Dark);
    }
}
