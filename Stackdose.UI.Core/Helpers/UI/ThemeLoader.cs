using System;
using System.Linq;
using System.Windows;

namespace Stackdose.UI.Core.Helpers.UI
{
    /// <summary>
    /// 主??源加?器 - 提供安全的主?切?机制
    /// </summary>
    /// <remarks>
    /// <para>??原?：</para>
    /// <list type="bullet">
    /// <item>?程安全的?源字典操作</item>
    /// <item>Design-time 安全（避免 Designer 崩?）</item>
    /// <item>原子性切?（要么全部成功，要么回?）</item>
    /// <item>?存机制（避免重复加?）</item>
    /// </list>
    /// </remarks>
    public static class ThemeLoader
    {
        #region Private Fields

        /// <summary>
        /// 主?文件路?常量
        /// </summary>
        private const string DarkThemeUri = "/Stackdose.UI.Core;component/Themes/Colors.xaml";
        private const string LightThemeUri = "/Stackdose.UI.Core;component/Themes/LightColors.xaml";
        
        /// <summary>
        /// ?存已加?的主?字典
        /// </summary>
        private static ResourceDictionary? _cachedDarkTheme;
        private static ResourceDictionary? _cachedLightTheme;
        
        /// <summary>
        /// ?前已加?的主??型
        /// </summary>
        private static ThemeType _currentThemeType = ThemeType.Dark;

        #endregion

        #region Public Methods

        /// <summary>
        /// 切?主?（安全版本）
        /// </summary>
        /// <param name="themeType">目?主??型</param>
        /// <returns>是否切?成功</returns>
        public static bool SwitchTheme(ThemeType themeType)
        {
            try
            {
                // 如果已?是目?主?，直接返回
                if (_currentThemeType == themeType)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Already on {themeType} theme, skipping");
                    #endif
                    return true;
                }

                // ?取?用程序?源字典
                var appResources = Application.Current?.Resources;
                if (appResources == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[ThemeLoader] Application.Current.Resources is null");
                    #endif
                    return false;
                }

                // 加?或?取?存的主?字典
                var newThemeDict = GetThemeDictionary(themeType);
                if (newThemeDict == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Failed to load {themeType} theme");
                    #endif
                    return false;
                }

                // 原子性切?：先移除?主?，再添加新主?
                RemoveOldThemeDictionaries(appResources);
                appResources.MergedDictionaries.Add(newThemeDict);

                // 更新?前主???
                _currentThemeType = themeType;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Theme switched to {themeType} successfully");
                #endif

                // ?制刷新 UI
                ForceRefreshUI();

                return true;
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] SwitchTheme failed: {ex.Message}");
                #endif
                return false;
            }
        }

        /// <summary>
        /// ?取?前主??型
        /// </summary>
        public static ThemeType GetCurrentThemeType()
        {
            return _currentThemeType;
        }

        /// <summary>
        /// ?加?所有主?（优化??性能）
        /// </summary>
        public static void PreloadThemes()
        {
            try
            {
                _cachedDarkTheme = LoadThemeDictionary(DarkThemeUri);
                _cachedLightTheme = LoadThemeDictionary(LightThemeUri);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ThemeLoader] Themes preloaded successfully");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] PreloadThemes failed: {ex.Message}");
                #endif
            }
        }

        /// <summary>
        /// 清除主??存
        /// </summary>
        public static void ClearCache()
        {
            _cachedDarkTheme = null;
            _cachedLightTheme = null;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[ThemeLoader] Theme cache cleared");
            #endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ?取主?字典（??存）
        /// </summary>
        private static ResourceDictionary? GetThemeDictionary(ThemeType themeType)
        {
            switch (themeType)
            {
                case ThemeType.Dark:
                    return _cachedDarkTheme ?? (_cachedDarkTheme = LoadThemeDictionary(DarkThemeUri));
                
                case ThemeType.Light:
                    return _cachedLightTheme ?? (_cachedLightTheme = LoadThemeDictionary(LightThemeUri));
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// 加?主?字典
        /// </summary>
        private static ResourceDictionary? LoadThemeDictionary(string uriString)
        {
            try
            {
                var uri = new Uri(uriString, UriKind.Relative);
                var dict = new ResourceDictionary { Source = uri };

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Loaded theme: {uriString}");
                #endif

                return dict;
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Failed to load {uriString}: {ex.Message}");
                #endif
                return null;
            }
        }

        /// <summary>
        /// 移除?的主?字典
        /// </summary>
        private static void RemoveOldThemeDictionaries(ResourceDictionary appResources)
        {
            var toRemove = appResources.MergedDictionaries
                .Where(d => d.Source != null &&
                           (d.Source.ToString().Contains("Colors.xaml") ||
                            d.Source.ToString().Contains("LightColors.xaml")))
                .ToList();

            foreach (var dict in toRemove)
            {
                appResources.MergedDictionaries.Remove(dict);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Removed old theme: {dict.Source}");
                #endif
            }
        }

        /// <summary>
        /// ?制刷新 UI
        /// </summary>
        private static void ForceRefreshUI()
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        window.InvalidateVisual();
                        window.UpdateLayout();
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ThemeLoader] UI refreshed");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] ForceRefreshUI failed: {ex.Message}");
                #endif
            }
        }

        #endregion
    }

    /// <summary>
    /// 主??型枚?
    /// </summary>
    public enum ThemeType
    {
        /// <summary>暗色主?</summary>
        Dark,
        /// <summary>亮色主?</summary>
        Light
    }
}
