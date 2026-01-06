using Stackdose.UI.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 統一主題管理器
    /// </summary>
    /// <remarks>
    /// <para>提供以下功能：</para>
    /// <list type="bullet">
    /// <item>自動註冊/註銷 IThemeAware 控制項</item>
    /// <item>統一主題切換通知</item>
    /// <item>主題檢測與快取</item>
    /// <item>執行緒安全的主題管理</item>
    /// <item>弱引用機制防止記憶體洩漏</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 註冊控制項：
    /// <code>
    /// ThemeManager.Register(this);
    /// </code>
    /// 手動切換主題：
    /// <code>
    /// ThemeManager.SwitchTheme(ThemeType.Light);
    /// </code>
    /// </example>
    public static class ThemeManager
    {
        #region Private Fields

        /// <summary>
        /// 已註冊的主題感知控制項（使用 WeakReference 防止記憶體洩漏）
        /// </summary>
        private static readonly ConcurrentBag<WeakReference<IThemeAware>> _registeredControls = new();

        /// <summary>
        /// 執行緒鎖
        /// </summary>
        private static readonly object _lock = new();

        /// <summary>
        /// 當前主題快取
        /// </summary>
        private static ThemeChangedEventArgs? _currentTheme;

        /// <summary>
        /// 最後清理時間（用於定期清理失效的 WeakReference）
        /// </summary>
        private static DateTime _lastCleanupTime = DateTime.Now;

        /// <summary>
        /// 清理間隔（秒）
        /// </summary>
        private static readonly int _cleanupIntervalSeconds = 30;

        #endregion

        #region Events

        /// <summary>
        /// 全域主題變更事件
        /// </summary>
        /// <remarks>
        /// 除了自動通知 IThemeAware 控制項外，也可透過此事件訂閱主題變更
        /// </remarks>
        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// 取得當前主題資訊
        /// </summary>
        public static ThemeChangedEventArgs CurrentTheme
        {
            get
            {
                if (_currentTheme == null)
                {
                    // 首次存取時自動檢測
                    _currentTheme = DetectCurrentTheme();
                }
                return _currentTheme;
            }
        }

        /// <summary>
        /// 取得已註冊的控制項數量（包含失效的）
        /// </summary>
        public static int RegisteredControlsCount => _registeredControls.Count;

        /// <summary>
        /// 取得存活的控制項數量
        /// </summary>
        public static int AliveControlsCount
        {
            get
            {
                lock (_lock)
                {
                    return _registeredControls.Count(wr => wr.TryGetTarget(out _));
                }
            }
        }

        #endregion

        #region Registration

        /// <summary>
        /// 註冊主題感知控制項
        /// </summary>
        /// <param name="control">實作 IThemeAware 的控制項</param>
        /// <remarks>
        /// 使用 WeakReference 儲存，不會阻止控制項被 GC 回收
        /// </remarks>
        public static void Register(IThemeAware control)
        {
            if (control == null)
                return;

            lock (_lock)
            {
                // 檢查是否已註冊（避免重複）
                bool alreadyRegistered = _registeredControls.Any(wr =>
                {
                    if (wr.TryGetTarget(out var target))
                        return ReferenceEquals(target, control);
                    return false;
                });

                if (!alreadyRegistered)
                {
                    _registeredControls.Add(new WeakReference<IThemeAware>(control));

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 註冊控制項: {control.GetType().Name} (總數: {RegisteredControlsCount})");
                    #endif

                    // 立即通知當前主題
                    try
                    {
                        control.OnThemeChanged(CurrentTheme);
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeManager] 註冊時通知主題失敗: {ex.Message}");
                        #endif
                    }
                }

                // 定期清理失效的 WeakReference
                AutoCleanup();
            }
        }

        /// <summary>
        /// 註銷主題感知控制項
        /// </summary>
        /// <param name="control">要註銷的控制項</param>
        /// <remarks>
        /// 通常不需要手動呼叫，WeakReference 會自動處理
        /// </remarks>
        public static void Unregister(IThemeAware control)
        {
            if (control == null)
                return;

            lock (_lock)
            {
                // 移除對應的 WeakReference
                var toRemove = _registeredControls
                    .Where(wr =>
                    {
                        if (wr.TryGetTarget(out var target))
                            return ReferenceEquals(target, control);
                        return true; // 順便清理已失效的
                    })
                    .ToList();

                foreach (var wr in toRemove)
                {
                    _registeredControls.TryTake(out _);
                }

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] 註銷控制項: {control.GetType().Name} (剩餘: {RegisteredControlsCount})");
                #endif
            }
        }

        #endregion

        #region Theme Switching

        /// <summary>
        /// 切換主題
        /// </summary>
        /// <param name="themeType">主題類型</param>
        /// <param name="themeName">主題名稱（選填）</param>
        /// <returns>是否切換成功</returns>
        public static bool SwitchTheme(ThemeType themeType, string? themeName = null)
        {
            bool isLightTheme = themeType == ThemeType.Light;
            string theme = themeName ?? themeType.ToString();

            return SwitchTheme(isLightTheme, theme);
        }

        /// <summary>
        /// 切換主題（進階版）
        /// </summary>
        /// <param name="isLightTheme">是否為淺色主題</param>
        /// <param name="themeName">主題名稱</param>
        /// <param name="backgroundColor">背景色（選填）</param>
        /// <param name="foregroundColor">前景色（選填）</param>
        /// <returns>是否切換成功</returns>
        public static bool SwitchTheme(
            bool isLightTheme, 
            string themeName = "Custom",
            Color? backgroundColor = null,
            Color? foregroundColor = null)
        {
            lock (_lock)
            {
                try
                {
                    var newTheme = new ThemeChangedEventArgs(
                        isLightTheme,
                        themeName,
                        backgroundColor,
                        foregroundColor
                    );

                    // 1. 詢問進階控制項是否允許切換
                    foreach (var weakRef in _registeredControls)
                    {
                        if (weakRef.TryGetTarget(out var control))
                        {
                            if (control is INotifyThemeAware notifyControl)
                            {
                                if (!notifyControl.OnThemeChanging(newTheme))
                                {
                                    #if DEBUG
                                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 主題切換被 {control.GetType().Name} 拒絕");
                                    #endif
                                    return false;
                                }
                            }
                        }
                    }

                    // 2. 更新快取
                    _currentTheme = newTheme;

                    // 3. 通知所有已註冊的控制項
                    int notifiedCount = 0;
                    int failedCount = 0;

                    foreach (var weakRef in _registeredControls)
                    {
                        if (weakRef.TryGetTarget(out var control))
                        {
                            try
                            {
                                control.OnThemeChanged(newTheme);
                                notifiedCount++;
                            }
                            catch (Exception ex)
                            {
                                failedCount++;
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[ThemeManager] 通知失敗 ({control.GetType().Name}): {ex.Message}");
                                #endif
                            }
                        }
                    }

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 主題已切換為 {themeName} ({(isLightTheme ? "Light" : "Dark")})");
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 通知成功: {notifiedCount}, 失敗: {failedCount}");
                    #endif

                    // 4. 觸發全域事件
                    ThemeChanged?.Invoke(null, newTheme);

                    // 5. 清理失效的 WeakReference
                    AutoCleanup();

                    return true;
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 切換主題失敗: {ex.Message}");
                    #endif
                    return false;
                }
            }
        }

        /// <summary>
        /// 刷新當前主題（重新通知所有控制項）
        /// </summary>
        public static void RefreshTheme()
        {
            var current = CurrentTheme;
            SwitchTheme(current.IsLightTheme, current.ThemeName, current.BackgroundColor, current.ForegroundColor);
        }

        #endregion

        #region Theme Detection

        /// <summary>
        /// 偵測當前主題
        /// </summary>
        /// <returns>主題資訊</returns>
        public static ThemeChangedEventArgs DetectCurrentTheme()
        {
            try
            {
                // 嘗試從 Application Resources 讀取 Plc.Bg.Main
                if (Application.Current?.TryFindResource("Plc.Bg.Main") is SolidColorBrush bgBrush)
                {
                    var bgColor = bgBrush.Color;
                    bool isLight = bgColor.R > 200 && bgColor.G > 200 && bgColor.B > 200;

                    // 嘗試讀取前景色
                    Color? fgColor = null;
                    if (Application.Current?.TryFindResource("Plc.Fg.Main") is SolidColorBrush fgBrush)
                    {
                        fgColor = fgBrush.Color;
                    }

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 偵測到主題: {(isLight ? "Light" : "Dark")}, Bg={bgColor}");
                    #endif

                    return new ThemeChangedEventArgs(
                        isLight,
                        isLight ? "Light" : "Dark",
                        bgColor,
                        fgColor
                    );
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] 主題偵測失敗: {ex.Message}");
                #endif
            }

            // 預設為 Dark 主題
            return new ThemeChangedEventArgs(false, "Dark");
        }

        /// <summary>
        /// 取得當前是否為淺色主題
        /// </summary>
        public static bool IsLightTheme()
        {
            return CurrentTheme.IsLightTheme;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 自動清理失效的 WeakReference
        /// </summary>
        private static void AutoCleanup()
        {
            // 每 30 秒清理一次
            if ((DateTime.Now - _lastCleanupTime).TotalSeconds < _cleanupIntervalSeconds)
                return;

            Cleanup();
        }

        /// <summary>
        /// 手動清理失效的 WeakReference
        /// </summary>
        public static void Cleanup()
        {
            lock (_lock)
            {
                int beforeCount = _registeredControls.Count;

                // 移除所有已失效的 WeakReference
                var deadRefs = _registeredControls
                    .Where(wr => !wr.TryGetTarget(out _))
                    .ToList();

                foreach (var deadRef in deadRefs)
                {
                    _registeredControls.TryTake(out _);
                }

                int afterCount = _registeredControls.Count;
                int removed = beforeCount - afterCount;

                if (removed > 0)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeManager] 清理了 {removed} 個失效的控制項參考 (剩餘: {afterCount})");
                    #endif
                }

                _lastCleanupTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 取得統計資訊
        /// </summary>
        public static (int Total, int Alive, int Dead) GetStatistics()
        {
            lock (_lock)
            {
                int total = _registeredControls.Count;
                int alive = _registeredControls.Count(wr => wr.TryGetTarget(out _));
                int dead = total - alive;

                return (total, alive, dead);
            }
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// 列印已註冊的控制項清單（DEBUG 用）
        /// </summary>
        public static void PrintRegisteredControls()
        {
            #if DEBUG
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine("========== ThemeManager Registered Controls ==========");
                int index = 1;
                foreach (var weakRef in _registeredControls)
                {
                    if (weakRef.TryGetTarget(out var control))
                    {
                        System.Diagnostics.Debug.WriteLine($"  [{index}] {control.GetType().FullName} (Alive)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  [{index}] (Dead Reference)");
                    }
                    index++;
                }
                System.Diagnostics.Debug.WriteLine($"Total: {RegisteredControlsCount}, Alive: {AliveControlsCount}");
                System.Diagnostics.Debug.WriteLine("====================================================");
            }
            #endif
        }

        #endregion
    }
}
