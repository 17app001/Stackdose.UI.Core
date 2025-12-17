using System;
using System.Windows;
using Stackdose.UI.Core.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PlcLabel 上下文管理 (PlcLabel Context Manager)
    /// 用途：統一管理所有 PlcLabel 的值變更事件，類似 SensorContext
    /// </summary>
    public static class PlcLabelContext
    {
        #region 靜態屬性

        /// <summary>
        /// 已註冊的 PlcLabel 清單（用於自動監控）
        /// </summary>
        private static readonly HashSet<WeakReference<PlcLabel>> _registeredLabels = new HashSet<WeakReference<PlcLabel>>();
        private static readonly object _lock = new object();

        #endregion

        #region 事件定義

        /// <summary>
        /// PlcLabel 值變更事件 (當任何 PlcLabel 的值變更時觸發)
        /// </summary>
        public static event EventHandler<PlcLabelValueChangedEventArgs>? ValueChanged;

        #endregion

        #region 註冊管理

        /// <summary>
        /// 註冊 PlcLabel 到上下文（由 PlcLabel 控制項呼叫）
        /// </summary>
        public static void Register(PlcLabel label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.Address))
                return;

            lock (_lock)
            {
                // 清理已回收的弱引用
                _registeredLabels.RemoveWhere(wr => !wr.TryGetTarget(out _));

                // 避免重複註冊
                if (!_registeredLabels.Any(wr => wr.TryGetTarget(out var l) && ReferenceEquals(l, label)))
                {
                    _registeredLabels.Add(new WeakReference<PlcLabel>(label));
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] Registered: {label.Address}");
                    #endif
                }
            }
        }

        /// <summary>
        /// 註銷 PlcLabel（由 PlcLabel 控制項呼叫）
        /// </summary>
        public static void Unregister(PlcLabel label)
        {
            if (label == null)
                return;

            lock (_lock)
            {
                _registeredLabels.RemoveWhere(wr => 
                {
                    if (wr.TryGetTarget(out var l))
                        return ReferenceEquals(l, label);
                    return true; // 清理已回收的引用
                });

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] Unregistered: {label.Address}");
                #endif
            }
        }

        /// <summary>
        /// ?? 從已註冊的 PlcLabel 中智慧提取監控位址
        /// 自動合併連續位址（例如 D90, D91, D92 → D90,3）
        /// </summary>
        /// <returns>監控位址字串（例如 "D90,3,M100,1,X10,1"）</returns>
        public static string GenerateMonitorAddresses()
        {
            lock (_lock)
            {
                // 清理已回收的弱引用
                _registeredLabels.RemoveWhere(wr => !wr.TryGetTarget(out _));

                var addresses = new List<string>();
                foreach (var weakRef in _registeredLabels)
                {
                    if (weakRef.TryGetTarget(out var label) && !string.IsNullOrWhiteSpace(label.Address))
                    {
                        addresses.Add(label.Address.Trim().ToUpper());
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
        /// 智慧合併連續位址（類似 SensorContext）
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
