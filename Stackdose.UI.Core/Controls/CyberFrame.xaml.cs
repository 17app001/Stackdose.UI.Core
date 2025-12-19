using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 賽博風格主框架控制項
    /// </summary>
    /// <remarks>
    /// <para>提供完整的工業控制介面框架，包含：</para>
    /// <list type="bullet">
    /// <item>系統標題列與時鐘顯示</item>
    /// <item>使用者登入/登出狀態顯示</item>
    /// <item>Dark/Light 主題切換</item>
    /// <item>狀態指示器（可選）</item>
    /// <item>自動填滿整個 Window</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法：
    /// <code>
    /// &lt;Custom:CyberFrame Title="MODEL-S" /&gt;
    /// </code>
    /// </example>
    public partial class CyberFrame : UserControl
    {
        #region Private Fields

        /// <summary>時鐘計時器</summary>
        private DispatcherTimer? _clockTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        public CyberFrame()
        {
            InitializeComponent();

            InitializeClock();
            InitializeSecurityEvents();
            UpdateUserInfo();

            // 控制項卸載時清理資源
            this.Unloaded += CyberFrame_Unloaded;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化時鐘計時器
        /// </summary>
        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        /// <summary>
        /// 訂閱安全上下文事件
        /// </summary>
        private void InitializeSecurityEvents()
        {
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;
        }

        /// <summary>
        /// 控制項卸載時的清理工作
        /// </summary>
        private void CyberFrame_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消訂閱事件避免記憶體洩漏
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;
            
            // 停止並清理計時器
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer.Tick -= ClockTimer_Tick;
                _clockTimer = null;
            }
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 系統標題
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title), 
                typeof(string), 
                typeof(CyberFrame),
                new PropertyMetadata("SYSTEM"));

        /// <summary>
        /// 取得或設定系統標題
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// 是否顯示狀態指示器（Alarm、Total 等）
        /// </summary>
        public static readonly DependencyProperty ShowStatusIndicatorsProperty =
            DependencyProperty.Register(
                nameof(ShowStatusIndicators), 
                typeof(bool), 
                typeof(CyberFrame), 
                new PropertyMetadata(false, OnShowStatusIndicatorsChanged));

        /// <summary>
        /// 取得或設定是否顯示狀態指示器
        /// </summary>
        public bool ShowStatusIndicators
        {
            get => (bool)GetValue(ShowStatusIndicatorsProperty);
            set => SetValue(ShowStatusIndicatorsProperty, value);
        }

        /// <summary>
        /// ShowStatusIndicators 屬性變更回呼
        /// </summary>
        private static void OnShowStatusIndicatorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame && frame.FindName("StatusIndicatorsPanel") is StackPanel panel)
            {
                panel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 是否使用淺色主題 (Light Theme)
        /// </summary>
        public static readonly DependencyProperty UseLightThemeProperty =
            DependencyProperty.Register(
                nameof(UseLightTheme), 
                typeof(bool), 
                typeof(CyberFrame),
                new PropertyMetadata(false, OnUseLightThemeChanged));

        /// <summary>
        /// 取得或設定是否使用淺色主題
        /// </summary>
        /// <remarks>
        /// 變更此屬性會自動觸發主題切換，並通知所有相關控制項更新
        /// </remarks>
        public bool UseLightTheme
        {
            get => (bool)GetValue(UseLightThemeProperty);
            set => SetValue(UseLightThemeProperty, value);
        }

        /// <summary>
        /// UseLightTheme 屬性變更回呼
        /// </summary>
        private static void OnUseLightThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame)
            {
                frame.ApplyTheme((bool)e.NewValue);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 時鐘更新
        /// </summary>
        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 登入成功事件
        /// </summary>
        private void OnLoginSuccess(object? sender, Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        /// <summary>
        /// 登出事件
        /// </summary>
        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        /// <summary>
        /// 登出按鈕點擊
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CyberMessageBox.Show(
                "確定要登出嗎？",
                "登出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                SecurityContext.Logout();
            }
        }

        /// <summary>
        /// 主題切換按鈕點擊事件
        /// </summary>
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("========== Theme Toggle START ==========");
            System.Diagnostics.Debug.WriteLine($"Current UseLightTheme: {UseLightTheme}");
            
            // 輸出當前資源字典狀態
            var appResources = Application.Current.Resources;
            System.Diagnostics.Debug.WriteLine($"Total MergedDictionaries Before: {appResources.MergedDictionaries.Count}");
            foreach (var dict in appResources.MergedDictionaries)
            {
                if (dict.Source != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {dict.Source}");
                }
            }
            
            // 切換主題
            ToggleTheme();
            
            System.Diagnostics.Debug.WriteLine($"New UseLightTheme: {UseLightTheme}");
            System.Diagnostics.Debug.WriteLine($"Total MergedDictionaries After: {appResources.MergedDictionaries.Count}");
            foreach (var dict in appResources.MergedDictionaries)
            {
                if (dict.Source != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {dict.Source}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("========== Theme Toggle END ==========");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 更新使用者資訊顯示
        /// </summary>
        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;
            
            // 使用 FindName 查找控制項
            var userNameText = this.FindName("UserNameText") as TextBlock;
            var userLevelText = this.FindName("UserLevelText") as TextBlock;

            if (userNameText != null && userLevelText != null)
            {
                if (session.IsLoggedIn)
                {
                    userNameText.Text = session.CurrentUserName;
                    userLevelText.Text = session.CurrentLevel.ToString();
                }
                else
                {
                    userNameText.Text = "Guest";
                    userLevelText.Text = "Not Logged In";
                }
            }
        }

        /// <summary>
        /// 套用主題
        /// </summary>
        /// <param name="useLightTheme">是否使用淺色主題</param>
        private void ApplyTheme(bool useLightTheme)
        {
            System.Diagnostics.Debug.WriteLine($"Applying Theme: {(useLightTheme ? "Light" : "Dark")}");
            
            try
            {
                // 取得應用程式層級的資源字典
                var appResources = Application.Current.Resources;
                
                // 載入對應的主題檔案
                var themeUri = new Uri(
                    useLightTheme 
                        ? "/Stackdose.UI.Core;component/Themes/LightColors.xaml" 
                        : "/Stackdose.UI.Core;component/Themes/Colors.xaml",
                    UriKind.Relative);

                var newThemeDict = new ResourceDictionary { Source = themeUri };
                
                // 找到並移除所有包含 Colors.xaml 或 LightColors.xaml 的字典
                var toRemove = appResources.MergedDictionaries
                    .Where(d => d.Source != null && 
                               (d.Source.ToString().Contains("Colors.xaml") || 
                                d.Source.ToString().Contains("LightColors.xaml")))
                    .ToList();

                foreach (var dict in toRemove)
                {
                    appResources.MergedDictionaries.Remove(dict);
                    System.Diagnostics.Debug.WriteLine($"Removed: {dict.Source}");
                }

                // 加入新的主題字典
                appResources.MergedDictionaries.Add(newThemeDict);
                
                System.Diagnostics.Debug.WriteLine($"Theme Applied Successfully: {themeUri}");
                System.Diagnostics.Debug.WriteLine($"Total MergedDictionaries: {appResources.MergedDictionaries.Count}");
                
                // 🔥 通知所有 PlcLabel 主題已變化
                PlcLabelContext.NotifyThemeChanged();
                System.Diagnostics.Debug.WriteLine("[CyberFrame] PlcLabel 主題變化通知已發送");
                
                // 🔥 刷新所有 LiveLogViewer
                foreach (Window window in Application.Current.Windows)
                {
                    RefreshLiveLogViewers(window);
                }
                System.Diagnostics.Debug.WriteLine("[CyberFrame] LiveLogViewer 刷新完成");
                
                // 強制刷新 UI
                Application.Current.Dispatcher.Invoke(() => 
                {
                    // 觸發視覺樹重繪
                    foreach (Window window in Application.Current.Windows)
                    {
                        window.InvalidateVisual();
                        window.UpdateLayout();
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
                
                //// 顯示通知
                //CyberMessageBox.Show(
                //    $"Theme changed to {(useLightTheme ? "Light" : "Dark")} mode",
                //    "Theme Switch",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Information
                //);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme Apply Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                CyberMessageBox.Show(
                    $"Theme switch failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 切換主題（公開方法，可從外部呼叫）
        /// </summary>
        public void ToggleTheme()
        {
            UseLightTheme = !UseLightTheme;
        }

        /// <summary>
        /// 刷新視覺樹中的所有 LiveLogViewer
        /// </summary>
        private void RefreshLiveLogViewers(DependencyObject parent)
        {
            if (parent == null) return;

            if (parent is LiveLogViewer liveLogViewer)
            {
                liveLogViewer.RefreshLogColors();
                return;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                RefreshLiveLogViewers(child);
            }
        }

        #endregion
    }
}