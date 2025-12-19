using System.Collections;
using System.Collections.Specialized; // 用於監聽集合變動以自動捲動
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls
{
    public partial class LiveLogViewer : UserControl
    {
        public LiveLogViewer()
        {
            InitializeComponent();
            this.Source = ComplianceContext.LiveLogs;
            
            // 🔥 註冊主題變化通知
            this.Loaded += LiveLogViewer_Loaded;
        }

        private void LiveLogViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // 如果 PlcLabelContext 有全域主題變化事件，可在此訂閱
            // 目前使用手動刷新的方式
        }

        /// <summary>
        /// 主題變化時強制刷新所有日誌項目
        /// </summary>
        public void RefreshLogColors()
        {
            System.Diagnostics.Debug.WriteLine("[LiveLogViewer] 刷新日誌顏色");
            
            // 強制 ListView 重新繪製所有項目
            if (LogList.ItemsSource != null)
            {
                var items = LogList.ItemsSource;
                LogList.ItemsSource = null;
                LogList.ItemsSource = items;
                
                // 捲動到最後一項
                if (LogList.Items.Count > 0)
                {
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                }
            }
        }

        // 定義一個依賴屬性 Source，讓外部可以綁定資料進來
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(IEnumerable), typeof(LiveLogViewer),
                new PropertyMetadata(null, OnSourceChanged));

        public IEnumerable Source
        {
            get { return (IEnumerable)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (LiveLogViewer)d;
            control.LogList.ItemsSource = e.NewValue as IEnumerable;

            // 自動捲動邏輯
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += (s, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        // 🔥 修正重點：把這段包在 Dispatcher.InvokeAsync 裡
                        // 這會告訴 WPF：「請在 UI 執行緒有空的時候，執行這段程式碼」
                        control.Dispatcher.InvokeAsync(() =>
                        {
                            // 這裡面已經回到 UI 執行緒了，可以安全操作 LogList
                            if (control.LogList.Items.Count > 0)
                            {
                                control.LogList.ScrollIntoView(control.LogList.Items[control.LogList.Items.Count - 1]);
                            }
                        });
                    }
                };
            }
        }
    }
}