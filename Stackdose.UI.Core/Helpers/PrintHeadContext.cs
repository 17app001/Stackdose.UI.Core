using System;
using System.Collections.Generic;
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
        public static Dictionary<string, dynamic> ConnectedPrintHeads { get; } = new Dictionary<string, dynamic>();

        /// <summary>
        /// 主要 PrintHead（預設）
        /// </summary>
        public static dynamic? MainPrintHead { get; set; }

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
        /// <param name="name">PrintHead 名稱</param>
        /// <param name="printHead">PrintHead 實例</param>
        public static void RegisterPrintHead(string name, dynamic printHead)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("PrintHead name cannot be null or empty", nameof(name));
            }

            if (printHead == null)
            {
                throw new ArgumentNullException(nameof(printHead));
            }

            if (ConnectedPrintHeads.ContainsKey(name))
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadContext] PrintHead '{name}' already registered, updating instance",
                    LogLevel.Warning,
                    showInUi: false
                );
            }

            ConnectedPrintHeads[name] = printHead;

            // 如果是第一個註冊的 PrintHead，設為主要 PrintHead
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
        /// <param name="name">PrintHead 名稱</param>
        public static void UnregisterPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (ConnectedPrintHeads.Remove(name))
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadContext] PrintHead unregistered: {name}",
                    LogLevel.Info,
                    showInUi: false
                );

                // 如果移除的是主要 PrintHead，選擇第一個可用的作為新的主要 PrintHead
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
        /// <param name="name">PrintHead 名稱</param>
        /// <returns>PrintHead 實例，如果不存在則返回 null</returns>
        public static dynamic? GetPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return ConnectedPrintHeads.TryGetValue(name, out var printHead) ? printHead : null;
        }

        /// <summary>
        /// 設定主要 PrintHead
        /// </summary>
        /// <param name="name">PrintHead 名稱</param>
        public static bool SetMainPrintHead(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

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
        /// 取得所有已連線的 PrintHead 清單
        /// </summary>
        /// <returns>包含所有已連線 PrintHead 的字典</returns>
        public static Dictionary<string, dynamic> GetAllConnectedPrintHeads()
        {
            return new Dictionary<string, dynamic>(ConnectedPrintHeads);
        }

        #endregion

        #region 狀態報告

        /// <summary>
        /// 通知 PrintHead 狀態變更
        /// </summary>
        /// <param name="name">PrintHead 名稱</param>
        /// <param name="status">狀態描述</param>
        public static void NotifyStatusChanged(string name, string status)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(status))
            {
                return;
            }

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
        /// <returns>狀態摘要字串</returns>
        public static string GetStatusSummary()
        {
            if (ConnectedPrintHeads.Count == 0)
            {
                return "No PrintHeads connected";
            }

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
        /// 清除所有 PrintHead 註冊
        /// </summary>
        public static void ClearAll()
        {
            ConnectedPrintHeads.Clear();
            MainPrintHead = null;
            CurrentPrintHeadName = null;

            ComplianceContext.LogSystem(
                "[PrintHeadContext] All PrintHeads cleared",
                LogLevel.Info,
                showInUi: false
            );
        }

        #endregion
    }
}
