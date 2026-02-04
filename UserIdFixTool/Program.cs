using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace UserIdFixTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" UserId 格式自動修正工具");
            Console.WriteLine(" Auto-fix Tool for UserId Format");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 找到資料庫檔案
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
                
                if (!File.Exists(dbPath))
                {
                    // 嘗試在上層目錄尋找
                    var parentPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.FullName ?? "", "ModelB.Demo", "bin", "Debug", "net8.0-windows", "StackDoseData.db");
                    
                    if (File.Exists(parentPath))
                    {
                        dbPath = parentPath;
                    }
                    else
                    {
                        Console.WriteLine($"? 找不到資料庫檔案");
                        Console.WriteLine($"   預期位置 1: {dbPath}");
                        Console.WriteLine($"   預期位置 2: {parentPath}");
                        Console.WriteLine();
                        Console.Write("請輸入資料庫檔案完整路徑: ");
                        var userPath = Console.ReadLine();
                        
                        if (string.IsNullOrWhiteSpace(userPath) || !File.Exists(userPath))
                        {
                            Console.WriteLine("? 無效的檔案路徑");
                            Console.ReadKey();
                            return;
                        }
                        
                        dbPath = userPath;
                    }
                }

                Console.WriteLine($"? 找到資料庫: {dbPath}");
                Console.WriteLine();

                var connectionString = $"Data Source={dbPath}";

                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();

                // 1. 顯示修正前的狀態
                Console.WriteLine("========================================");
                Console.WriteLine(" 修正前的使用者列表");
                Console.WriteLine("========================================");
                
                var usersBefore = await conn.QueryAsync<dynamic>(
                    "SELECT Id, UserId, DisplayName, AccessLevel, IsActive FROM Users ORDER BY AccessLevel DESC, Id");
                
                Console.WriteLine($"{"Id",-5} {"UserId",-20} {"DisplayName",-20} {"Level",-8} {"Active"}");
                Console.WriteLine(new string('-', 80));
                
                foreach (var user in usersBefore)
                {
                    Console.WriteLine($"{user.Id,-5} {user.UserId,-20} {user.DisplayName,-20} {user.AccessLevel,-8} {(user.IsActive == 1 ? "?" : "?")}");
                }
                
                Console.WriteLine();

                // 2. 檢查需要修正的使用者
                var needsFix = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE UserId NOT LIKE 'UID-%'");

                if (needsFix == 0)
                {
                    Console.WriteLine("? 所有使用者的 UserId 已經是正確格式，不需要修正！");
                    Console.WriteLine();
                    Console.WriteLine("按任意鍵結束...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"??  發現 {needsFix} 個使用者需要修正");
                Console.WriteLine();
                Console.Write("確認要執行自動修正? (Y/N): ");
                
                var confirm = Console.ReadLine();
                if (confirm?.ToUpper() != "Y")
                {
                    Console.WriteLine("? 已取消操作");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" 開始修正...");
                Console.WriteLine("========================================");

                // 3. 開始事務
                using var transaction = conn.BeginTransaction();

                try
                {
                    // 3.1 備份原始資料
                    await conn.ExecuteAsync(
                        "CREATE TABLE IF NOT EXISTS Users_Backup_" + DateTime.Now.ToString("yyyyMMddHHmmss") + " AS SELECT * FROM Users",
                        transaction: transaction);
                    
                    Console.WriteLine("? 已建立備份表");

                    // 3.2 修正所有 UserId
                    var updated = await conn.ExecuteAsync(@"
                        UPDATE Users 
                        SET UserId = 'UID-' || printf('%06d', Id)
                        WHERE UserId NOT LIKE 'UID-%'",
                        transaction: transaction);

                    Console.WriteLine($"? 已更新 {updated} 個使用者的 UserId");

                    // 3.3 同步更新所有日誌表中的 UserId
                    
                    // AuditTrails (User 欄位)
                    var auditUpdated = await conn.ExecuteAsync(@"
                        UPDATE AuditTrails 
                        SET User = COALESCE(
                            (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
                             FROM Users u 
                             WHERE u.DisplayName = AuditTrails.User 
                                OR u.UserId = AuditTrails.User),
                            'System'
                        )
                        WHERE User NOT LIKE 'UID-%(%)'",
                        transaction: transaction);
                    
                    if (auditUpdated > 0)
                    {
                        Console.WriteLine($"? 已更新 {auditUpdated} 筆 AuditTrails 記錄");
                    }

                    // OperationLogs
                    var opUpdated = await conn.ExecuteAsync(@"
                        UPDATE OperationLogs 
                        SET UserId = COALESCE(
                            (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
                             FROM Users u 
                             WHERE u.DisplayName = OperationLogs.UserId 
                                OR u.UserId = OperationLogs.UserId),
                            'System'
                        )
                        WHERE UserId NOT LIKE 'UID-%(%)'",
                        transaction: transaction);
                    
                    if (opUpdated > 0)
                    {
                        Console.WriteLine($"? 已更新 {opUpdated} 筆 OperationLogs 記錄");
                    }

                    // EventLogs
                    var eventUpdated = await conn.ExecuteAsync(@"
                        UPDATE EventLogs 
                        SET UserId = COALESCE(
                            (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
                             FROM Users u 
                             WHERE u.DisplayName = EventLogs.UserId 
                                OR u.UserId = EventLogs.UserId),
                            'System'
                        )
                        WHERE UserId NOT LIKE 'UID-%(%)'",
                        transaction: transaction);
                    
                    if (eventUpdated > 0)
                    {
                        Console.WriteLine($"? 已更新 {eventUpdated} 筆 EventLogs 記錄");
                    }

                    // PeriodicDataLogs
                    var periodicUpdated = await conn.ExecuteAsync(@"
                        UPDATE PeriodicDataLogs 
                        SET UserId = COALESCE(
                            (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
                             FROM Users u 
                             WHERE u.DisplayName = PeriodicDataLogs.UserId 
                                OR u.UserId = PeriodicDataLogs.UserId),
                            'System'
                        )
                        WHERE UserId NOT LIKE 'UID-%(%)'",
                        transaction: transaction);
                    
                    if (periodicUpdated > 0)
                    {
                        Console.WriteLine($"? 已更新 {periodicUpdated} 筆 PeriodicDataLogs 記錄");
                    }

                    // 提交事務
                    transaction.Commit();

                    Console.WriteLine();
                    Console.WriteLine("========================================");
                    Console.WriteLine(" ? 修正完成！");
                    Console.WriteLine("========================================");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.WriteLine("========================================");
                    Console.WriteLine(" ? 修正失敗，已回滾所有變更");
                    Console.WriteLine("========================================");
                    Console.WriteLine($" 錯誤訊息: {ex.Message}");
                    Console.WriteLine("========================================");
                    Console.ReadKey();
                    return;
                }

                // 4. 顯示修正後的狀態
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" 修正後的使用者列表");
                Console.WriteLine("========================================");
                
                var usersAfter = await conn.QueryAsync<dynamic>(
                    "SELECT Id, UserId, DisplayName, AccessLevel, IsActive FROM Users ORDER BY AccessLevel DESC, Id");
                
                Console.WriteLine($"{"Id",-5} {"UserId",-20} {"DisplayName",-20} {"Level",-8} {"Active"}");
                Console.WriteLine(new string('-', 80));
                
                foreach (var user in usersAfter)
                {
                    Console.WriteLine($"{user.Id,-5} {user.UserId,-20} {user.DisplayName,-20} {user.AccessLevel,-8} {(user.IsActive == 1 ? "?" : "?")}");
                }

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" ?? 統計資訊");
                Console.WriteLine("========================================");
                
                var stats = await conn.QueryFirstAsync<dynamic>(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Users) as TotalUsers,
                        (SELECT COUNT(*) FROM Users WHERE UserId LIKE 'UID-%') as UidFormatUsers,
                        (SELECT COUNT(*) FROM AuditTrails WHERE User LIKE 'UID-%(%') as AuditTrailsFixed,
                        (SELECT COUNT(*) FROM OperationLogs WHERE UserId LIKE 'UID-%(%') as OperationLogsFixed,
                        (SELECT COUNT(*) FROM EventLogs WHERE UserId LIKE 'UID-%(%') as EventLogsFixed,
                        (SELECT COUNT(*) FROM PeriodicDataLogs WHERE UserId LIKE 'UID-%(%') as PeriodicDataFixed
                ");
                
                Console.WriteLine($" 使用者總數: {stats.TotalUsers}");
                Console.WriteLine($" UID 格式: {stats.UidFormatUsers} / {stats.TotalUsers}");
                Console.WriteLine();
                Console.WriteLine(" 日誌記錄修正:");
                Console.WriteLine($"   AuditTrails: {stats.AuditTrailsFixed} 筆");
                Console.WriteLine($"   OperationLogs: {stats.OperationLogsFixed} 筆");
                Console.WriteLine($"   EventLogs: {stats.EventLogsFixed} 筆");
                Console.WriteLine($"   PeriodicDataLogs: {stats.PeriodicDataFixed} 筆");
                Console.WriteLine("========================================");
                
                Console.WriteLine();
                Console.WriteLine("? 所有資料已修正完成！");
                Console.WriteLine();
                Console.WriteLine("?? 下一步:");
                Console.WriteLine("   1. 重新啟動您的應用程式");
                Console.WriteLine("   2. 登出當前帳號");
                Console.WriteLine("   3. 使用 UID-000001 或 SuperAdmin 重新登入");
                Console.WriteLine("   4. 檢查 User 欄位是否顯示為 'UID-000001 (SuperAdmin)'");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" ? 發生錯誤");
                Console.WriteLine("========================================");
                Console.WriteLine($" 錯誤訊息: {ex.Message}");
                Console.WriteLine($" 堆疊追蹤: {ex.StackTrace}");
                Console.WriteLine("========================================");
            }

            Console.WriteLine();
            Console.WriteLine("按任意鍵結束...");
            Console.ReadKey();
        }
    }
}
