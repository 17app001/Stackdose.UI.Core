using System;
using System.Linq;
using System.Windows;

namespace Stackdose.UI.Core.Services;

/// <summary>
/// 佈景主題管理員：負責在 Dark (默認) 與 Light 模式之間切換。
/// </summary>
public static class ThemeManager
{
    private const string CoreNamespace = "Stackdose.UI.Core";
    private const string DarkThemeUri  = "pack://application:,,,/Stackdose.UI.Core;component/Themes/Colors.xaml";
    private const string LightThemeUri = "pack://application:,,,/Stackdose.UI.Core;component/Themes/LightTheme.xaml";

    public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;

    public enum ThemeType { Dark, Light }

    public static void ApplyTheme(Application app, ThemeType theme)
    {
        string targetSource = theme == ThemeType.Light ? LightThemeUri : DarkThemeUri;
        string otherSource  = theme == ThemeType.Light ? DarkThemeUri : LightThemeUri;

        if (TryReplaceResource(app.Resources, targetSource, otherSource))
        {
            CurrentTheme = theme;
        }
    }

    private static bool TryReplaceResource(ResourceDictionary root, string targetUri, string otherUri)
    {
        bool found = false;
        // 1. 檢查目前層級是否有匹配的主題
        for (int i = 0; i < root.MergedDictionaries.Count; i++)
        {
            var dict = root.MergedDictionaries[i];
            bool isMatch = false;
            
            if (dict.Source != null)
            {
                string src = dict.Source.ToString();
                // 檢查是否為主題色字典 (Colors.xaml, LightTheme.xaml, LightColors.xaml)
                if (src.EndsWith("Colors.xaml", StringComparison.OrdinalIgnoreCase) || 
                    src.EndsWith("LightTheme.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    // 找到目標或另一個主題，進行替換
                    if (!src.Equals(targetUri, StringComparison.OrdinalIgnoreCase))
                    {
                        // 必須用 RemoveAt+Insert，不能用 [i]=，後者不觸發 NotifyOwners
                        root.MergedDictionaries.RemoveAt(i);
                        root.MergedDictionaries.Insert(i, new ResourceDictionary { Source = new Uri(targetUri) });
                    }
                    isMatch = true;
                    found = true;
                }
            }

            // 2. 遞迴檢查子字典 (即使當前層級已匹配，子字典內可能還有其他需要替換的，例如 Theme.xaml 內部)
            if (!isMatch) // 如果當前層級已經是主題字典，通常不需要再往內找
            {
                if (TryReplaceResource(dict, targetUri, otherUri))
                {
                    found = true;
                }
            }
        }
        return found;
    }

    public static void ToggleTheme(Application app)
    {
        ApplyTheme(app, CurrentTheme == ThemeType.Dark ? ThemeType.Light : ThemeType.Dark);
    }
}
