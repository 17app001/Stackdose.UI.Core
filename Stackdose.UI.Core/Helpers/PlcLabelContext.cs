using System;
using System.Windows;
using Stackdose.UI.Core.Controls;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PlcLabel 上下文管理 (PlcLabel Context Manager)
    /// 用途：統一管理所有 PlcLabel 的值變更事件，類似 SensorContext
    /// </summary>
    public static class PlcLabelContext
    {
        #region 事件定義

        /// <summary>
        /// PlcLabel 值變更事件 (當任何 PlcLabel 的值變更時觸發)
        /// </summary>
        public static event EventHandler<PlcLabelValueChangedEventArgs>? ValueChanged;

        #endregion

        #region 公開方法

        /// <summary>
        /// 通知 PlcLabel 值已變更（由 PlcLabel 控制項內部呼叫）
        /// </summary>
        /// <param name="label">觸發的 PlcLabel</param>
        /// <param name="value">新的值</param>
        public static void NotifyValueChanged(PlcLabel label, object value)
        {
            if (label == null || value == null)
                return;

            // ?? 在 UI 執行緒上觸發事件（確保執行緒安全，類似 SensorContext）
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                ValueChanged?.Invoke(null, new PlcLabelValueChangedEventArgs(label, value));
            });

            // 記錄日誌（可選，用於除錯）
            LogValueChange(label, value);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 記錄值變更日誌（用於除錯）
        /// </summary>
        private static void LogValueChange(PlcLabel label, object value)
        {
            // 只在需要時記錄（避免過多日誌）
            #if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[PlcLabelContext] {label.Label} ({label.Address}) = {value}"
            );
            #endif
        }

        #endregion
    }

    #region 事件參數

    /// <summary>
    /// PlcLabel 值變更事件參數
    /// </summary>
    public class PlcLabelValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 觸發事件的 PlcLabel
        /// </summary>
        public PlcLabel PlcLabel { get; }

        /// <summary>
        /// 新的值（原始值）
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// PlcLabel 的位址（快速存取）
        /// </summary>
        public string Address => PlcLabel.Address;

        /// <summary>
        /// PlcLabel 的標籤文字（快速存取）
        /// </summary>
        public string Label => PlcLabel.Label;

        public PlcLabelValueChangedEventArgs(PlcLabel label, object value)
        {
            PlcLabel = label ?? throw new ArgumentNullException(nameof(label));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}
