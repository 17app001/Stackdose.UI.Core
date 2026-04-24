using System;
using System.Collections.Generic;
using Stackdose.Abstractions.Logging;
using Stackdose.Abstractions.Print;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// PrintHead 全域上下文管理器
    /// 用途：管理多個 PrintHead 實例的全域狀態
    /// </summary>
    public static class PrintHeadContext
    {
        #region 靜態屬性

        /// <summary>
        /// 所有已連線的 PrintHead 集合
        /// </summary>
        public static Dictionary<string, IPrintHead> ConnectedPrintHeads { get; } = new Dictionary<string, IPrintHead>();

        /// <summary>
        /// 主要 PrintHead（預設）
        /// </summary>
        public static IPrintHead? MainPrintHead { get; set; }

        /// <summary>
        /// 當前選中的 PrintHead 名稱
        /// </summary>
        public static string? CurrentPrintHeadName { get; set; }

        /// <summary>
        /// 是否有任何 PrintHead 已連線
        /// </summary>
        public static bool HasConnectedPrintHead => ConnectedPrintHeads.Count > 0;

        #endregion

        #region 事件定義

        /// <summary>
        /// PrintHead 連線成功事件
        /// </summary>
        public static event Action<string>? PrintHeadConnected;

        /// <summary>
        /// PrintHead 斷線事件
        /// </summary>
        public static event Action<string>? PrintHeadDisconnected;

        /// <summary>
        /// PrintHead 狀態變更事件
        /// </summary>
        public static event Action<string, string>? PrintHeadStatusChanged;

        #endregion

        #region PrintHead 管理

        /// <summary>
        /// 註冊 PrintHead 實例
        /// </summary>
        public static void RegisterPrintHead(string name, IPrintHead printHead)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("PrintHead name cannot be null or empty", nameof(name));

            if (printHead == null)
                throw new ArgumentNullException(nameof(printHead));

            if (ConnectedPrintHeads.ContainsKey(name))
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadContext] PrintHead '{name}' already registered, updating instance",
                    LogLevel.Warning,
                    showInUi: false
                );
            }

            ConnectedPrintHeads[name] = printHead;

            if (MainPrintHead == null)
            {
                MainPrintHead = printHead;
                CurrentPrintHeadName = name;
            }

            ComplianceContext.LogSystem(
                $"[PrintHeadContext] PrintHead registered: {name}",
                LogLevel.Info,
                showInUi: false
            );

            PrintHeadConnected?.Invoke(name);
        }

        /// <summary>
        /// 取消註冊 PrintHead 實例
        /// </summary>
        public static void UnregisterPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (ConnectedPrintHeads.Remove(name))
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadContext] PrintHead unregistered: {name}",
                    LogLevel.Info,
                    showInUi: false
                );

                if (CurrentPrintHeadName == name)
                {
                    if (ConnectedPrintHeads.Count > 0)
                    {
                        var firstKey = new List<string>(ConnectedPrintHeads.Keys)[0];
                        MainPrintHead = ConnectedPrintHeads[firstKey];
                        CurrentPrintHeadName = firstKey;
                    }
                    else
                    {
                        MainPrintHead = null;
                        CurrentPrintHeadName = null;
                    }
                }

                PrintHeadDisconnected?.Invoke(name);
            }
        }

        /// <summary>
        /// 取得指定名稱的 PrintHead 實例
        /// </summary>
        public static IPrintHead? GetPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return ConnectedPrintHeads.TryGetValue(name, out var printHead) ? printHead : null;
        }

        /// <summary>
        /// 設定主要 PrintHead
        /// </summary>
        public static bool SetMainPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var printHead = GetPrintHead(name);
            if (printHead != null)
            {
                MainPrintHead = printHead;
                CurrentPrintHeadName = name;

                ComplianceContext.LogSystem(
                    $"[PrintHeadContext] Main PrintHead changed to: {name}",
                    LogLevel.Info,
                    showInUi: false
                );

                return true;
            }

            return false;
        }

        /// <summary>
        /// 取得所有已連線的 PrintHead 清單（快照）
        /// </summary>
        public static Dictionary<string, IPrintHead> GetAllConnectedPrintHeads()
        {
            return new Dictionary<string, IPrintHead>(ConnectedPrintHeads);
        }

        #endregion

        #region 狀態報告

        /// <summary>
        /// 通知 PrintHead 狀態變更
        /// </summary>
        public static void NotifyStatusChanged(string name, string status)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(status))
                return;

            ComplianceContext.LogSystem(
                $"[PrintHeadContext] {name}: {status}",
                LogLevel.Info,
                showInUi: false
            );

            PrintHeadStatusChanged?.Invoke(name, status);
        }

        /// <summary>
        /// 取得所有 PrintHead 的狀態摘要
        /// </summary>
        public static string GetStatusSummary()
        {
            if (ConnectedPrintHeads.Count == 0)
                return "No PrintHeads connected";

            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Connected PrintHeads: {ConnectedPrintHeads.Count}");

            foreach (var kvp in ConnectedPrintHeads)
            {
                string mainIndicator = CurrentPrintHeadName == kvp.Key ? " [MAIN]" : "";
                summary.AppendLine($"  - {kvp.Key}{mainIndicator}");
            }

            return summary.ToString();
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清除所有 PrintHead 註冊並廣播斷線事件
        /// </summary>
        public static void ClearAll()
        {
            var names = new List<string>(ConnectedPrintHeads.Keys);
            ConnectedPrintHeads.Clear();
            MainPrintHead = null;
            CurrentPrintHeadName = null;

            foreach (var name in names)
                PrintHeadDisconnected?.Invoke(name);

            ComplianceContext.LogSystem(
                "[PrintHeadContext] All PrintHeads cleared",
                LogLevel.Info,
                showInUi: false
            );
        }

        #endregion
    }
}
