using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace Stackdose.UI.Core.Examples
{
    /// <summary>
    /// ThemeManager 功能展示視窗
    /// </summary>
    /// <remarks>
    /// 展示以下功能：
    /// <list type="bullet">
    /// <item>統一主題切換（透過 ThemeManager）</item>
    /// <item>自動註冊/註銷 IThemeAware 控制項</item>
    /// <item>WeakReference 記憶體管理</item>
    /// <item>主題統計資訊</item>
    /// <item>多控制項同步更新</item>
    /// </list>
    /// </remarks>
    public class ThemeManagerDemoWindow : Window
    {
        /// <summary>
        /// 自訂測試控制項（實作 IThemeAware）
        /// </summary>
        private class TestControl : UserControl, IThemeAware
        {
            private readonly TextBlock _textBlock;
            private readonly string _name;

            public TestControl(string name)
            {
                _name = name;
                _textBlock = new TextBlock
                {
                    Text = $"{name}: Waiting for theme...",
                    Padding = new Thickness(10),
                    FontSize = 14
                };
                Content = _textBlock;

                // 自動註冊到 ThemeManager
                Loaded += (s, e) => ThemeManager.Register(this);
                Unloaded += (s, e) => ThemeManager.Unregister(this);
            }

            public void OnThemeChanged(ThemeChangedEventArgs e)
            {
                _textBlock.Text = $"{_name}: {e.ThemeName} ({(e.IsLightTheme ? "Light" : "Dark")}) at {e.ChangedAt:HH:mm:ss}";
                Background = e.IsLightTheme 
                    ? System.Windows.Media.Brushes.LightGray 
                    : System.Windows.Media.Brushes.DarkGray;
                
                Debug.WriteLine($"[TestControl:{_name}] OnThemeChanged called");
            }
        }

        public ThemeManagerDemoWindow()
        {
            Title = "ThemeManager Demo";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            InitializeDemo();
            
            // 訂閱全域主題變更事件
            ThemeManager.ThemeChanged += OnGlobalThemeChanged;
            
            Closed += (s, e) => ThemeManager.ThemeChanged -= OnGlobalThemeChanged;
        }

        private void InitializeDemo()
        {
            // 建立測試控制項
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            
            // 標題
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "ThemeManager 功能展示",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 當前主題資訊
            var currentThemeText = new TextBlock
            {
                Text = $"當前主題: {ThemeManager.CurrentTheme}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(currentThemeText);

            // 統計資訊
            var statsText = new TextBlock
            {
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };
            UpdateStatsText(statsText);
            stackPanel.Children.Add(statsText);

            // 測試控制項容器
            var testControlsPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
            testControlsPanel.Children.Add(new TextBlock 
            { 
                Text = "已註冊的測試控制項：",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            for (int i = 1; i <= 5; i++)
            {
                testControlsPanel.Children.Add(new TestControl($"TestControl{i}"));
            }
            stackPanel.Children.Add(testControlsPanel);

            // 按鈕區域
            var buttonPanel = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            
            // 切換到 Light 主題
            var lightButton = new Button 
            { 
                Content = "切換到 Light 主題",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(15, 5, 15, 5)
            };
            lightButton.Click += (s, e) => 
            {
                ThemeManager.SwitchTheme(ThemeType.Light);
                currentThemeText.Text = $"當前主題: {ThemeManager.CurrentTheme}";
                UpdateStatsText(statsText);
            };
            buttonPanel.Children.Add(lightButton);

            // 切換到 Dark 主題
            var darkButton = new Button 
            { 
                Content = "切換到 Dark 主題",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(15, 5, 15, 5)
            };
            darkButton.Click += (s, e) => 
            {
                ThemeManager.SwitchTheme(ThemeType.Dark);
                currentThemeText.Text = $"當前主題: {ThemeManager.CurrentTheme}";
                UpdateStatsText(statsText);
            };
            buttonPanel.Children.Add(darkButton);

            // 刷新主題
            var refreshButton = new Button 
            { 
                Content = "刷新主題",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(15, 5, 15, 5)
            };
            refreshButton.Click += (s, e) => 
            {
                ThemeManager.RefreshTheme();
                UpdateStatsText(statsText);
            };
            buttonPanel.Children.Add(refreshButton);

            // 手動清理
            var cleanupButton = new Button 
            { 
                Content = "手動清理失效參考",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(15, 5, 15, 5)
            };
            cleanupButton.Click += (s, e) => 
            {
                ThemeManager.Cleanup();
                UpdateStatsText(statsText);
                MessageBox.Show("清理完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            buttonPanel.Children.Add(cleanupButton);

            // 列印已註冊控制項
            var printButton = new Button 
            { 
                Content = "列印已註冊控制項 (Debug)",
                Margin = new Thickness(0, 0, 5, 5),
                Padding = new Thickness(15, 5, 15, 5)
            };
            printButton.Click += (s, e) => 
            {
                ThemeManager.PrintRegisteredControls();
                MessageBox.Show("已輸出到 Debug Console", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            buttonPanel.Children.Add(printButton);

            stackPanel.Children.Add(buttonPanel);

            // 說明文字
            var infoText = new TextBlock
            {
                Text = "?? 提示：\n" +
                       "? 切換主題時，所有已註冊的控制項會自動更新\n" +
                       "? 使用 WeakReference 管理控制項，不會造成記憶體洩漏\n" +
                       "? 關閉此視窗後，測試控制項會自動註銷\n" +
                       "? 可在 Debug Console 查看詳細日誌",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 15, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(infoText);

            Content = new ScrollViewer 
            { 
                Content = stackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private void UpdateStatsText(TextBlock textBlock)
        {
            var stats = ThemeManager.GetStatistics();
            textBlock.Text = $"統計資訊: 總註冊={stats.Total}, 存活={stats.Alive}, 失效={stats.Dead}";
        }

        private void OnGlobalThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            Debug.WriteLine($"[ThemeManagerDemo] 全域主題變更事件: {e}");
            
            Dispatcher.BeginInvoke(() =>
            {
                Title = $"ThemeManager Demo - {e.ThemeName} Mode";
            });
        }
    }
}
