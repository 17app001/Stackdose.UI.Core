using System;
using System.IO;
using System.Data.SQLite; // NuGet: System.Data.SQLite.Core
using Dapper;


namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// SQLite 資料庫操作輔助類別
    /// 負責實際將資料寫入本地端的 .db 檔案
    /// </summary>
    public static class SqliteLogger
    {
        // 資料庫檔案路徑：放在應用程式執行目錄下
        private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
        private static string _connectionString = $"Data Source={_dbPath};Version=3;";

        /// <summary>
        /// 初始化資料庫
        /// 1. 檢查檔案是否存在，不存在則建立
        /// 2. 建立所需的表格 (DataLogs, AuditTrails)
        /// </summary>
        public static void Initialize()
        {
            // 如果資料庫檔案不存在，建立一個新的
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1. 建立生產數據表 (DataLogs)
                // 用於儲存 PlcLabel 收集到的歷史數據
                string sqlData = @"
                    CREATE TABLE IF NOT EXISTS DataLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LabelName TEXT,
                        Address TEXT,
                        Value TEXT
                    );";
                // Dapper: Execute 擴充方法
                conn.Execute(sqlData);

                // 2. 建立審計軌跡表 (AuditTrails)
                // 符合 FDA 21 CFR Part 11 要求：紀錄誰、何時、改了什麼、原因
                string sqlAudit = @"
                    CREATE TABLE IF NOT EXISTS AuditTrails (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        User TEXT,
                        Action TEXT,
                        TargetDevice TEXT,
                        OldValue TEXT,
                        NewValue TEXT,
                        Reason TEXT
                    );";
                conn.Execute(sqlAudit);
            }
        }

        /// <summary>
        /// 寫入生產數據 (給 PlcLabel 使用)
        /// </summary>
        public static void LogData(string labelName, string address, string value)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    string sql = "INSERT INTO DataLogs (Timestamp, LabelName, Address, Value) VALUES (@Timestamp, @Name, @Addr, @Val)";
                    // Dapper: 自動將匿名物件對應到 SQL 參數
                    conn.Execute(sql, new { Timestamp = DateTime.Now, Name = labelName, Addr = address, Val = value });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogData Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 寫入操作紀錄 (給 PlcTextBox 與 ComplianceContext 使用)
        /// </summary>
        public static void LogAudit(string user, string action, string device, string oldVal, string newVal)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    string sql = @"
                        INSERT INTO AuditTrails (Timestamp, User, Action, TargetDevice, OldValue, NewValue) 
                        VALUES (@Timestamp, @User, @Action, @Dev, @Old, @New)";

                    conn.Execute(sql, new
                    {
                        Timestamp = DateTime.Now,
                        User = user,
                        Action = action,
                        Dev = device,
                        Old = oldVal,
                        New = newVal
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogAudit Error: {ex.Message}");
            }
        }
    }
}