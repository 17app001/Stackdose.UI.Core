using Stackdose.UI.Core.Models;
using System;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// 主題服務介面（用於依賴注入）
    /// </summary>
    /// <remarks>
    /// <para>提供主題管理的抽象層，方便測試與替換實作</para>
    /// <para>預設實作為 ThemeManager 的包裝</para>
    /// </remarks>
    public interface IThemeService
    {
        /// <summary>
        /// 主題變更事件
        /// </summary>
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// 取得當前主題資訊
        /// </summary>
        ThemeChangedEventArgs CurrentTheme { get; }

        /// <summary>
        /// 取得當前是否為淺色主題
        /// </summary>
        bool IsLightTheme { get; }

        /// <summary>
        /// 註冊主題感知控制項
        /// </summary>
        /// <param name="control">實作 IThemeAware 的控制項</param>
        void Register(IThemeAware control);

        /// <summary>
        /// 註銷主題感知控制項
        /// </summary>
        /// <param name="control">要註銷的控制項</param>
        void Unregister(IThemeAware control);

        /// <summary>
        /// 切換主題
        /// </summary>
        /// <param name="themeType">主題類型</param>
        /// <param name="themeName">主題名稱（選填）</param>
        /// <returns>是否切換成功</returns>
        bool SwitchTheme(ThemeType themeType, string? themeName = null);

        /// <summary>
        /// 刷新當前主題
        /// </summary>
        void RefreshTheme();

        /// <summary>
        /// 偵測當前主題
        /// </summary>
        /// <returns>主題資訊</returns>
        ThemeChangedEventArgs DetectTheme();

        /// <summary>
        /// 清理失效的控制項參考
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 取得統計資訊
        /// </summary>
        /// <returns>(總數, 存活, 失效)</returns>
        (int Total, int Alive, int Dead) GetStatistics();
    }
}
