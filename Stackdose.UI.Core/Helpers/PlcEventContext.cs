using System;
using System.Windows;
using Stackdose.UI.Core.Controls;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PlcEventTrigger 上下文管理 (PlcEvent Context Manager)
    /// 用途：統一管理所有 PlcEventTrigger 的觸發事件
    /// 類似 SensorContext 的設計，用於處理 PLC 事件觸發（如 M237, M238）
    /// </summary>
    public static class PlcEventContext
    {
        #region 事件定義

        /// <summary>
        /// 事件觸發事件 (當 PlcEventTrigger 偵測到條件滿足時觸發)
        /// </summary>
        public static event EventHandler<PlcEventTriggeredEventArgs>? EventTriggered;

        #endregion

        #region 公開方法

        /// <summary>
        /// 通知事件已觸發（由 PlcEventTrigger 控制項內部呼叫）
        /// ?? 使用同步 Invoke，確保事件處理完成後才返回
        /// </summary>
        /// <param name="trigger">觸發的 PlcEventTrigger</param>
        /// <param name="value">觸發時的值</param>
        public static void NotifyEventTriggered(PlcEventTrigger trigger, object value)
        {
            if (trigger == null)
                return;

            // ?? 改用同步 Invoke（阻塞等待），確保事件處理完成
            Application.Current?.Dispatcher.Invoke(() =>
            {
                EventTriggered?.Invoke(null, new PlcEventTriggeredEventArgs(trigger, value));
            });

            // 記錄日誌
            LogEventTrigger(trigger, value);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 記錄事件觸發日誌
        /// </summary>
        private static void LogEventTrigger(PlcEventTrigger trigger, object value)
        {
            ComplianceContext.LogSystem(
                $"[PlcEvent] {trigger.EventName} ({trigger.Address}) = {value}",
                Models.LogLevel.Info,
                showInUi: false
            );
        }

        #endregion
    }

    #region 事件參數

    /// <summary>
    /// PlcEvent 觸發事件參數
    /// </summary>
    public class PlcEventTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// 觸發事件的 PlcEventTrigger
        /// </summary>
        public PlcEventTrigger Trigger { get; }

        /// <summary>
        /// 觸發時的值
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// PLC 位址（快速存取）
        /// </summary>
        public string Address => Trigger.Address;

        /// <summary>
        /// 事件名稱（快速存取）
        /// </summary>
        public string EventName => Trigger.EventName;

        public PlcEventTriggeredEventArgs(PlcEventTrigger trigger, object value)
        {
            Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}
