using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// 日誌查詢服務 - 從 SQLite 讀取 AuditTrails 和 DataLogs
    /// </summary>
    public class LogService
    {
        private readonly string _connectionString;

        public LogService()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
            _connectionString = $"Data Source={dbPath}";
        }

        #region AuditTrails

        /// <summary>
        /// 取得所有稽核軌跡記錄（按日期分組）
        /// </summary>
        public List<AuditTrailRecord> GetAllAuditTrails()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<AuditTrailRecord>(
                "SELECT * FROM AuditTrails ORDER BY Timestamp DESC"
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期範圍的稽核軌跡
        /// </summary>
        public List<AuditTrailRecord> GetAuditTrailsByDateRange(DateTime from, DateTime to)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<AuditTrailRecord>(
                @"SELECT * FROM AuditTrails 
                  WHERE DATE(Timestamp) BETWEEN DATE(@From) AND DATE(@To)
                  ORDER BY Timestamp DESC",
                new { From = from, To = to }
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期的稽核軌跡
        /// </summary>
        public List<AuditTrailRecord> GetAuditTrailsByDate(DateTime date)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<AuditTrailRecord>(
                @"SELECT * FROM AuditTrails 
                  WHERE DATE(Timestamp) = DATE(@Date)
                  ORDER BY Timestamp DESC",
                new { Date = date }
            ).ToList();

            return records;
        }

        #endregion

        #region DataLogs

        /// <summary>
        /// 取得所有生產數據記錄
        /// </summary>
        public List<DataLogRecord> GetAllDataLogs()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<DataLogRecord>(
                "SELECT * FROM DataLogs ORDER BY Timestamp DESC"
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期範圍的生產數據
        /// </summary>
        public List<DataLogRecord> GetDataLogsByDateRange(DateTime from, DateTime to)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<DataLogRecord>(
                @"SELECT * FROM DataLogs 
                  WHERE DATE(Timestamp) BETWEEN DATE(@From) AND DATE(@To)
                  ORDER BY Timestamp DESC",
                new { From = from, To = to }
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期的生產數據
        /// </summary>
        public List<DataLogRecord> GetDataLogsByDate(DateTime date)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<DataLogRecord>(
                @"SELECT * FROM DataLogs 
                  WHERE DATE(Timestamp) = DATE(@Date)
                  ORDER BY Timestamp DESC",
                new { Date = date }
            ).ToList();

            return records;
        }

        #endregion

        #region OperationLogs

        /// <summary>
        /// 取得所有操作日誌
        /// </summary>
        public List<OperationLogRecord> GetAllOperationLogs()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<OperationLogRecord>(
                "SELECT * FROM OperationLogs ORDER BY Timestamp DESC"
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期範圍的操作日誌
        /// </summary>
        public List<OperationLogRecord> GetOperationLogsByDateRange(DateTime from, DateTime to)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<OperationLogRecord>(
                @"SELECT * FROM OperationLogs 
                  WHERE DATE(Timestamp) BETWEEN DATE(@From) AND DATE(@To)
                  ORDER BY Timestamp DESC",
                new { From = from, To = to }
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期的操作日誌
        /// </summary>
        public List<OperationLogRecord> GetOperationLogsByDate(DateTime date)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<OperationLogRecord>(
                @"SELECT * FROM OperationLogs 
                  WHERE DATE(Timestamp) = DATE(@Date)
                  ORDER BY Timestamp DESC",
                new { Date = date }
            ).ToList();

            return records;
        }

        #endregion

        #region EventLogs

        /// <summary>
        /// 取得所有事件日誌
        /// </summary>
        public List<EventLogRecord> GetAllEventLogs()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<EventLogRecord>(
                "SELECT * FROM EventLogs ORDER BY Timestamp DESC"
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期範圍的事件日誌
        /// </summary>
        public List<EventLogRecord> GetEventLogsByDateRange(DateTime from, DateTime to)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<EventLogRecord>(
                @"SELECT * FROM EventLogs 
                  WHERE DATE(Timestamp) BETWEEN DATE(@From) AND DATE(@To)
                  ORDER BY Timestamp DESC",
                new { From = from, To = to }
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期的事件日誌
        /// </summary>
        public List<EventLogRecord> GetEventLogsByDate(DateTime date)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<EventLogRecord>(
                @"SELECT * FROM EventLogs 
                  WHERE DATE(Timestamp) = DATE(@Date)
                  ORDER BY Timestamp DESC",
                new { Date = date }
            ).ToList();

            return records;
        }

        #endregion

        #region PeriodicDataLogs

        /// <summary>
        /// 取得所有週期性數據
        /// </summary>
        public List<PeriodicDataLogRecord> GetAllPeriodicDataLogs()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<PeriodicDataLogRecord>(
                "SELECT * FROM PeriodicDataLogs ORDER BY Timestamp DESC"
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期範圍的週期性數據
        /// </summary>
        public List<PeriodicDataLogRecord> GetPeriodicDataLogsByDateRange(DateTime from, DateTime to)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<PeriodicDataLogRecord>(
                @"SELECT * FROM PeriodicDataLogs 
                  WHERE DATE(Timestamp) BETWEEN DATE(@From) AND DATE(@To)
                  ORDER BY Timestamp DESC",
                new { From = from, To = to }
            ).ToList();

            return records;
        }

        /// <summary>
        /// 取得指定日期的週期性數據
        /// </summary>
        public List<PeriodicDataLogRecord> GetPeriodicDataLogsByDate(DateTime date)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var records = conn.Query<PeriodicDataLogRecord>(
                @"SELECT * FROM PeriodicDataLogs 
                  WHERE DATE(Timestamp) = DATE(@Date)
                  ORDER BY Timestamp DESC",
                new { Date = date }
            ).ToList();

            return records;
        }

        #endregion

        #region Date Grouping

        /// <summary>
        /// 取得所有有記錄的日期列表（按日期分組統計）
        /// </summary>
        public List<DateGroup> GetDateGroups()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 從 AuditTrails 和 DataLogs 聯集取得所有日期
            var auditDates = conn.Query<string>(
                "SELECT DISTINCT DATE(Timestamp) as DateStr FROM AuditTrails"
            ).ToList();

            var dataLogDates = conn.Query<string>(
                "SELECT DISTINCT DATE(Timestamp) as DateStr FROM DataLogs"
            ).ToList();

            var operationLogDates = conn.Query<string>(
                "SELECT DISTINCT DATE(Timestamp) as DateStr FROM OperationLogs"
            ).ToList();

            var eventLogDates = conn.Query<string>(
                "SELECT DISTINCT DATE(Timestamp) as DateStr FROM EventLogs"
            ).ToList();

            var periodicDataLogDates = conn.Query<string>(
                "SELECT DISTINCT DATE(Timestamp) as DateStr FROM PeriodicDataLogs"
            ).ToList();

            var allDates = auditDates.Union(dataLogDates)
                .Union(operationLogDates)
                .Union(eventLogDates)
                .Union(periodicDataLogDates)
                .Select(d => DateTime.Parse(d))
                .OrderByDescending(d => d)
                .ToList();

            // 統計每個日期的記錄數量
            var dateGroups = allDates.Select(date =>
            {
                var auditCount = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM AuditTrails WHERE DATE(Timestamp) = DATE(@Date)",
                    new { Date = date }
                );

                var dataLogCount = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM DataLogs WHERE DATE(Timestamp) = DATE(@Date)",
                    new { Date = date }
                );

                var operationLogCount = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM OperationLogs WHERE DATE(Timestamp) = DATE(@Date)",
                    new { Date = date }
                );

                var eventLogCount = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM EventLogs WHERE DATE(Timestamp) = DATE(@Date)",
                    new { Date = date }
                );

                var periodicDataLogCount = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM PeriodicDataLogs WHERE DATE(Timestamp) = DATE(@Date)",
                    new { Date = date }
                );

                return new DateGroup
                {
                    Date = date,
                    AuditTrailCount = auditCount,
                    DataLogCount = dataLogCount,
                    OperationLogCount = operationLogCount,
                    EventLogCount = eventLogCount,
                    PeriodicDataLogCount = periodicDataLogCount,
                    TotalCount = auditCount + dataLogCount + operationLogCount + eventLogCount + periodicDataLogCount
                };
            }).ToList();

            return dateGroups;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// 取得統計資訊
        /// </summary>
        public LogStatistics GetStatistics()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            return new LogStatistics
            {
                TotalAuditTrails = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AuditTrails"),
                TotalDataLogs = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM DataLogs"),
                TotalOperationLogs = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM OperationLogs"),
                TotalEventLogs = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM EventLogs"),
                TotalPeriodicDataLogs = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM PeriodicDataLogs"),
                EarliestAuditDate = conn.ExecuteScalar<DateTime?>("SELECT MIN(Timestamp) FROM AuditTrails"),
                LatestAuditDate = conn.ExecuteScalar<DateTime?>("SELECT MAX(Timestamp) FROM AuditTrails"),
                EarliestDataLogDate = conn.ExecuteScalar<DateTime?>("SELECT MIN(Timestamp) FROM DataLogs"),
                LatestDataLogDate = conn.ExecuteScalar<DateTime?>("SELECT MAX(Timestamp) FROM DataLogs"),
                EarliestOperationLogDate = conn.ExecuteScalar<DateTime?>("SELECT MIN(Timestamp) FROM OperationLogs"),
                LatestOperationLogDate = conn.ExecuteScalar<DateTime?>("SELECT MAX(Timestamp) FROM OperationLogs"),
                EarliestEventLogDate = conn.ExecuteScalar<DateTime?>("SELECT MIN(Timestamp) FROM EventLogs"),
                LatestEventLogDate = conn.ExecuteScalar<DateTime?>("SELECT MAX(Timestamp) FROM EventLogs"),
                EarliestPeriodicDataLogDate = conn.ExecuteScalar<DateTime?>("SELECT MIN(Timestamp) FROM PeriodicDataLogs"),
                LatestPeriodicDataLogDate = conn.ExecuteScalar<DateTime?>("SELECT MAX(Timestamp) FROM PeriodicDataLogs")
            };
        }

        #endregion

        #region PDF Export

        /// <summary>
        /// 匯出稽核軌跡到 PDF
        /// </summary>
        public void ExportAuditTrailsToPdf(List<AuditTrailRecord> records, string filePath)
        {
            using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // 寫入標題
                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine($"{"稽核軌跡報表 (Audit Trail Report)",60}");
                writer.WriteLine($"{"匯出時間",20}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"{"記錄數量",20}: {records.Count} 筆");
                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine();

                // 寫入記錄
                foreach (var record in records)
                {
                    writer.WriteLine("-".PadRight(100, '-'));
                    writer.WriteLine($"{"ID",15}: {record.Id}");
                    writer.WriteLine($"{"時間戳記",15}: {record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine($"{"使用者",15}: {record.User}");
                    writer.WriteLine($"{"動作",15}: {record.Action}");
                    writer.WriteLine($"{"目標裝置",15}: {record.TargetDevice}");
                    writer.WriteLine($"{"舊值",15}: {record.OldValue}");
                    writer.WriteLine($"{"新值",15}: {record.NewValue}");
                    writer.WriteLine($"{"原因",15}: {record.Reason}");
                    writer.WriteLine();
                }

                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine($"{"報表結束",60}");
                writer.WriteLine("=".PadRight(100, '='));
            }

            // 由於無 PDF 套件，先匯出為文字檔，但檔名仍為 .pdf
            // 實際生產環境應使用 QuestPDF 或 iTextSharp
            System.Diagnostics.Debug.WriteLine($"[LogService] Exported {records.Count} audit trails to {filePath}");
        }

        /// <summary>
        /// 匯出生產數據到 PDF
        /// </summary>
        public void ExportDataLogsToPdf(List<DataLogRecord> records, string filePath)
        {
            using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // 寫入標題
                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine($"{"生產數據報表 (Data Log Report)",60}");
                writer.WriteLine($"{"匯出時間",20}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"{"記錄數量",20}: {records.Count} 筆");
                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine();

                // 寫入記錄
                foreach (var record in records)
                {
                    writer.WriteLine("-".PadRight(100, '-'));
                    writer.WriteLine($"{"ID",15}: {record.Id}");
                    writer.WriteLine($"{"時間戳記",15}: {record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine($"{"標籤名稱",15}: {record.LabelName}");
                    writer.WriteLine($"{"位址",15}: {record.Address}");
                    writer.WriteLine($"{"數值",15}: {record.Value}");
                    writer.WriteLine();
                }

                writer.WriteLine("=".PadRight(100, '='));
                writer.WriteLine($"{"報表結束",60}");
                writer.WriteLine("=".PadRight(100, '='));
            }

            System.Diagnostics.Debug.WriteLine($"[LogService] Exported {records.Count} data logs to {filePath}");
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// 稽核軌跡記錄
    /// </summary>
    public class AuditTrailRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string TargetDevice { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Reason { get; set; } = "";

        // UI 顯示用
        public string TimeStr => Timestamp.ToString("HH:mm:ss.fff");
        public string DateStr => Timestamp.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 生產數據記錄
    /// </summary>
    public class DataLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string LabelName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Value { get; set; } = "";

        // UI 顯示用
        public string TimeStr => Timestamp.ToString("HH:mm:ss.fff");
        public string DateStr => Timestamp.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 操作日誌記錄
    /// </summary>
    public class OperationLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string BatchId { get; set; } = "";
        public string UserId { get; set; } = "SuperAdmin"; // ?? FDA 21 CFR Part 11 - 預設為 SuperAdmin
        public string CommandName { get; set; } = "";
        public string Category { get; set; } = "";
        public string BeforeState { get; set; } = "";
        public string AfterState { get; set; } = "";
        public string Message { get; set; } = "";

        // UI 顯示用
        public string TimeStr => Timestamp.ToString("HH:mm:ss.fff");
        public string DateStr => Timestamp.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 事件日誌記錄
    /// </summary>
    public class EventLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string BatchId { get; set; } = "";
        public string EventType { get; set; } = "";
        public string EventCode { get; set; } = "";
        public string EventDescription { get; set; } = "";
        public string Severity { get; set; } = "";
        public string CurrentState { get; set; } = "";
        public string UserId { get; set; } = "SuperAdmin"; // ?? FDA 21 CFR Part 11 - 預設為 SuperAdmin
        public string Message { get; set; } = "";

        // UI 顯示用
        public string TimeStr => Timestamp.ToString("HH:mm:ss.fff");
        public string DateStr => Timestamp.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 週期性數據記錄
    /// </summary>
    public class PeriodicDataLogRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string BatchId { get; set; } = "";
        public string UserId { get; set; } = "SuperAdmin"; // ?? FDA 21 CFR Part 11 - 預設為 SuperAdmin
        public double PredryTemp { get; set; }
        public double DryTemp { get; set; }
        public double CdaInletPressure { get; set; }

        // UI 顯示用
        public string TimeStr => Timestamp.ToString("HH:mm:ss.fff");
        public string DateStr => Timestamp.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 日期分組
    /// </summary>
    public class DateGroup
    {
        public DateTime Date { get; set; }
        public int AuditTrailCount { get; set; }
        public int DataLogCount { get; set; }
        public int OperationLogCount { get; set; }
        public int EventLogCount { get; set; }
        public int PeriodicDataLogCount { get; set; }
        public int TotalCount { get; set; }

        // UI 顯示用
        public string DateStr => Date.ToString("yyyy-MM-dd (ddd)", new System.Globalization.CultureInfo("zh-TW"));
        public string RelativeStr
        {
            get
            {
                var diff = (DateTime.Today - Date.Date).Days;
                return diff switch
                {
                    0 => "今天",
                    1 => "昨天",
                    _ when diff < 7 => $"{diff} 天前",
                    _ => Date.ToString("yyyy/MM/dd")
                };
            }
        }
    }

    /// <summary>
    /// 統計資訊
    /// </summary>
    public class LogStatistics
    {
        public int TotalAuditTrails { get; set; }
        public int TotalDataLogs { get; set; }
        public int TotalOperationLogs { get; set; }
        public int TotalEventLogs { get; set; }
        public int TotalPeriodicDataLogs { get; set; }
        public DateTime? EarliestAuditDate { get; set; }
        public DateTime? LatestAuditDate { get; set; }
        public DateTime? EarliestDataLogDate { get; set; }
        public DateTime? LatestDataLogDate { get; set; }
        public DateTime? EarliestOperationLogDate { get; set; }
        public DateTime? LatestOperationLogDate { get; set; }
        public DateTime? EarliestEventLogDate { get; set; }
        public DateTime? LatestEventLogDate { get; set; }
        public DateTime? EarliestPeriodicDataLogDate { get; set; }
        public DateTime? LatestPeriodicDataLogDate { get; set; }

        public int TotalLogs => TotalAuditTrails + TotalDataLogs + TotalOperationLogs + TotalEventLogs + TotalPeriodicDataLogs;
    }

    #endregion
}
