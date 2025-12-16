using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Data;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 法規合規引擎 (Compliance Engine)
    /// 用途：統一處理符合 FDA 21 CFR Part 11 的電子紀錄與審計軌跡
    /// </summary>
    public static class ComplianceContext
    {
        // 模擬當前登入使用者 (在實際專案中，這裡應串接您的登入系統/權限管理)
        public static string CurrentUser { get; set; } = "Operator_A";
        // 1. 建立一個可綁定的集合 (這就是我們要綁定給 UI 的源頭)
        public static ObservableCollection<LogEntry> LiveLogs { get; } = new ObservableCollection<LogEntry>();

        private static object _lock = new object();

        // 靜態建構子：確保程式一啟動或第一次使用時，資料庫已準備就緒
        static ComplianceContext()
        {
            BindingOperations.EnableCollectionSynchronization(LiveLogs, _lock);
            try
            {
                SqliteLogger.Initialize();
            }
            catch (Exception ex)
            {
                // 在實際生產環境，這裡應該寫入系統事件檢視器或發出嚴重警報
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] Compliance Engine Init Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 記錄審計軌跡 (Audit Trail) - 用於參數修改、操作紀錄
        /// </summary>
        /// <param name="deviceName">裝置/參數名稱 (如 "加熱器溫度")</param>
        /// <param name="address">PLC 位址 (如 "D100")</param>
        /// <param name="oldValue">修改前的值</param>
        /// <param name="newValue">修改後的值</param>
        /// <param name="reason">修改原因 (預設為手動操作)</param>
        public static void LogAuditTrail(string deviceName, string address, string oldValue, string newValue, string reason = "Manual Operation", bool showInUi = true)
        {
            // 🔥 修正：必須將 reason 參數傳遞給 LogAudit，確保紀錄完整性
            SqliteLogger.LogAudit(CurrentUser, "WRITE", $"{deviceName}({address})", oldValue, newValue, reason);
            // 2. 🔥 新增：發送到 UI 顯示
            // B. 🔥 新增：寫入即時集合 (UI 綁定用)

            AddToLiveLog($"[Audit] {deviceName} ({address}) : {oldValue} -> {newValue}", LogLevel.Warning, showInUi);

        }

        // 您也可以加一個通用的 LogSystem 方法
        public static void LogSystem(string message, LogLevel level = LogLevel.Info, bool showInUi = true)
        {
            AddToLiveLog(message, level, showInUi);
        }

        /// <summary>
        /// 記錄生產數據 (Data History) - 用於生產履歷、趨勢分析
        /// </summary>
        /// <param name="labelName">數據名稱</param>
        /// <param name="address">PLC 位址</param>
        /// <param name="value">數值</param>
        public static void LogDataHistory(string labelName, string address, string value)
        {
            SqliteLogger.LogData(labelName, address, value);
        }

        private static void AddToLiveLog(string msg, LogLevel level, bool showInUi = true)
        {
            if (!showInUi) return;
            lock (_lock)
            {
                // 因為是 ObservableCollection，一 Add 畫面就會自動更新
                LiveLogs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = msg,
                    Level = level
                });

                // 限制只留最新的 100 筆
                if (LiveLogs.Count > 100) LiveLogs.RemoveAt(0);
            }
        }
    }
}