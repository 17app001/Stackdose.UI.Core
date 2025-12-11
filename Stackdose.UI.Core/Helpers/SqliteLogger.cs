using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Stackdose.UI.Core.Helpers
{
    public static class SqliteLogger
    {
        private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
        private static string _connectionString = $"Data Source={_dbPath}";

        public static void Initialize()
        {
            //if (!File.Exists(_dbPath)) SqliteConnection.CreateFile(_dbPath);

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
                // 🔥 重點：確保有 Reason 欄位
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
        }

        public static void LogData(string labelName, string address, string value)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Execute("INSERT INTO DataLogs (Timestamp, LabelName, Address, Value) VALUES (@Timestamp, @Name, @Addr, @Val)",
                        new { Timestamp = DateTime.Now, Name = labelName, Addr = address, Val = value });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogData Error: {ex.Message}"); }
        }

        // 🔥 修正：加入 reason 參數並寫入資料庫
        public static void LogAudit(string user, string action, string device, string oldVal, string newVal, string reason)
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    string sql = @"
                        INSERT INTO AuditTrails (Timestamp, User, Action, TargetDevice, OldValue, NewValue, Reason) 
                        VALUES (@Timestamp, @User, @Action, @Dev, @Old, @New, @Reason)";

                    conn.Execute(sql, new
                    {
                        Timestamp = DateTime.Now,
                        User = user,
                        Action = action,
                        Dev = device,
                        Old = oldVal,
                        New = newVal,
                        Reason = reason
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogAudit Error: {ex.Message}"); }
        }
    }
}