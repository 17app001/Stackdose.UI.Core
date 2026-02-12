using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Controls.Base
{
    /// <summary>
    /// 可重用控件基類 - 提供通用生命週期管理和主題感知
    /// </summary>
    /// <remarks>
    /// <para>提供的功能：</para>
    /// <list type="bullet">
    /// <item>自動設計時檢測（避免 Designer 崩潰）</item>
    /// <item>統一的 Loaded/Unloaded 生命週期管理</item>
    /// <item>主題感知自動註冊/註銷</item>
    /// <item>線程安全的 Dispatcher 操作</item>
    /// <item>資源清理管理</item>
    /// </list>
    /// 
    /// <para>使用方式：</para>
    /// <code>
    /// public partial class MyControl : CyberControlBase
    /// {
    ///     protected override void OnControlLoaded()
    ///     {
    ///         // 你的初始化邏輯
    ///     }
    ///     
    ///     protected override void OnControlUnloaded()
    ///     {
    ///         // 你的清理邏輯
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class CyberControlBase : UserControl, IThemeAware, IDisposable
    {
        #region Private Fields

        /// <summary>是否已載入</summary>
        private bool _isLoaded = false;

        /// <summary>是否已disposed</summary>
        private bool _disposed = false;

        /// <summary>是否在設計模式下</summary>
        private readonly bool _isInDesignMode;

        #endregion

        #region Constructor

        /// <summary>
        /// 基類建構函數
        /// </summary>
        protected CyberControlBase()
        {
            // 檢測設計模式
            _isInDesignMode = DesignerProperties.GetIsInDesignMode(this);

            // 設計模式下不初始化
            if (_isInDesignMode)
            {
                return;
            }

            // 註冊生命週期事件
            this.Loaded += CyberControlBase_Loaded;
            this.Unloaded += CyberControlBase_Unloaded;
        }

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// 控件載入時的統一處理
        /// </summary>
        private void CyberControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInDesignMode || _isLoaded)
            {
                return;
            }

            _isLoaded = true;

            try
            {
                // 1. 註冊到 ThemeManager
                if (this is IThemeAware)
                {
                    Helpers.ThemeManager.Register(this);
                }

                // 2. 呼叫子類的初始化邏輯
                OnControlLoaded();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Control loaded");
                #endif
            }
            catch (Exception ex)
            {
                OnLoadError(ex);
            }
        }

        /// <summary>
        /// 控件卸載時的統一處理
        /// </summary>
        private void CyberControlBase_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_isInDesignMode || !_isLoaded)
            {
                return;
            }

            try
            {
                // 1. 呼叫子類的清理邏輯
                OnControlUnloaded();

                // 2. 從 ThemeManager 註銷
                if (this is IThemeAware)
                {
                    Helpers.ThemeManager.Unregister(this);
                }

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Control unloaded");
                #endif
            }
            catch (Exception ex)
            {
                OnUnloadError(ex);
            }

            _isLoaded = false;
        }

        /// <summary>
        /// 子類實作：控件載入時的初始化邏輯
        /// </summary>
        protected virtual void OnControlLoaded()
        {
            // 子類覆寫此方法實作自訂初始化邏輯
        }

        /// <summary>
        /// 子類實作：控件卸載時的清理邏輯
        /// </summary>
        protected virtual void OnControlUnloaded()
        {
            // 子類覆寫此方法實作自訂清理邏輯
        }

        /// <summary>
        /// 子類實作：載入時發生錯誤的處理邏輯
        /// </summary>
        protected virtual void OnLoadError(Exception ex)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Load error: {ex.Message}");
            #endif
        }

        /// <summary>
        /// 子類實作：卸載時發生錯誤的處理邏輯
        /// </summary>
        protected virtual void OnUnloadError(Exception ex)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Unload error: {ex.Message}");
            #endif
        }

        #endregion

        #region IThemeAware Implementation

        /// <summary>
        /// 主題變更時的處理（預設空實作）
        /// </summary>
        /// <param name="e">主題變更事件參數</param>
        /// <remarks>
        /// 子類如需處理主題變更，覆寫此方法
        /// </remarks>
        public virtual void OnThemeChanged(ThemeChangedEventArgs e)
        {
            // 子類覆寫此方法實作主題變更邏輯
        }

        #endregion

        #region Dispatcher Helpers

        /// <summary>
        /// 安全執行 UI 操作（自動切換到 UI 執行緒）
        /// </summary>
        /// <param name="action">要執行的操作</param>
        protected void SafeInvoke(Action action)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }

                if (Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] SafeInvoke error: {ex.Message}");
                #endif
            }
        }

        /// <summary>
        /// 安全執行 UI 操作（非同步）
        /// </summary>
        /// <param name="action">要執行的操作</param>
        protected void SafeBeginInvoke(Action action)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }

                Dispatcher.BeginInvoke(action);
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] SafeBeginInvoke error: {ex.Message}");
                #endif
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 釋放資源（可覆寫）
        /// </summary>
        /// <param name="disposing">是否為手動釋放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // 釋放 Managed 資源
                try
                {
                    // 移除事件訂閱
                    this.Loaded -= CyberControlBase_Loaded;
                    this.Unloaded -= CyberControlBase_Unloaded;

                    // 從 ThemeManager 註銷
                    if (this is IThemeAware)
                    {
                        Helpers.ThemeManager.Unregister(this);
                    }

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Disposed");
                    #endif
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Dispose error: {ex.Message}");
                    #endif
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// 解構函數
        /// </summary>
        ~CyberControlBase()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 取得控件是否已載入
        /// </summary>
        protected bool IsControlLoaded => _isLoaded;

        /// <summary>
        /// 取得是否在設計模式下
        /// </summary>
        protected bool IsInDesignMode => _isInDesignMode;

        /// <summary>
        /// 取得控件是否已disposed
        /// </summary>
        protected bool IsDisposed => _disposed;

        #endregion
    }
}
