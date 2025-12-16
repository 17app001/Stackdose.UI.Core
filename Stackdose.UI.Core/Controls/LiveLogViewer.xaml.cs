using Stackdose.UI.Core.Helpers;
using System.Collections;
using System.Collections.Specialized; // 用於監聽集合變動以自動捲動
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Core.Controls
{
    public partial class LiveLogViewer : UserControl
    {
        public LiveLogViewer()
        {
            InitializeComponent();
            this.Source = ComplianceContext.LiveLogs;
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