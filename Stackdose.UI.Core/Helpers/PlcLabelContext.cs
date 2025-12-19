using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stackdose.UI.Core.Controls;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PlcLabel 上下文管理器
    /// </summary>
    /// <remarks>
    /// <para>提供全域 PlcLabel 控制項管理功能：</para>
    /// <list type="bullet">
    /// <item>自動註冊與註銷 PlcLabel 實例</item>
    /// <item>主題變化時統一通知所有 PlcLabel 更新</item>
    /// <item>智慧合併連續 PLC 位址以優化監控效率</item>
    /// <item>使用 WeakReference 避免記憶體洩漏</item>
    /// <item>執行緒安全的註冊與通知機制</item>
    /// </list>
    /// </remarks>
    public static class PlcLabelContext
    {
        #region Private Fields

        /// <summary>已註冊的 PlcLabel 弱引用集合</summary>
        /// <remarks>使用 WeakReference 避免阻止 GC 回收已不使用的控制項</remarks>
        private static readonly HashSet<WeakReference<PlcLabel>> _registeredLabels = new();
        
        /// <summary>執行緒鎖定物件</summary>
        private static readonly object _lock = new();

        #endregion

        #region Events

        /// <summary>
        /// PlcLabel 值變更事件
        /// </summary>
        /// <remarks>
        /// 當任何註冊的 PlcLabel 的值發生變更時觸發
        /// 可用於全域監控或記錄所有 PLC 數據變化
        /// </remarks>
        public static event EventHandler<PlcLabelValueChangedEventArgs>? ValueChanged;

        #endregion

        #region Registration Management

        /// <summary>
        /// 註冊 PlcLabel 到上下文
        /// </summary>
        /// <param name="label">要註冊的 PlcLabel 實例</param>
        /// <remarks>
        /// <para>由 PlcLabel 的 Loaded 事件自動呼叫</para>
        /// <para>使用 WeakReference 儲存，不影響 GC 回收</para>
        /// <para>執行緒安全</para>
        /// </remarks>
        public static void Register(PlcLabel label)
        {
            if (label == null || string.IsNullOrWhiteSpace(label.Address))
                return;

            lock (_lock)
            {
                // 清理已被 GC 回收的引用
                CleanupDeadReferences();

                // 避免重複註冊相同實例
                if (!IsAlreadyRegistered(label))
                {
                    _registeredLabels.Add(new WeakReference<PlcLabel>(label));
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] ? Registered: {label.Address} (Total: {_registeredLabels.Count})");
                    #endif
                }
            }
        }

        /// <summary>
        /// 註銷 PlcLabel
        /// </summary>
        /// <param name="label">要註銷的 PlcLabel 實例</param>
        /// <remarks>
        /// 由 PlcLabel 的 Unloaded 事件自動呼叫
        /// </remarks>
        public static void Unregister(PlcLabel label)
        {
            if (label == null)
                return;

            lock (_lock)
            {
                int removed = _registeredLabels.RemoveWhere(wr => 
                {
                    if (wr.TryGetTarget(out var l))
                        return ReferenceEquals(l, label);
                    return true; // 同時清理已回收的引用
                });

                #if DEBUG
                if (removed > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] ? Unregistered: {label.Address} (Remaining: {_registeredLabels.Count})");
                }
                #endif
            }
        }

        /// <summary>
        /// 檢查指定 PlcLabel 是否已註冊
        /// </summary>
        private static bool IsAlreadyRegistered(PlcLabel label)
        {
            return _registeredLabels.Any(wr => wr.TryGetTarget(out var l) && ReferenceEquals(l, label));
        }

        /// <summary>
        /// 清理已被 GC 回收的弱引用
        /// </summary>
        private static void CleanupDeadReferences()
        {
            _registeredLabels.RemoveWhere(wr => !wr.TryGetTarget(out _));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 通知 PlcLabel 值已變更
        /// </summary>
        /// <param name="label">觸發的 PlcLabel</param>
        /// <param name="value">新的值</param>
        /// <remarks>
        /// 由 PlcLabel 控制項內部在數值更新時呼叫
        /// 在 UI 執行緒上觸發事件確保執行緒安全
        /// </remarks>
        public static void NotifyValueChanged(PlcLabel label, object value)
        {
            if (label == null || value == null)
                return;

            // 在 UI 執行緒上觸發事件
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                ValueChanged?.Invoke(null, new PlcLabelValueChangedEventArgs(label, value));
            });

            LogValueChange(label, value);
        }

        /// <summary>
        /// 從已註冊的 PlcLabel 中智慧提取監控位址
        /// </summary>
        /// <returns>優化後的監控位址字串（例如 "D90,3,M100,1,X10,1"）</returns>
        /// <remarks>
        /// <para>自動合併連續位址以優化監控效率</para>
        /// <para>例如：D90, D91, D92 會合併為 D90,3</para>
        /// <para>DWord 類型會自動註冊兩個連續 Word（例如 D65 → D65, D66）</para>
        /// </remarks>
        public static string GenerateMonitorAddresses()
        {
            lock (_lock)
            {
                // 清理已回收的弱引用
                CleanupDeadReferences();

                var addresses = new List<string>();
                foreach (var weakRef in _registeredLabels)
                {
                    if (weakRef.TryGetTarget(out var label) && !string.IsNullOrWhiteSpace(label.Address))
                    {
                        string baseAddr = label.Address.Trim().ToUpper();
                        addresses.Add(baseAddr);
                        
                        // ?? 如果是 DWord 類型，自動加入下一個位址
                        if (label.DataType == Controls.PlcDataType.DWord)
                        {
                            // 解析位址（例如 D65 → D66）
                            var match = System.Text.RegularExpressions.Regex.Match(baseAddr, @"^([A-Z]+)(\d+)$");
                            if (match.Success)
                            {
                                string deviceType = match.Groups[1].Value;  // D, M, R 等
                                int deviceNumber = int.Parse(match.Groups[2].Value);
                                string nextAddr = $"{deviceType}{deviceNumber + 1}";
                                addresses.Add(nextAddr);
                                
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] DWord 自動註冊: {baseAddr} + {nextAddr}");
                                #endif
                            }
                        }
                    }
                }

                return addresses.Count == 0 ? string.Empty : GenerateOptimizedAddresses(addresses);
            }
        }

        #endregion

        #region Private Methods

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

        #region 主題變化通知

        /// <summary>
        /// 通知所有 PlcLabel 主題已變化
        /// </summary>
        public static void NotifyThemeChanged()
        {
            lock (_lock)
            {
                // 清理已回收的引用
                _registeredLabels.RemoveWhere(wr => !wr.TryGetTarget(out _));

                System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] 通知 {_registeredLabels.Count} 個 PlcLabel 主題已變化");

                foreach (var weakRef in _registeredLabels.ToList())
                {
                    if (weakRef.TryGetTarget(out var label))
                    {
                        try
                        {
                            label.OnThemeChanged();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PlcLabelContext] 更新 PlcLabel 失敗: {ex.Message}");
                        }
                    }
                }
            }
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
