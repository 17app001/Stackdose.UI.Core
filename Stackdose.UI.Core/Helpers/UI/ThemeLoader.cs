using System;
using System.Linq;
using System.Windows;

namespace Stackdose.UI.Core.Helpers.UI
{
    /// <summary>
    /// �D??���[?�� - ���Ѧw�����D?��?���
    /// </summary>
    /// <remarks>
    /// <para>??��?�G</para>
    /// <list type="bullet">
    /// <item>?�{�w����?���r��ާ@</item>
    /// <item>Design-time �w���]�קK Designer �Y?�^</item>
    /// <item>��l�ʤ�?�]�n�\�������\�A�n�\�^?�^</item>
    /// <item>?�s���]�קK���`�[?�^</item>
    /// </list>
    /// </remarks>
    public static class ThemeLoader
    {
        #region Private Fields

        /// <summary>
        /// �D?����?�`�q
        /// </summary>
        private const string DarkThemeUri = "/Stackdose.UI.Core;component/Themes/Colors.xaml";
        private const string LightThemeUri = "/Stackdose.UI.Core;component/Themes/LightColors.xaml";
        
        /// <summary>
        /// ?�s�w�[?���D?�r��
        /// </summary>
        private static ResourceDictionary? _cachedDarkTheme;
        private static ResourceDictionary? _cachedLightTheme;
        
        /// <summary>
        /// ?�e�w�[?���D??��
        /// </summary>
        private static ThemeType _currentThemeType = ThemeType.Dark;

        #endregion

        #region Public Methods

        /// <summary>
        /// ��?�D?�]�w�������^
        /// </summary>
        /// <param name="themeType">��?�D??��</param>
        /// <returns>�O�_��?���\</returns>
        public static bool SwitchTheme(ThemeType themeType)
        {
            try
            {
                // �p�G�w?�O��?�D?�A������^
                if (_currentThemeType == themeType)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Already on {themeType} theme, skipping");
                    #endif
                    return true;
                }

                // ?��?�ε{��?���r��
                var appResources = Application.Current?.Resources;
                if (appResources == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[ThemeLoader] Application.Current.Resources is null");
                    #endif
                    return false;
                }

                // �[?��?��?�s���D?�r��
                var newThemeDict = GetThemeDictionary(themeType);
                if (newThemeDict == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Failed to load {themeType} theme");
                    #endif
                    return false;
                }

                // ��l�ʤ�?�G������?�D?�A�A�K�[�s�D?
                RemoveOldThemeDictionaries(appResources);
                appResources.MergedDictionaries.Add(newThemeDict);

                // ��s?�e�D???
                _currentThemeType = themeType;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Theme switched to {themeType} successfully");
                #endif

                // ?���s UI
                ForceRefreshUI();

                return true;
            }
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ThemeLoader] SwitchTheme failed");
                #endif
                return false;
            }
        }

        /// <summary>
        /// ?��?�e�D??��
        /// </summary>
        public static ThemeType GetCurrentThemeType()
        {
            return _currentThemeType;
        }

        /// <summary>
        /// ?�[?�Ҧ��D?�]ɬ��??�ʯ�^
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
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ThemeLoader] PreloadThemes failed");
                #endif
            }
        }

        /// <summary>
        /// �M���D??�s
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
        /// ?���D?�r��]??�s�^
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
        /// �[?�D?�r��
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
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeLoader] Failed to load {uriString}");
                #endif
                return null;
            }
        }

        /// <summary>
        /// ����?���D?�r��
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
        /// ?���s UI
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
            catch (Exception)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ThemeLoader] ForceRefreshUI failed");
                #endif
            }
        }

        #endregion
    }

    /// <summary>
    /// �D??���T?
    /// </summary>
    public enum ThemeType
    {
        /// <summary>�t��D?</summary>
        Dark,
        /// <summary>�G��D?</summary>
        Light
    }
}
