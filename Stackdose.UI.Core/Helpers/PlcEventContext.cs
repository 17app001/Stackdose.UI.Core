using System;
using System.Windows;
using Stackdose.UI.Core.Controls;
using System.Collections.Generic;
using System.Linq;
using Stackdose.Abstractions.Logging;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PlcEventTrigger 上下文管理 (PlcEvent Context Manager)
    /// 用途：統一管理所有 PlcEventTrigger 的觸發事件
    /// 類似 SensorContext 的設計，用於處理 PLC 事件觸發（如 M237, M238）
    /// </summary>
    public static class PlcEventContext
    {
        #region 靜態屬性

        /// <summary>
        /// 已註冊的 PlcEventTrigger 清單（用於自動監控）
        /// </summary>
        private static readonly HashSet<WeakReference<PlcEventTrigger>> _registeredTriggers = new HashSet<WeakReference<PlcEventTrigger>>();
        private static readonly object _lock = new object();

        #endregion

        #region 事件定義

        /// <summary>
        /// 事件觸發事件 (當 PlcEventTrigger 偵測到條件滿足時觸發)
        /// </summary>
        public static event EventHandler<PlcEventTriggeredEventArgs>? EventTriggered;

        #endregion

        #region 註冊管理

        /// <summary>
        /// 註冊 PlcEventTrigger 到上下文（由 PlcEventTrigger 控制項呼叫）
        /// </summary>
        public static void Register(PlcEventTrigger trigger)
        {
            if (trigger == null || string.IsNullOrWhiteSpace(trigger.Address))
                return;

            lock (_lock)
            {
                // 清理已回收的弱引用
                _registeredTriggers.RemoveWhere(wr => !wr.TryGetTarget(out _));

                // 避免重複註冊
                if (!_registeredTriggers.Any(wr => wr.TryGetTarget(out var t) && ReferenceEquals(t, trigger)))
                {
                    _registeredTriggers.Add(new WeakReference<PlcEventTrigger>(trigger));
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcEventContext] Registered: {trigger.EventName} ({trigger.Address})");
                    #endif
                }
            }
        }

        /// <summary>
        /// 註銷 PlcEventTrigger（由 PlcEventTrigger 控制項呼叫）
        /// </summary>
        public static void Unregister(PlcEventTrigger trigger)
        {
            if (trigger == null)
                return;

            lock (_lock)
            {
                _registeredTriggers.RemoveWhere(wr => 
                {
                    if (wr.TryGetTarget(out var t))
                        return ReferenceEquals(t, trigger);
                    return true; // 清理已回收的引用
                });

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlcEventContext] Unregistered: {trigger.EventName} ({trigger.Address})");
                #endif
            }
        }

        /// <summary>
        /// ?? 從已註冊的 PlcEventTrigger 中智慧提取監控位址
        /// 自動合併連續位址（例如 M237, M238, M239 → M237,3）
        /// </summary>
        /// <returns>監控位址字串（例如 "M237,3,M400,1"）</returns>
        public static string GenerateMonitorAddresses()
        {
            lock (_lock)
            {
                // 清理已回收的弱引用
                _registeredTriggers.RemoveWhere(wr => !wr.TryGetTarget(out _));

                var addresses = new List<string>();
                foreach (var weakRef in _registeredTriggers)
                {
                    if (weakRef.TryGetTarget(out var trigger) && !string.IsNullOrWhiteSpace(trigger.Address))
                    {
                        addresses.Add(trigger.Address.Trim().ToUpper());
                    }
                }

                if (addresses.Count == 0)
                    return string.Empty;

                return GenerateOptimizedAddresses(addresses);
            }
        }

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
        /// 智慧合併連續位址（類似 PlcLabelContext）
        /// </summary>
        private static string GenerateOptimizedAddresses(List<string> addresses)
        {
            var addressGroups = new Dictionary<string, List<int>>();

            // 1. 解析並分組位址
            foreach (var address in addresses)
            {
                if (System.Text.RegularExpressions.Regex.Match(address, @"^([A-Z]+)(\d+)$") is var match && match.Success)
                {
                    string deviceType = match.Groups[1].Value; // D, M, X, Y, R
                    int deviceNumber = int.Parse(match.Groups[2].Value);

                    if (!addressGroups.ContainsKey(deviceType))
                    {
                        addressGroups[deviceType] = new List<int>();
                    }

                    if (!addressGroups[deviceType].Contains(deviceNumber))
                    {
                        addressGroups[deviceType].Add(deviceNumber);
                    }
                }
            }

            // 2. 智慧合併連續位址
            var monitorParts = new List<string>();

            foreach (var group in addressGroups.OrderBy(g => g.Key))
            {
                string deviceType = group.Key;
                var numbers = group.Value.OrderBy(n => n).ToList();

                int i = 0;
                while (i < numbers.Count)
                {
                    int start = numbers[i];
                    int end = start;

                    // 找出連續範圍
                    while (i + 1 < numbers.Count && numbers[i + 1] == end + 1)
                    {
                        i++;
                        end = numbers[i];
                    }

                    int length = end - start + 1;

                    // 連續 2 個以上就批次合併
                    if (length >= 2)
                    {
                        monitorParts.Add($"{deviceType}{start},{length}");
                    }
                    else
                    {
                        monitorParts.Add($"{deviceType}{start},1");
                    }

                    i++;
                }
            }

            return string.Join(",", monitorParts);
        }

        /// <summary>
        /// 記錄事件觸發日誌
        /// </summary>
        private static void LogEventTrigger(PlcEventTrigger trigger, object value)
        {
            ComplianceContext.LogSystem(
                $"[PlcEvent] {trigger.EventName} ({trigger.Address}) = {value}",
                LogLevel.Info,
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
