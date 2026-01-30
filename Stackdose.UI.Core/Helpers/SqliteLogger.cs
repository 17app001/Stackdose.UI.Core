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

        // Batch write queues
        private static readonly ConcurrentQueue<DataLogEntry> _dataLogQueue = new();
        private static readonly ConcurrentQueue<AuditLogEntry> _auditLogQueue = new();
        private static readonly ConcurrentQueue<OperationLogEntry> _operationLogQueue = new();
        private static readonly ConcurrentQueue<EventLogEntry> _eventLogQueue = new();
        private static readonly ConcurrentQueue<PeriodicDataLogEntry> _periodicDataQueue = new();

        // Flush timer
        private static System.Threading.Timer? _flushTimer;

        // Thread lock
        private static readonly object _flushLock = new();

        // Configuration parameters
        private static int _batchSize = 100;          // Batch size
        private static int _flushIntervalMs = 5000;   // Flush interval (ms)

        // Statistics
        private static long _totalDataLogs = 0;
        private static long _totalAuditLogs = 0;
        private static long _totalOperationLogs = 0;
        private static long _totalEventLogs = 0;
        private static long _totalPeriodicDataLogs = 0;
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
            public string BatchId { get; set; } = "";
            public string User { get; set; } = "";
            public string Action { get; set; } = "";
            public string TargetDevice { get; set; } = "";
            public string OldValue { get; set; } = "";
            public string NewValue { get; set; } = "";
            public string Reason { get; set; } = "";
            public string Parameter { get; set; } = "";
        }

        /// <summary>
        /// OperationLog 批次項目
        /// </summary>
        private class OperationLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string BatchId { get; set; } = "";
            public string UserId { get; set; } = "";
            public string CommandName { get; set; } = "";
            public string Category { get; set; } = "";
            public string BeforeState { get; set; } = "";
            public string AfterState { get; set; } = "";
            public string Message { get; set; } = "";
        }

        /// <summary>
        /// EventLog 批次項目
        /// </summary>
        private class EventLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string BatchId { get; set; } = "";
            public string EventType { get; set; } = "";
            public string EventCode { get; set; } = "";
            public string EventDescription { get; set; } = "";
            public string Severity { get; set; } = "";
            public string CurrentState { get; set; } = "";
            public string UserId { get; set; } = "";
            public string Message { get; set; } = "";
        }

        /// <summary>
        /// PeriodicDataLog 批次項目
        /// </summary>
        private class PeriodicDataLogEntry
        {
            public DateTime Timestamp { get; set; }
            public string BatchId { get; set; } = "";
            public double PredryTemp { get; set; }
            public double DryTemp { get; set; }
            public double CdaInletPressure { get; set; }
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
                        BatchId TEXT,
                        User TEXT,
                        Action TEXT,
                        TargetDevice TEXT,
                        OldValue TEXT,
                        NewValue TEXT,
                        Reason TEXT,
                        Parameter TEXT
                    );");

                // 3. OperationLogs (操作日誌)
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS OperationLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        BatchId TEXT,
                        UserId TEXT,
                        CommandName TEXT,
                        Category TEXT,
                        BeforeState TEXT,
                        AfterState TEXT,
                        Message TEXT
                    );");

                // 4. EventLogs (事件日誌)
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS EventLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        BatchId TEXT,
                        EventType TEXT,
                        EventCode TEXT,
                        EventDescription TEXT,
                        Severity TEXT,
                        CurrentState TEXT,
                        UserId TEXT,
                        Message TEXT
                    );");

                // 5. PeriodicDataLogs (週期性參數記錄)
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS PeriodicDataLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        BatchId TEXT,
                        PredryTemp REAL,
                        DryTemp REAL,
                        CdaInletPressure REAL
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
        /// <remarks>
        /// <para>符合 FDA 21 CFR Part 11</para>
        /// </remarks>
        /// <param name="user">使用者帳號</param>
        /// <param name="action">操作動作</param>
        /// <param name="device">目標裝置/參數</param>
        /// <param name="oldVal">修改前的值</param>
        /// <param name="newVal">修改後的值</param>
        /// <param name="reason">修改原因</param>
        /// <param name="parameter">被變更的參數名稱（選填）</param>
        /// <param name="batchId">批次編號（選填）</param>
        public static void LogAudit(string user, string action, string device, string oldVal, string newVal, string reason, string parameter = "", string batchId = "")
        {
            try
            {
                // Add to batch queue with millisecond precision
                _auditLogQueue.Enqueue(new AuditLogEntry
                {
                    Timestamp = DateTime.Now,
                    BatchId = batchId,
                    User = user,
                    Action = action,
                    TargetDevice = device,
                    OldValue = oldVal,
                    NewValue = newVal,
                    Reason = reason,
                    Parameter = parameter
                });

                // Auto flush when batch size reached
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

        /// <summary>
        /// 記錄操作日誌（批次模式）
        /// </summary>
        public static void LogOperation(string userId, string commandName, string category, string beforeState, string afterState, string message, string batchId = "")
        {
            try
            {
                _operationLogQueue.Enqueue(new OperationLogEntry
                {
                    Timestamp = DateTime.Now,
                    BatchId = batchId,
                    UserId = userId,
                    CommandName = commandName,
                    Category = category,
                    BeforeState = beforeState,
                    AfterState = afterState,
                    Message = message
                });

                if (_operationLogQueue.Count >= _batchSize)
                {
                    FlushOperationLogs();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogOperation Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 記錄事件日誌（批次模式）
        /// </summary>
        public static void LogEvent(string eventType, string eventCode, string eventDescription, string severity, string currentState, string userId, string message, string batchId = "")
        {
            try
            {
                _eventLogQueue.Enqueue(new EventLogEntry
                {
                    Timestamp = DateTime.Now,
                    BatchId = batchId,
                    EventType = eventType,
                    EventCode = eventCode,
                    EventDescription = eventDescription,
                    Severity = severity,
                    CurrentState = currentState,
                    UserId = userId,
                    Message = message
                });

                if (_eventLogQueue.Count >= _batchSize)
                {
                    FlushEventLogs();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogEvent Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 記錄週期性製程參數（批次模式） - 符合 FDA 21 CFR Part 11
        /// </summary>
        /// <param name="batchId">批次編號</param>
        /// <param name="predryTemp">預乾燥溫度</param>
        /// <param name="dryTemp">乾燥模組溫度</param>
        /// <param name="cdaInletPressure">設備入口氣壓</param>
        public static void LogPeriodicData(string batchId, double predryTemp, double dryTemp, double cdaInletPressure)
        {
            try
            {
                _periodicDataQueue.Enqueue(new PeriodicDataLogEntry
                {
                    Timestamp = DateTime.Now,
                    BatchId = batchId,
                    PredryTemp = predryTemp,
                    DryTemp = dryTemp,
                    CdaInletPressure = cdaInletPressure
                });

                if (_periodicDataQueue.Count >= _batchSize)
                {
                    FlushPeriodicDataLogs();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogPeriodicData Error: {ex.Message}");
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
            FlushOperationLogs();
            FlushEventLogs();
            FlushPeriodicDataLogs();
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
                                    @"INSERT INTO AuditTrails (Timestamp, BatchId, User, Action, TargetDevice, OldValue, NewValue, Reason, Parameter) 
                                      VALUES (@Timestamp, @BatchId, @User, @Action, @TargetDevice, @OldValue, @NewValue, @Reason, @Parameter)",
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

        /// <summary>
        /// 刷新 OperationLogs 佇列
        /// </summary>
        private static void FlushOperationLogs()
        {
            if (_operationLogQueue.IsEmpty) return;

            lock (_flushLock)
            {
                if (_operationLogQueue.IsEmpty) return;

                try
                {
                    var batch = new List<OperationLogEntry>();

                    while (_operationLogQueue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0) return;

                    using (var conn = new SqliteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            foreach (var entry in batch)
                            {
                                conn.Execute(
                                    @"INSERT INTO OperationLogs (Timestamp, BatchId, UserId, CommandName, Category, BeforeState, AfterState, Message) 
                                      VALUES (@Timestamp, @BatchId, @UserId, @CommandName, @Category, @BeforeState, @AfterState, @Message)",
                                    entry,
                                    transaction
                                );
                            }
                            transaction.Commit();
                        }
                    }

                    _totalOperationLogs += batch.Count;
                    _batchFlushCount++;

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] OperationLogs 批次寫入: {batch.Count} 筆 (累計: {_totalOperationLogs})");
                    #endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] FlushOperationLogs Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新 EventLogs 佇列
        /// </summary>
        private static void FlushEventLogs()
        {
            if (_eventLogQueue.IsEmpty) return;

            lock (_flushLock)
            {
                if (_eventLogQueue.IsEmpty) return;

                try
                {
                    var batch = new List<EventLogEntry>();

                    while (_eventLogQueue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0) return;

                    using (var conn = new SqliteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            foreach (var entry in batch)
                            {
                                conn.Execute(
                                    @"INSERT INTO EventLogs (Timestamp, BatchId, EventType, EventCode, EventDescription, Severity, CurrentState, UserId, Message) 
                                      VALUES (@Timestamp, @BatchId, @EventType, @EventCode, @EventDescription, @Severity, @CurrentState, @UserId, @Message)",
                                    entry,
                                    transaction
                                );
                            }
                            transaction.Commit();
                        }
                    }

                    _totalEventLogs += batch.Count;
                    _batchFlushCount++;

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] EventLogs 批次寫入: {batch.Count} 筆 (累計: {_totalEventLogs})");
                    #endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] FlushEventLogs Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 刷新 PeriodicDataLogs 佇列
        /// </summary>
        private static void FlushPeriodicDataLogs()
        {
            if (_periodicDataQueue.IsEmpty) return;

            lock (_flushLock)
            {
                if (_periodicDataQueue.IsEmpty) return;

                try
                {
                    var batch = new List<PeriodicDataLogEntry>();

                    while (_periodicDataQueue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0) return;

                    using (var conn = new SqliteConnection(_connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            foreach (var entry in batch)
                            {
                                conn.Execute(
                                    @"INSERT INTO PeriodicDataLogs (Timestamp, BatchId, PredryTemp, DryTemp, CdaInletPressure) 
                                      VALUES (@Timestamp, @BatchId, @PredryTemp, @DryTemp, @CdaInletPressure)",
                                    entry,
                                    transaction
                                );
                            }
                            transaction.Commit();
                        }
                    }

                    _totalPeriodicDataLogs += batch.Count;
                    _batchFlushCount++;

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] PeriodicDataLogs 批次寫入: {batch.Count} 筆 (累計: {_totalPeriodicDataLogs})");
                    #endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] FlushPeriodicDataLogs Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 取得統計資訊
        /// </summary>
        public static (long DataLogs, long AuditLogs, long OperationLogs, long EventLogs, long PeriodicDataLogs, long BatchFlushes, int PendingDataLogs, int PendingAuditLogs, int PendingOperationLogs, int PendingEventLogs, int PendingPeriodicData) GetStatistics()
        {
            return (
                DataLogs: _totalDataLogs,
                AuditLogs: _totalAuditLogs,
                OperationLogs: _totalOperationLogs,
                EventLogs: _totalEventLogs,
                PeriodicDataLogs: _totalPeriodicDataLogs,
                BatchFlushes: _batchFlushCount,
                PendingDataLogs: _dataLogQueue.Count,
                PendingAuditLogs: _auditLogQueue.Count,
                PendingOperationLogs: _operationLogQueue.Count,
                PendingEventLogs: _eventLogQueue.Count,
                PendingPeriodicData: _periodicDataQueue.Count
            );
        }

        /// <summary>
        /// 重置統計資訊
        /// </summary>
        public static void ResetStatistics()
        {
            _totalDataLogs = 0;
            _totalAuditLogs = 0;
            _totalOperationLogs = 0;
            _totalEventLogs = 0;
            _totalPeriodicDataLogs = 0;
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
            System.Diagnostics.Debug.WriteLine($"[SqliteLogger] Shutdown - 總計寫入: DataLogs={stats.DataLogs}, AuditLogs={stats.AuditLogs}, OperationLogs={stats.OperationLogs}, EventLogs={stats.EventLogs}, PeriodicDataLogs={stats.PeriodicDataLogs}, BatchFlushes={stats.BatchFlushes}");
            #endif
        }

        #endregion
    }
}