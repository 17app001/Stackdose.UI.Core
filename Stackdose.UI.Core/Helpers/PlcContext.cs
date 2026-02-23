using System.Windows;
using Stackdose.UI.Core.Controls;

namespace Stackdose.UI.Core.Helpers
{
    public static class PlcContext
    {
        // 1. 全域靜態參考 (懶人模式核心)
        // 當 PlcStatus 設定 IsGlobal="True" 時，會自動填入這裡
        private static PlcStatus? _globalStatus;

        public static event EventHandler<PlcStatus?>? GlobalStatusChanged;

        public static PlcStatus? GlobalStatus
        {
            get => _globalStatus;
            set
            {
                if (ReferenceEquals(_globalStatus, value))
                {
                    return;
                }

                _globalStatus = value;
                GlobalStatusChanged?.Invoke(null, _globalStatus);
            }
        }

        // 2. 區域繼承屬性 (原本的功能)
        // 讓子控制項可以自動讀取到父容器設定的值
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.RegisterAttached(
                "Status",
                typeof(PlcStatus),
                typeof(PlcContext),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetStatus(DependencyObject element, PlcStatus value)
        {
            element.SetValue(StatusProperty, value);
        }

        public static PlcStatus GetStatus(DependencyObject element)
        {
            return (PlcStatus)element.GetValue(StatusProperty);
        }
    }
}
