using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// 使用者管理服務實作 (符合 FDA 21 CFR Part 11)
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly string _connectionString;

        public UserManagementService(string? dbPath = null)
        {
            var path = dbPath ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
            _connectionString = $"Data Source={path}";
            InitializeDatabase();
            
            // ?? 確保預設 Admin 帳號存在
            EnsureDefaultAdminExists();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[UserManagementService] Initialized with default admin");
            #endif
        }

        #region Database Initialization

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Users 資料表
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId TEXT UNIQUE NOT NULL,
                    DisplayName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    AccessLevel INTEGER NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt DATETIME NOT NULL,
                    CreatedByUserId INTEGER,
                    CreatedBy TEXT,
                    LastLoginAt DATETIME,
                    LastModifiedAt DATETIME,
                    LastModifiedByUserId INTEGER,
                    Email TEXT,
                    Department TEXT,
                    Remarks TEXT
                );");

            // UserAuditLogs 資料表
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS UserAuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp DATETIME NOT NULL,
                    OperatorUserId INTEGER NOT NULL,
                    OperatorUserName TEXT NOT NULL,
                    Action INTEGER NOT NULL,
                    TargetUserId INTEGER,
                    TargetUserName TEXT,
                    Details TEXT,
                    IpAddress TEXT
                );");

            // 建立索引
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_users_userid ON Users(UserId);");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_users_accesslevel ON Users(AccessLevel);");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_auditlogs_timestamp ON UserAuditLogs(Timestamp);");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_auditlogs_targetuser ON UserAuditLogs(TargetUserId);");
        }

        /// <summary>
        /// 確保預設 Admin 和 SuperAdmin 帳號存在
        /// </summary>
        private void EnsureDefaultAdminExists()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[UserManagementService] Checking default accounts...");
                #endif

                // 檢查是否已有 SuperAdmin 帳號
                var superAdminCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE AccessLevel = @Level", 
                    new { Level = (int)AccessLevel.SuperAdmin });

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] SuperAdmin count: {superAdminCount}");
                #endif

                // 檢查 superadmin 帳號是否存在
                var superAdminExists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE UserId = @UserId",
                    new { UserId = "superadmin" });

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] superadmin account exists: {superAdminExists > 0}");
                #endif

                if (superAdminExists == 0)
                {
                    // 創建預設 SuperAdmin 帳號
                    var (superHash, superSalt) = HashPassword("superadminsuperadmin");

                    conn.Execute(@"
                        INSERT INTO Users (UserId, DisplayName, PasswordHash, Salt, AccessLevel, IsActive, 
                                          CreatedAt, CreatedBy, Email, Department, Remarks)
                        VALUES (@UserId, @DisplayName, @PasswordHash, @Salt, @AccessLevel, @IsActive, 
                               @CreatedAt, @CreatedBy, @Email, @Department, @Remarks)",
                        new
                        {
                            UserId = "superadmin",
                            DisplayName = "Super Administrator",
                            PasswordHash = superHash,
                            Salt = superSalt,
                            AccessLevel = (int)AccessLevel.SuperAdmin,
                            IsActive = 1,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "System",
                            Email = "superadmin@stackdose.com",
                            Department = "IT",
                            Remarks = "Default super administrator account with full access"
                        });

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[UserManagementService] ? Default SuperAdmin account created: superadmin / superadminsuperadmin");
                    #endif
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[UserManagementService] SuperAdmin account already exists, skipping creation");
                    #endif
                }

                // 檢查是否已有 Admin 帳號
                var adminExists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE UserId = @UserId",
                    new { UserId = "admin01" });

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] admin01 account exists: {adminExists > 0}");
                #endif

                if (adminExists == 0)
                {
                    // 創建預設 Admin 帳號
                    var (hash, salt) = HashPassword("admin123");

                    conn.Execute(@"
                        INSERT INTO Users (UserId, DisplayName, PasswordHash, Salt, AccessLevel, IsActive, 
                                          CreatedAt, CreatedBy, Email, Department, Remarks)
                        VALUES (@UserId, @DisplayName, @PasswordHash, @Salt, @AccessLevel, @IsActive, 
                               @CreatedAt, @CreatedBy, @Email, @Department, @Remarks)",
                        new
                        {
                            UserId = "admin01",
                            DisplayName = "System Administrator",
                            PasswordHash = hash,
                            Salt = salt,
                            AccessLevel = (int)AccessLevel.Admin,
                            IsActive = 1,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "System",
                            Email = "admin@stackdose.com",
                            Department = "IT",
                            Remarks = "Default system administrator account"
                        });

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[UserManagementService] ? Default admin account created: admin01 / admin123");
                    #endif
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[UserManagementService] Admin account already exists, skipping creation");
                    #endif
                }

                // ?? 輸出所有使用者列表（調試用）
                #if DEBUG
                var allUsers = conn.Query<dynamic>("SELECT UserId, DisplayName, AccessLevel, IsActive FROM Users ORDER BY AccessLevel DESC");
                System.Diagnostics.Debug.WriteLine("[UserManagementService] Current users in database:");
                foreach (var user in allUsers)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {user.UserId} ({user.DisplayName}) - Level {user.AccessLevel}, Active: {user.IsActive}");
                }
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] EnsureDefaultAdminExists Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Password Hashing

        private (string Hash, string Salt) HashPassword(string password)
        {
            // 生成 Salt
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // 計算 Hash (SHA256 + Salt)
            using (var sha256 = SHA256.Create())
            {
                var passwordWithSalt = Encoding.UTF8.GetBytes(password + salt);
                var hashBytes = sha256.ComputeHash(passwordWithSalt);
                string hash = Convert.ToBase64String(hashBytes);
                return (hash, salt);
            }
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var passwordWithSalt = Encoding.UTF8.GetBytes(password + salt);
                var hashBytes = sha256.ComputeHash(passwordWithSalt);
                string computedHash = Convert.ToBase64String(hashBytes);
                return computedHash == hash;
            }
        }

        #endregion

        #region Authentication

        /// <summary>
        /// ?? 資料庫密碼驗證（用於本地創建的使用者）
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="password">明文密碼</param>
        /// <returns>驗證結果</returns>
        public async Task<(bool Success, string Message, UserAccount? User)> AuthenticateAsync(string userId, string password)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // 查詢使用者
                var user = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1",
                    new { UserId = userId });

                if (user == null)
                {
                    return (false, "使用者不存在或已停用", null);
                }

                // 驗證密碼
                if (!VerifyPassword(password, user.PasswordHash, user.Salt))
                {
                    return (false, "密碼錯誤", null);
                }

                // 更新最後登入時間
                await conn.ExecuteAsync(
                    "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
                    new { Now = DateTime.Now, Id = user.Id });

                user.LastLoginAt = DateTime.Now;

                return (true, "驗證成功", user);
            }
            catch (Exception ex)
            {
                return (false, $"驗證失敗: {ex.Message}", null);
            }
        }

        #endregion

        #region Create User

        public async Task<(bool Success, string Message, UserAccount? User)> CreateUserAsync(
            string userId,
            string displayName,
            string password,
            AccessLevel accessLevel,
            int creatorUserId,
            string? email = null,
            string? department = null,
            string? remarks = null)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // 檢查 UserId 是否已存在
                var existing = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE UserId = @UserId",
                    new { UserId = userId });

                if (existing != null)
                {
                    return (false, $"使用者 ID '{userId}' 已存在", null);
                }

                // 取得創建者資訊
                var creator = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id",
                    new { Id = creatorUserId });

                if (creator == null)
                {
                    return (false, "找不到創建者資訊", null);
                }

                // 檢查權限
                if (!CanManageUser(creator.AccessLevel, accessLevel))
                {
                    return (false, $"您沒有權限創建 {accessLevel} 等級的使用者", null);
                }

                // Hash 密碼
                var (hash, salt) = HashPassword(password);

                // 建立使用者
                var newUser = new UserAccount
                {
                    UserId = userId,
                    DisplayName = displayName,
                    PasswordHash = hash,
                    Salt = salt,
                    AccessLevel = accessLevel,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = creatorUserId,
                    CreatedBy = creator.DisplayName,
                    Email = email,
                    Department = department,
                    Remarks = remarks
                };

                // 插入資料庫
                var sql = @"
                    INSERT INTO Users (UserId, DisplayName, PasswordHash, Salt, AccessLevel, IsActive, 
                                      CreatedAt, CreatedByUserId, CreatedBy, Email, Department, Remarks)
                    VALUES (@UserId, @DisplayName, @PasswordHash, @Salt, @AccessLevel, @IsActive, 
                           @CreatedAt, @CreatedByUserId, @CreatedBy, @Email, @Department, @Remarks);
                    SELECT last_insert_rowid();";

                newUser.Id = await conn.ExecuteScalarAsync<int>(sql, newUser);

                // 記錄稽核日誌
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = creatorUserId,
                    OperatorUserName = creator.DisplayName,
                    Action = UserAuditAction.CreateUser,
                    TargetUserId = newUser.Id,
                    TargetUserName = userId,
                    Details = $"創建使用者: {displayName} ({accessLevel})"
                });

                return (true, "使用者創建成功", newUser);
            }
            catch (Exception ex)
            {
                return (false, $"創建失敗: {ex.Message}", null);
            }
        }

        /// <summary>
        /// ?? 新增：從 Windows AD 建立使用者
        /// </summary>
        /// <param name="adUsername">AD 使用者名稱</param>
        /// <param name="accessLevel">要給予的權限等級</param>
        /// <param name="creatorUserId">建立者的 UserId</param>
        /// <param name="defaultPassword">預設密碼（選填，用於本地驗證 fallback）</param>
        /// <returns>操作結果</returns>
        public async Task<(bool Success, string Message, UserAccount? User)> CreateUserFromAdAsync(
            string adUsername,
            AccessLevel accessLevel,
            int creatorUserId,
            string? defaultPassword = null)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // 1. 檢查 AD 使用者是否存在
                var adService = new AdAuthenticationService();
                var adUserInfo = adService.GetUserInfo(adUsername);

                if (adUserInfo == null)
                {
                    return (false, $"AD 使用者 '{adUsername}' 不存在或無法存取", null);
                }

                if (!adUserInfo.IsEnabled)
                {
                    return (false, $"AD 使用者 '{adUsername}' 已停用", null);
                }

                // 2. 檢查 UserId 是否已存在
                var existing = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE UserId = @UserId",
                    new { UserId = adUsername });

                if (existing != null)
                {
                    return (false, $"使用者 ID '{adUsername}' 已存在", null);
                }

                // 3. 取得建立者資訊
                var creator = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id",
                    new { Id = creatorUserId });

                if (creator == null)
                {
                    return (false, "找不到建立者資訊", null);
                }

                // 4. 檢查權限
                if (!CanManageUser(creator.AccessLevel, accessLevel))
                {
                    return (false, $"您沒有權限建立 {accessLevel} 等級的使用者", null);
                }

                // 5. Hash 預設密碼（如果提供）
                string hash, salt;
                if (!string.IsNullOrWhiteSpace(defaultPassword))
                {
                    (hash, salt) = HashPassword(defaultPassword);
                }
                else
                {
                    // 使用隨機密碼作為 fallback（但實際不會用到，因為優先使用 AD）
                    var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 16);
                    (hash, salt) = HashPassword(randomPassword);
                }

                // 6. 建立使用者（從 AD 同步資訊）
                var newUser = new UserAccount
                {
                    UserId = adUserInfo.Username,
                    DisplayName = adUserInfo.DisplayName,
                    PasswordHash = hash,
                    Salt = salt,
                    AccessLevel = accessLevel,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedByUserId = creatorUserId,
                    CreatedBy = creator.DisplayName,
                    Email = adUserInfo.Email,
                    Department = adUserInfo.Description, // AD Description 可能包含部門資訊
                    Remarks = $"Created from Windows AD: {adUserInfo.Username}"
                };

                // 7. 寫入資料庫
                var sql = @"
                    INSERT INTO Users (UserId, DisplayName, PasswordHash, Salt, AccessLevel, IsActive, 
                                      CreatedAt, CreatedByUserId, CreatedBy, Email, Department, Remarks)
                    VALUES (@UserId, @DisplayName, @PasswordHash, @Salt, @AccessLevel, @IsActive, 
                           @CreatedAt, @CreatedByUserId, @CreatedBy, @Email, @Department, @Remarks);
                    SELECT last_insert_rowid();";

                newUser.Id = await conn.ExecuteScalarAsync<int>(sql, newUser);

                // 8. 記錄審計軌跡
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = creatorUserId,
                    OperatorUserName = creator.DisplayName,
                    Action = UserAuditAction.CreateUser,
                    TargetUserId = newUser.Id,
                    TargetUserName = adUsername,
                    Details = $"從 AD 建立使用者: {adUserInfo.DisplayName} ({accessLevel})"
                });

                return (true, $"已從 AD 建立使用者: {adUserInfo.DisplayName}", newUser);
            }
            catch (Exception ex)
            {
                return (false, $"建立失敗: {ex.Message}", null);
            }
        }

        #endregion

        #region Delete User

        public async Task<(bool Success, string Message)> SoftDeleteUserAsync(int targetUserId, int operatorUserId)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var operatorUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = operatorUserId });
                var targetUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = targetUserId });

                if (operatorUser == null || targetUser == null)
                {
                    return (false, "找不到使用者資訊");
                }

                // 檢查權限
                if (!CanDeleteUser(operatorUserId, targetUserId, operatorUser.AccessLevel))
                {
                    return (false, "您沒有權限刪除此使用者");
                }

                // 軟刪除
                await conn.ExecuteAsync(
                    "UPDATE Users SET IsActive = 0, LastModifiedAt = @Now, LastModifiedByUserId = @OperatorId WHERE Id = @TargetId",
                    new { Now = DateTime.Now, OperatorId = operatorUserId, TargetId = targetUserId });

                // 記錄稽核日誌
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = operatorUserId,
                    OperatorUserName = operatorUser.DisplayName,
                    Action = UserAuditAction.DeleteUser,
                    TargetUserId = targetUserId,
                    TargetUserName = targetUser.UserId,
                    Details = $"停用使用者: {targetUser.DisplayName}"
                });

                return (true, "使用者已停用");
            }
            catch (Exception ex)
            {
                return (false, $"刪除失敗: {ex.Message}");
            }
        }

        #endregion

        #region Activate User

        public async Task<(bool Success, string Message)> ActivateUserAsync(int targetUserId, int operatorUserId)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var operatorUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = operatorUserId });
                var targetUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = targetUserId });

                if (operatorUser == null || targetUser == null)
                {
                    return (false, "找不到使用者資訊");
                }

                // 啟用使用者
                await conn.ExecuteAsync(
                    "UPDATE Users SET IsActive = 1, LastModifiedAt = @Now, LastModifiedByUserId = @OperatorId WHERE Id = @TargetId",
                    new { Now = DateTime.Now, OperatorId = operatorUserId, TargetId = targetUserId });

                // 記錄稽核日誌
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = operatorUserId,
                    OperatorUserName = operatorUser.DisplayName,
                    Action = UserAuditAction.ActivateUser,
                    TargetUserId = targetUserId,
                    TargetUserName = targetUser.UserId,
                    Details = $"啟用使用者: {targetUser.DisplayName}"
                });

                return (true, "使用者已啟用");
            }
            catch (Exception ex)
            {
                return (false, $"啟用失敗: {ex.Message}");
            }
        }

        #endregion

        #region Update User

        public async Task<(bool Success, string Message)> UpdateUserAsync(
            int targetUserId,
            int operatorUserId,
            string? displayName = null,
            string? email = null,
            string? department = null,
            string? remarks = null,
            AccessLevel? newAccessLevel = null)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var operatorUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = operatorUserId });
                var targetUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = targetUserId });

                if (operatorUser == null || targetUser == null)
                {
                    return (false, "找不到使用者資訊");
                }

                // ?? 檢查權限：不能修改比自己權限高或相等的使用者
                if (operatorUser.AccessLevel <= targetUser.AccessLevel && operatorUser.Id != targetUser.Id)
                {
                    return (false, $"您沒有權限修改 {targetUser.AccessLevel} 等級的使用者");
                }

                // 檢查權限
                if (newAccessLevel.HasValue && !CanManageUser(operatorUser.AccessLevel, newAccessLevel.Value))
                {
                    return (false, "您沒有權限設定此權限等級");
                }

                // ?? 檢查：不能將使用者權限設定為高於或等於自己
                if (newAccessLevel.HasValue && newAccessLevel.Value >= operatorUser.AccessLevel)
                {
                    return (false, $"您不能將使用者權限設定為 {newAccessLevel.Value}（必須低於您的權限）");
                }

                // 更新欄位
                var updates = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("Id", targetUserId);
                parameters.Add("Now", DateTime.Now);
                parameters.Add("OperatorId", operatorUserId);

                if (displayName != null)
                {
                    updates.Add("DisplayName = @DisplayName");
                    parameters.Add("DisplayName", displayName);
                }
                if (email != null)
                {
                    updates.Add("Email = @Email");
                    parameters.Add("Email", email);
                }
                if (department != null)
                {
                    updates.Add("Department = @Department");
                    parameters.Add("Department", department);
                }
                if (remarks != null)
                {
                    updates.Add("Remarks = @Remarks");
                    parameters.Add("Remarks", remarks);
                }
                if (newAccessLevel.HasValue)
                {
                    updates.Add("AccessLevel = @AccessLevel");
                    parameters.Add("AccessLevel", (int)newAccessLevel.Value);
                }

                if (updates.Count == 0)
                {
                    return (false, "沒有任何欄位需要更新");
                }

                updates.Add("LastModifiedAt = @Now");
                updates.Add("LastModifiedByUserId = @OperatorId");

                var sql = $"UPDATE Users SET {string.Join(", ", updates)} WHERE Id = @Id";
                await conn.ExecuteAsync(sql, parameters);

                // 記錄稽核日誌
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = operatorUserId,
                    OperatorUserName = operatorUser.DisplayName,
                    Action = UserAuditAction.ModifyUser,
                    TargetUserId = targetUserId,
                    TargetUserName = targetUser.UserId,
                    Details = $"更新使用者: {string.Join(", ", updates)}"
                });

                return (true, "使用者資料已更新");
            }
            catch (Exception ex)
            {
                return (false, $"更新失敗: {ex.Message}");
            }
        }

        #endregion

        #region Reset Password

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            int targetUserId,
            int operatorUserId,
            string newPassword)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                var operatorUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = operatorUserId });
                var targetUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                    "SELECT * FROM Users WHERE Id = @Id", new { Id = targetUserId });

                if (operatorUser == null || targetUser == null)
                {
                    return (false, "找不到使用者資訊");
                }

                // Hash 新密碼
                var (hash, salt) = HashPassword(newPassword);

                // 更新密碼
                await conn.ExecuteAsync(
                    @"UPDATE Users SET PasswordHash = @Hash, Salt = @Salt, 
                      LastModifiedAt = @Now, LastModifiedByUserId = @OperatorId 
                      WHERE Id = @TargetId",
                    new { Hash = hash, Salt = salt, Now = DateTime.Now, OperatorId = operatorUserId, TargetId = targetUserId });

                // 記錄稽核日誌
                await LogAuditAsync(conn, new UserAuditLog
                {
                    Timestamp = DateTime.Now,
                    OperatorUserId = operatorUserId,
                    OperatorUserName = operatorUser.DisplayName,
                    Action = UserAuditAction.ResetPassword,
                    TargetUserId = targetUserId,
                    TargetUserName = targetUser.UserId,
                    Details = $"重設密碼: {targetUser.DisplayName}"
                });

                return (true, "密碼已重設");
            }
            catch (Exception ex)
            {
                return (false, $"重設失敗: {ex.Message}");
            }
        }

        #endregion

        #region Query Methods

        public async Task<List<UserAccount>> GetManagedUsersAsync(int operatorUserId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var operatorUser = await conn.QueryFirstOrDefaultAsync<UserAccount>(
                "SELECT * FROM Users WHERE Id = @Id", new { Id = operatorUserId });

            if (operatorUser == null)
                return new List<UserAccount>();

            // SuperAdmin: 查看所有使用者（包含其他 SuperAdmin）
            if (operatorUser.AccessLevel == AccessLevel.SuperAdmin)
            {
                return (await conn.QueryAsync<UserAccount>("SELECT * FROM Users ORDER BY AccessLevel DESC, UserId")).ToList();
            }

            // Admin: 查看所有使用者（除了 SuperAdmin）
            if (operatorUser.AccessLevel == AccessLevel.Admin)
            {
                return (await conn.QueryAsync<UserAccount>(
                    @"SELECT * FROM Users 
                      WHERE AccessLevel < @SuperAdminLevel
                      ORDER BY AccessLevel DESC, UserId",
                    new { SuperAdminLevel = (int)AccessLevel.SuperAdmin }
                )).ToList();
            }

            // Supervisor: 查看權限 <= Supervisor 的所有使用者（包含自己與所有低權限）
            if (operatorUser.AccessLevel == AccessLevel.Supervisor)
            {
                return (await conn.QueryAsync<UserAccount>(
                    @"SELECT * FROM Users 
                      WHERE AccessLevel <= @SupervisorLevel
                      ORDER BY AccessLevel DESC, UserId",
                    new { SupervisorLevel = (int)AccessLevel.Supervisor }
                )).ToList();
            }

            // Operator 與以下：只能看自己
            return new List<UserAccount> { operatorUser };
        }

        public async Task<List<UserAccount>> GetAllUsersAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            return (await conn.QueryAsync<UserAccount>("SELECT * FROM Users ORDER BY AccessLevel DESC, UserId")).ToList();
        }

        public async Task<UserAccount?> GetUserByIdAsync(int userId)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            return await conn.QueryFirstOrDefaultAsync<UserAccount>(
                "SELECT * FROM Users WHERE Id = @Id", new { Id = userId });
        }

        public async Task<List<UserAuditLog>> GetAuditLogsAsync(int? targetUserId = null, int pageSize = 100)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            if (targetUserId.HasValue)
            {
                return (await conn.QueryAsync<UserAuditLog>(
                    "SELECT * FROM UserAuditLogs WHERE TargetUserId = @TargetUserId ORDER BY Timestamp DESC LIMIT @PageSize",
                    new { TargetUserId = targetUserId.Value, PageSize = pageSize }
                )).ToList();
            }
            else
            {
                return (await conn.QueryAsync<UserAuditLog>(
                    "SELECT * FROM UserAuditLogs ORDER BY Timestamp DESC LIMIT @PageSize",
                    new { PageSize = pageSize }
                )).ToList();
            }
        }

        #endregion

        #region Permission Checks

        public bool CanDeleteUser(int operatorUserId, int targetUserId, AccessLevel operatorLevel)
        {
            // 不能刪除自己
            if (operatorUserId == targetUserId)
                return false;

            // SuperAdmin 可刪除所有人（除了自己）
            if (operatorLevel == AccessLevel.SuperAdmin)
                return true;

            // Admin 可刪除所有 < SuperAdmin（除了自己）
            if (operatorLevel == AccessLevel.Admin)
                return true;

            // Supervisor 可刪除低於 Supervisor（不包含自己）
            // 以及自己創建的 Level 1-2 使用者
            if (operatorLevel == AccessLevel.Supervisor)
                return true;

            return false;
        }

        public bool CanManageUser(AccessLevel operatorLevel, AccessLevel targetLevel)
        {
            // SuperAdmin 可管理所有等級
            if (operatorLevel == AccessLevel.SuperAdmin)
                return true;

            // Admin 可管理所有等級（除了 SuperAdmin）
            if (operatorLevel == AccessLevel.Admin && targetLevel < AccessLevel.SuperAdmin)
                return true;

            // Supervisor 可管理 Supervisor 與 Level 1-2
            if (operatorLevel == AccessLevel.Supervisor && targetLevel <= AccessLevel.Supervisor)
                return true;

            return false;
        }

        #endregion

        #region AD Integration Helpers

        /// <summary>
        /// ?? 新增：取得本機所有 AD 使用者清單（用於下拉選單）
        /// </summary>
        /// <returns>AD 使用者清單</returns>
        public List<string> GetAvailableAdUsers()
        {
            var users = new List<string>();
            
            try
            {
                var adService = new AdAuthenticationService();
                
                // 取得本機所有使用者（限制 LocalMachine 模式）
                using (var context = new System.DirectoryServices.AccountManagement.PrincipalContext(
                    System.DirectoryServices.AccountManagement.ContextType.Machine))
                {
                    var searcher = new System.DirectoryServices.AccountManagement.UserPrincipal(context);
                    using (var search = new System.DirectoryServices.AccountManagement.PrincipalSearcher(searcher))
                    {
                        foreach (var result in search.FindAll())
                        {
                            if (result is System.DirectoryServices.AccountManagement.UserPrincipal userPrincipal)
                            {
                                if (!string.IsNullOrWhiteSpace(userPrincipal.SamAccountName))
                                {
                                    users.Add(userPrincipal.SamAccountName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserManagementService] GetAvailableAdUsers Error: {ex.Message}");
            }
            
            return users.OrderBy(u => u).ToList();
        }

        /// <summary>
        /// ?? 新增：檢查 AD 使用者是否已在本地資料庫中
        /// </summary>
        /// <param name="adUsername">AD 使用者名稱</param>
        /// <returns>是否已存在</returns>
        public async Task<bool> IsAdUserRegisteredAsync(string adUsername)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE UserId = @UserId",
                new { UserId = adUsername });
            
            return count > 0;
        }

        #endregion

        #region Audit Logging

        private async Task LogAuditAsync(SqliteConnection conn, UserAuditLog log)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO UserAuditLogs (Timestamp, OperatorUserId, OperatorUserName, Action, 
                                            TargetUserId, TargetUserName, Details, IpAddress)
                  VALUES (@Timestamp, @OperatorUserId, @OperatorUserName, @Action, 
                         @TargetUserId, @TargetUserName, @Details, @IpAddress)",
                log);
        }

        /// <summary>
        /// ?? 新增：根據 AD 群組自動判斷對應的 AccessLevel
        /// </summary>
        /// <param name="userGroups">使用者所屬的 AD 群組清單</param>
        /// <returns>對應的 AccessLevel</returns>
        public static AccessLevel DetermineAccessLevelFromAdGroups(List<string> userGroups)
        {
            // 將群組名稱轉為小寫以便忽略大小寫差異
            var groupsLower = userGroups.Select(g => g.ToLower()).ToList();

            // 判斷權限等級（從高到低）
            // L5: App_SuperAdmins
            if (groupsLower.Contains("app_superadmins"))
            {
                return AccessLevel.SuperAdmin;
            }
            // L4: App_Admins
            else if (groupsLower.Contains("app_admins"))
            {
                return AccessLevel.Admin;
            }
            // L3: App_Supervisors
            else if (groupsLower.Contains("app_supervisors"))
            {
                return AccessLevel.Supervisor;
            }
            // L2: App_Instructors
            else if (groupsLower.Contains("app_instructors"))
            {
                return AccessLevel.Instructor;
            }
            // L1: App_Operators
            else if (groupsLower.Contains("app_operators"))
            {
                return AccessLevel.Operator;
            }
            // Domain/Local Admins -> 對應到 SuperAdmin
            else if (groupsLower.Any(g => g.Contains("domain admins") || g.Contains("enterprise admins")))
            {
                return AccessLevel.SuperAdmin;
            }
            // Local Administrators -> 對應到 Admin
            else if (groupsLower.Contains("administrators"))
            {
                return AccessLevel.Admin;
            }
            // Guest - 預設最低權限
            else
            {
                return AccessLevel.Guest;
            }
        }

        #endregion
    }
}
