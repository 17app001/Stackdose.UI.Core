using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.IO;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// SQLite 日誌記錄器 - 支援批次寫入優化
    /// </summary>
    /// <remarks>
    /// <para>效能優化特性：</para>
    /// <list type="bullet">
    /// <item>批次寫入佇列（預設 100 筆自動刷新）</item>
    /// <item>定時刷新（預設 5 秒）</item>
    /// <item>執行緒安全</item>
    /// <item>自動錯誤處理</item>
    /// </list>
    /// </remarks>
    public static class SqliteLogger
    {
        #region Private Fields

        private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
        private static string _connectionString = $"Data Source={_dbPath}";

        // 🔥 批次寫入佇列
        private static readonly ConcurrentQueue<DataLogEntry> _dataLogQueue = new();
        private static readonly ConcurrentQueue<AuditLogEntry> _auditLogQueue = new();

        // 🔥 定時刷新 Timer
        private static System.Threading.Timer? _flushTimer;

        // 🔥 執行緒鎖
        private static readonly object _flushLock = new();

        // 🔥 設定參數
        private static int _batchSize = 100;          // 批次大小（超過此數量自動刷新）
        private static int _flushIntervalMs = 5000;   // 刷新間隔（毫秒）

        // 🔥 統計資訊
        private static long _totalDataLogs = 0;
        private static long _totalAuditLogs = 0;
        private static long _batchFlushCount = 0;

        #endregion

        #region Events

        /// <summary>
        /// 批次刷新開始事件
        /// </summary>
        /// <remarks>
        /// 參數：(dataCount, auditCount) - 待寫入的日誌數量
        /// </remarks>
        public static event Action<int, int>? BatchFlushStarted;

        /// <summary>
        /// 批次刷新完成事件
        /// </summary>
        /// <remarks>
        /// 參數：(dataCount, auditCount) - 成功寫入的日誌數量
        /// </remarks>
        public static event Action<int, int>? BatchFlushCompleted;

        #endregion

        #region Nested Classes

        /// <summary>
        /// DataLog 批次項目
        /// </summary>
        private class DataLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string LabelName { get; set; } = "";
            public string Address { get; set; } = "";
            public string Value { get; set; } = "";
        }

        /// <summary>
        /// AuditLog 批次項目
        /// </summary>
        private class AuditLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string User { get; set; } = "";
            public string Action { get; set; } = "";
            public string TargetDevice { get; set; } = "";
            public string OldValue { get; set; } = "";
            public string NewValue { get; set; } = "";
            public string Reason { get; set; } = "";
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化資料庫並啟動批次刷新 Timer
        /// </summary>
        public static void Initialize()
        {
            // 建立資料表
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // 1. DataLogs (生產數據)
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS DataLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LabelName TEXT,
                        Address TEXT,
                        Value TEXT
                    );");

                // 2. AuditTrails (審計軌跡)
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS AuditTrails (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        User TEXT,
                        Action TEXT,
                        TargetDevice TEXT,
                        OldValue TEXT,
                        NewValue TEXT,
                        Reason TEXT
                    );");
            }

            // 🔥 啟動定時刷新 Timer
            _flushTimer = new System.Threading.Timer(
                callback: _ => FlushAll(),
                state: null,
                dueTime: _flushIntervalMs,
                period: _flushIntervalMs
            );

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[SqliteLogger] 批次寫入模式已啟用");
            System.Diagnostics.Debug.WriteLine($"[SqliteLogger] 批次大小: {_batchSize}, 刷新間隔: {_flushIntervalMs}ms");
            #endif
        }

        /// <summary>
        /// 設定批次參數
        /// </summary>
        /// <param name="batchSize">批次大小（預設 100）</param>
        /// <param name="flushIntervalMs">刷新間隔（毫秒，預設 5000）</param>
        public static void ConfigureBatch(int batchSize = 100, int flushIntervalMs = 5000)
        {
            _batchSize = batchSize;
            _flushIntervalMs = flushIntervalMs;

            // 重啟 Timer
            _flushTimer?.Dispose();
            _flushTimer = new System.Threading.Timer(
                callback: _ => FlushAll(),
                state: null,
                dueTime: _flushIntervalMs,
                period: _flushIntervalMs
            );

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SqliteLogger] 批次參數已更新: BatchSize={_batchSize}, Interval={_flushIntervalMs}ms");
            #endif
        }

        #endregion

        #region Public API (支援批次模式)

        /// <summary>
        /// 記錄生產數據（批次模式）
        /// </summary>
        public static void LogData(string labelName, string address, string value)
        {
            try
            {
                // 🔥 加入批次佇列
                _dataLogQueue.Enqueue(new DataLogEntry
                {
                    Timestamp = DateTime.Now,
                    LabelName = labelName,
                    Address = address,
                    Value = value
                });

                // 🔥 超過批次大小時自動刷新
                if (_dataLogQueue.Count >= _batchSize)
                {
                    FlushDataLogs();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogData Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 記錄審計軌跡（批次模式）
        /// </summary>
        public static void LogAudit(string user, string action, string device, string oldVal, string newVal, string reason)
        {
            try
            {
                // 🔥 加入批次佇列
                _auditLogQueue.Enqueue(new AuditLogEntry
                {
                    Timestamp = DateTime.Now,
                    User = user,
                    Action = action,
                    TargetDevice = device,
                    OldValue = oldVal,
                    NewValue = newVal,
                    Reason = reason
                });

                // 🔥 超過批次大小時自動刷新
                if (_auditLogQueue.Count >= _batchSize)
                {
                    FlushAuditLogs();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogAudit Error: {ex.Message}");
            }
        }

        #endregion

        #region Flush Methods

        /// <summary>
        /// 手動刷新所有待寫入的日誌
        /// </summary>
        public static void FlushAll()
        {
            FlushDataLogs();
            FlushAuditLogs();
        }

        /// <summary>
        /// 刷新 DataLogs 佇列
        /// </summary>
        private static void FlushDataLogs()
        {
            if (_dataLogQueue.IsEmpty) return;

            lock (_flushLock)
            {
                if (_dataLogQueue.IsEmpty) return;

                try
                {
                    var batch = new List<DataLogEntry>();

                    // 取出所有待寫入項目
                    while (_dataLogQueue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0) return;

                    // 🔥 觸發批次刷新開始事件
                    BatchFlushStarted?.Invoke(batch.Count, 0);

                    // 🔥 批次寫入資料庫
                    using (var conn = new SqliteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            foreach (var entry in batch)
                            {
                                conn.Execute(
                                    "INSERT INTO DataLogs (Timestamp, LabelName, Address, Value) VALUES (@Timestamp, @LabelName, @Address, @Value)",
                                    entry,
                                    transaction
                                );
                            }
                            transaction.Commit();
                        }
                    }

                    // 更新統計
                    _totalDataLogs += batch.Count;
                    _batchFlushCount++;

                    // 🔥 觸發批次刷新完成事件
                    BatchFlushCompleted?.Invoke(batch.Count, 0);

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] DataLogs 批次寫入: {batch.Count} 筆 (累計: {_totalDataLogs})");
                    #endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] FlushDataLogs Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新 AuditLogs 佇列
        /// </summary>
        private static void FlushAuditLogs()
        {
            if (_auditLogQueue.IsEmpty) return;

            lock (_flushLock)
            {
                if (_auditLogQueue.IsEmpty) return;

                try
                {
                    var batch = new List<AuditLogEntry>();

                    // 取出所有待寫入項目
                    while (_auditLogQueue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0) return;

                    // 🔥 觸發批次刷新開始事件
                    BatchFlushStarted?.Invoke(0, batch.Count);

                    // 🔥 批次寫入資料庫
                    using (var conn = new SqliteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            foreach (var entry in batch)
                            {
                                conn.Execute(
                                    @"INSERT INTO AuditTrails (Timestamp, User, Action, TargetDevice, OldValue, NewValue, Reason) 
                                      VALUES (@Timestamp, @User, @Action, @TargetDevice, @OldValue, @NewValue, @Reason)",
                                    entry,
                                    transaction
                                );
                            }
                            transaction.Commit();
                        }
                    }

                    // 更新統計
                    _totalAuditLogs += batch.Count;
                    _batchFlushCount++;

                    // 🔥 觸發批次刷新完成事件
                    BatchFlushCompleted?.Invoke(0, batch.Count);

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] AuditLogs 批次寫入: {batch.Count} 筆 (累計: {_totalAuditLogs})");
                    #endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] FlushAuditLogs Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 取得統計資訊
        /// </summary>
        public static (long DataLogs, long AuditLogs, long BatchFlushes, int PendingDataLogs, int PendingAuditLogs) GetStatistics()
        {
            return (
                DataLogs: _totalDataLogs,
                AuditLogs: _totalAuditLogs,
                BatchFlushes: _batchFlushCount,
                PendingDataLogs: _dataLogQueue.Count,
                PendingAuditLogs: _auditLogQueue.Count
            );
        }

        /// <summary>
        /// 重置統計資訊
        /// </summary>
        public static void ResetStatistics()
        {
            _totalDataLogs = 0;
            _totalAuditLogs = 0;
            _batchFlushCount = 0;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 關閉 Logger 並刷新所有待寫入日誌
        /// </summary>
        public static void Shutdown()
        {
            _flushTimer?.Dispose();
            _flushTimer = null;

            // 最後一次刷新
            FlushAll();

            #if DEBUG
            var stats = GetStatistics();
            System.Diagnostics.Debug.WriteLine($"[SqliteLogger] Shutdown - 總計寫入: DataLogs={stats.DataLogs}, AuditLogs={stats.AuditLogs}, BatchFlushes={stats.BatchFlushes}");
            #endif
        }

        #endregion
    }
}