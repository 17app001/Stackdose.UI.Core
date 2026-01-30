using Stackdose.Abstractions.Logging;
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
    /// <remarks>
    /// <para>效能優化特性：</para>
    /// <list type="bullet">
    /// <item>批次寫入 SQLite（預設 100 筆自動刷新）</item>
    /// <item>定時刷新（預設 5 秒）</item>
    /// <item>執行緒安全的日誌記錄</item>
    /// <item>手動刷新 API</item>
    /// </list>
    /// </remarks>
    public static class ComplianceContext
    {
        #region Private Fields

        // 模擬當前登入使用者 (在實際專案中，這裡應串接您的登入系統/權限管理)
        public static string CurrentUser { get; set; } = "Operator_A";
        
        // 1. 建立一個可綁定的集合 (這就是我們要綁定給 UI 的源頭)
        public static ObservableCollection<LogEntry> LiveLogs { get; } = new ObservableCollection<LogEntry>();

        private static object _lock = new object();

        #endregion

        #region Initialization

        // 靜態建構子：確保程式一啟動或第一次使用時，資料庫已準備就緒
        static ComplianceContext()
        {
            BindingOperations.EnableCollectionSynchronization(LiveLogs, _lock);
            try
            {
                // 🔥 初始化批次寫入模式的 SqliteLogger
                SqliteLogger.Initialize();
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ComplianceContext] 合規引擎已啟動（批次寫入模式）");
                #endif
            }
            catch (Exception ex)
            {
                // 在實際生產環境，這裡應該寫入系統事件檢視器或發出嚴重警報
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] Compliance Engine Init Failed: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 記錄審計軌跡 (Audit Trail) - 用於參數修改、操作紀錄
        /// </summary>
        /// <param name="deviceName">裝置/參數名稱 (如 "加熱器溫度")</param>
        /// <param name="address">PLC 位址 (如 "D100")</param>
        /// <param name="oldValue">修改前的值</param>
        /// <param name="newValue">修改後的值</param>
        /// <param name="reason">修改原因 (預設為手動操作)</param>
        /// <param name="parameter">被變更的參數名稱（選填）</param>
        /// <param name="batchId">批次編號（選填）</param>
        /// <param name="showInUi">是否顯示在 UI 日誌檢視器</param>
        /// <remarks>
        /// 此方法使用批次寫入模式，日誌會先存入佇列，再定期批次寫入資料庫
        /// 符合 FDA 21 CFR Part 11 規範
        /// </remarks>
        public static void LogAuditTrail(string deviceName, string address, string oldValue, string newValue, string reason = "Manual Operation", string parameter = "", string batchId = "", bool showInUi = true)
        {
            // Batch write mode: logs are queued first - now includes parameter and batchId
            SqliteLogger.LogAudit(CurrentUser, "WRITE", $"{deviceName}({address})", oldValue, newValue, reason, parameter, batchId);
            
            // Display in UI
            AddToLiveLog($"[Audit] {deviceName} ({address}) : {oldValue} -> {newValue}", LogLevel.Warning, showInUi);
        }

        /// <summary>
        /// 記錄系統日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        /// <param name="level">日誌等級</param>
        /// <param name="showInUi">是否顯示在 UI 日誌檢視器</param>
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
        /// <remarks>
        /// 此方法使用批次寫入模式，日誌會先存入佇列，再定期批次寫入資料庫
        /// </remarks>
        public static void LogDataHistory(string labelName, string address, string value)
        {
            // Batch write mode: logs are queued first
            SqliteLogger.LogData(labelName, address, value);
        }

        /// <summary>
        /// 記錄操作日誌 (Operation Log) - 用於操作記錄
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="commandName">命令名稱</param>
        /// <param name="category">分類</param>
        /// <param name="beforeState">操作前狀態</param>
        /// <param name="afterState">操作後狀態</param>
        /// <param name="message">訊息</param>
        /// <param name="batchId">批次ID（選填）</param>
        /// <param name="showInUi">是否顯示在 UI 日誌檢視器</param>
        public static void LogOperation(string userId, string commandName, string category, string beforeState, string afterState, string message, string batchId = "", bool showInUi = true)
        {
            // Batch write mode: logs are queued first
            SqliteLogger.LogOperation(userId, commandName, category, beforeState, afterState, message, batchId);
            
            // Display in UI
            if (showInUi)
            {
                AddToLiveLog($"[Operation] {commandName} - {message}", LogLevel.Info, true);
            }
        }

        /// <summary>
        /// 記錄事件日誌 (Event Log) - 用於系統事件記錄
        /// </summary>
        /// <param name="eventType">事件類型</param>
        /// <param name="eventCode">事件代碼</param>
        /// <param name="eventDescription">事件描述</param>
        /// <param name="severity">嚴重性</param>
        /// <param name="currentState">當前狀態</param>
        /// <param name="userId">使用者ID</param>
        /// <param name="message">訊息</param>
        /// <param name="batchId">批次ID（選填）</param>
        /// <param name="showInUi">是否顯示在 UI 日誌檢視器</param>
        public static void LogEvent(string eventType, string eventCode, string eventDescription, string severity, string currentState, string userId, string message, string batchId = "", bool showInUi = true)
        {
            // Batch write mode: logs are queued first
            SqliteLogger.LogEvent(eventType, eventCode, eventDescription, severity, currentState, userId, message, batchId);
            
            // Display in UI
            if (showInUi)
            {
                var level = severity switch
                {
                    "Critical" => LogLevel.Error,
                    "Major" => LogLevel.Error,
                    "Minor" => LogLevel.Warning,
                    "Info" => LogLevel.Info,
                    _ => LogLevel.Info
                };
                AddToLiveLog($"[Event] {eventDescription} - {message}", level, true);
            }
        }

        /// <summary>
        /// 記錄週期性製程參數 (Periodic Data) - 每5秒記錄一次
        /// </summary>
        /// <param name="batchId">批次編號</param>
        /// <param name="predryTemp">預乾燥溫度</param>
        /// <param name="dryTemp">乾燥模組溫度</param>
        /// <param name="cdaInletPressure">設備入口氣壓</param>
        /// <remarks>
        /// 此方法使用批次寫入模式，符合 FDA 21 CFR Part 11 規範
        /// 製程中每5秒記錄一筆資料
        /// </remarks>
        public static void LogPeriodicData(string batchId, double predryTemp, double dryTemp, double cdaInletPressure)
        {
            // Batch write mode: logs are queued first
            SqliteLogger.LogPeriodicData(batchId, predryTemp, dryTemp, cdaInletPressure);
        }

        #endregion

        #region Batch Control

        /// <summary>
        /// 手動刷新所有待寫入的日誌到資料庫
        /// </summary>
        /// <remarks>
        /// 通常在程式關閉前或需要立即持久化時呼叫
        /// </remarks>
        public static void FlushLogs()
        {
            try
            {
                SqliteLogger.FlushAll();
                
                #if DEBUG
                var stats = SqliteLogger.GetStatistics();
                System.Diagnostics.Debug.WriteLine($"[ComplianceContext] 手動刷新完成 - Pending: DataLogs={stats.PendingDataLogs}, AuditLogs={stats.PendingAuditLogs}");
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComplianceContext] FlushLogs Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定批次寫入參數
        /// </summary>
        /// <param name="batchSize">批次大小（超過此數量自動刷新，預設 100）</param>
        /// <param name="flushIntervalMs">刷新間隔（毫秒，預設 5000）</param>
        /// <example>
        /// <code>
        /// // 設定為 50 筆自動刷新，每 3 秒定時刷新
        /// ComplianceContext.ConfigureBatch(50, 3000);
        /// </code>
        /// </example>
        public static void ConfigureBatch(int batchSize = 100, int flushIntervalMs = 5000)
        {
            SqliteLogger.ConfigureBatch(batchSize, flushIntervalMs);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ComplianceContext] 批次參數已更新: BatchSize={batchSize}, Interval={flushIntervalMs}ms");
            #endif
        }

        /// <summary>
        /// 取得批次寫入統計資訊
        /// </summary>
        /// <returns>
        /// (DataLogs: 已寫入 DataLogs 數量, 
        ///  AuditLogs: 已寫入 AuditLogs 數量,
        ///  OperationLogs: 已寫入 OperationLogs 數量,
        ///  EventLogs: 已寫入 EventLogs 數量,
        ///  PeriodicDataLogs: 已寫入 PeriodicDataLogs 數量,
        ///  BatchFlushes: 批次刷新次數,
        ///  PendingDataLogs: 待寫入 DataLogs 數量,
        ///  PendingAuditLogs: 待寫入 AuditLogs 數量,
        ///  PendingOperationLogs: 待寫入 OperationLogs 數量,
        ///  PendingEventLogs: 待寫入 EventLogs 數量,
        ///  PendingPeriodicData: 待寫入 PeriodicData 數量)
        /// </returns>
        public static (long DataLogs, long AuditLogs, long OperationLogs, long EventLogs, long PeriodicDataLogs, long BatchFlushes, int PendingDataLogs, int PendingAuditLogs, int PendingOperationLogs, int PendingEventLogs, int PendingPeriodicData) GetBatchStatistics()
        {
            return SqliteLogger.GetStatistics();
        }

        /// <summary>
        /// 重置批次寫入統計資訊
        /// </summary>
        public static void ResetBatchStatistics()
        {
            SqliteLogger.ResetStatistics();
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// 關閉合規引擎並刷新所有待寫入日誌
        /// </summary>
        /// <remarks>
        /// 應在程式關閉前呼叫，確保所有日誌都已寫入資料庫
        /// </remarks>
        public static void Shutdown()
        {
            try
            {
                SqliteLogger.Shutdown();
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[ComplianceContext] 合規引擎已關閉");
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComplianceContext] Shutdown Error: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 加入即時日誌到 UI 顯示集合
        /// </summary>
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

        #endregion
    }
}