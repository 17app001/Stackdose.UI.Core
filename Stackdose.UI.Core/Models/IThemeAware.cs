using System;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 主題感知介面
    /// </summary>
    /// <remarks>
    /// <para>實作此介面的控制項會自動接收主題變更通知</para>
    /// <para>用於實現統一的主題管理機制</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public partial class MyControl : UserControl, IThemeAware
    /// {
    ///     public void OnThemeChanged(ThemeChangedEventArgs e)
    ///     {
    ///         if (e.IsLightTheme)
    ///         {
    ///             this.Background = Brushes.White;
    ///         }
    ///         else
    ///         {
    ///             this.Background = Brushes.Black;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IThemeAware
    {
        /// <summary>
        /// 主題變更時的回呼方法
        /// </summary>
        /// <param name="e">主題變更事件參數</param>
        /// <remarks>
        /// 此方法會在 ThemeManager 切換主題時自動呼叫
        /// 實作時應避免執行耗時操作，以免影響 UI 回應速度
        /// </remarks>
        void OnThemeChanged(ThemeChangedEventArgs e);
    }

    /// <summary>
    /// 可通知的主題感知介面（進階版）
    /// </summary>
    /// <remarks>
    /// 除了接收主題變更通知外，還可以在主題即將變更前進行處理
    /// </remarks>
    public interface INotifyThemeAware : IThemeAware
    {
        /// <summary>
        /// 主題即將變更前的回呼方法
        /// </summary>
        /// <param name="e">主題變更事件參數</param>
        /// <returns>是否允許變更（返回 false 可阻止主題切換）</returns>
        /// <remarks>
        /// 可用於資源清理或狀態保存
        /// </remarks>
        bool OnThemeChanging(ThemeChangedEventArgs e);
    }
}
