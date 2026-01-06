using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 即時日誌檢視器控制項
    /// </summary>
    /// <remarks>
    /// <para>提供即時系統日誌/稽核軌跡顯示功能：</para>
    /// <list type="bullet">
    /// <item>即時顯示系統事件與操作記錄</item>
    /// <item>支援不同等級的日誌顯示（Info/Warning/Error/Success）</item>
    /// <item>自動捲動到最新日誌</item>
    /// <item>Dark/Light 主題自動適應（實作 IThemeAware）</item>
    /// <item>整合 ComplianceContext 即時日誌來源</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法（使用預設日誌來源）：
    /// <code>
    /// &lt;Custom:LiveLogViewer /&gt;
    /// </code>
    /// 自訂日誌來源：
    /// <code>
    /// &lt;Custom:LiveLogViewer Source="{Binding CustomLogs}" /&gt;
    /// </code>
    /// </example>
    public partial class LiveLogViewer : UserControl, IThemeAware
    {
        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        public LiveLogViewer()
        {
            InitializeComponent();
            
            // 預設綁定到全域即時日誌
            this.Source = ComplianceContext.LiveLogs;
            
            this.Loaded += LiveLogViewer_Loaded;
            this.Unloaded += LiveLogViewer_Unloaded;
        }

        #endregion

        #region IThemeAware Implementation

        /// <summary>
        /// 主題變更時的回呼方法（實作 IThemeAware）
        /// </summary>
        /// <param name="e">主題變更事件參數</param>
        public void OnThemeChanged(ThemeChangedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LiveLogViewer] OnThemeChanged: {e.ThemeName} ({(e.IsLightTheme ? "Light" : "Dark")})");
            #endif
            
            // 刷新所有日誌項目顏色
            RefreshLogColors();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 控制項載入完成事件
        /// </summary>
        private void LiveLogViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // 🔥 註冊到 ThemeManager（自動接收主題變更通知）
            ThemeManager.Register(this);
        }

        /// <summary>
        /// 控制項卸載事件
        /// </summary>
        private void LiveLogViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            // 🔥 註銷 ThemeManager（WeakReference 會自動處理，但手動註銷更安全）
            ThemeManager.Unregister(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 主題變化時強制刷新所有日誌項目顏色
        /// </summary>
        /// <remarks>
        /// 透過重新綁定 ItemsSource 觸發所有 LogEntry.Color 屬性重新計算
        /// </remarks>
        public void RefreshLogColors()
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LiveLogViewer] 刷新日誌顏色");
            #endif
            
            if (LogList?.ItemsSource is not IEnumerable items)
                return;

            try
            {
                // 強制 ListView 重新繪製所有項目
                LogList.ItemsSource = null;
                LogList.ItemsSource = items;
                
                // 捲動到最後一項
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LiveLogViewer] 刷新失敗: {ex.Message}");
                #endif
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 捲動到最後一個日誌項目
        /// </summary>
        private void ScrollToBottom()
        {
            if (LogList != null && LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            }
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 日誌資料來源依賴屬性
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source), 
                typeof(IEnumerable), 
                typeof(LiveLogViewer),
                new PropertyMetadata(null, OnSourceChanged));

        /// <summary>
        /// 取得或設定日誌資料來源
        /// </summary>
        /// <remarks>
        /// 預設為 ComplianceContext.LiveLogs
        /// 支援任何實作 IEnumerable 的集合
        /// 若集合實作 INotifyCollectionChanged，會自動捲動到最新日誌
        /// </remarks>
        public IEnumerable Source
        {
            get => (IEnumerable)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Source 屬性變更回呼
        /// </summary>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LiveLogViewer control)
                return;

            // 綁定新的資料來源
            control.LogList.ItemsSource = e.NewValue as IEnumerable;

            // 訂閱集合變更事件以自動捲動
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += (s, args) =>
                {
                    // 當有新日誌加入時，自動捲動到底部
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        control.Dispatcher.InvokeAsync(() =>
                        {
                            control.ScrollToBottom();
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }
                };
            }
        }

        #endregion
    }
}