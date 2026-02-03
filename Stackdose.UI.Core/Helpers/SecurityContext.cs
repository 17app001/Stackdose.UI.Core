using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;
using Stackdose.Abstractions.Logging;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 安全性上下文管理 (Security Context Manager)
    /// 用途：統一管理使用者登入、權限控制、自動登出
    /// 符合 FDA 21 CFR Part 11 要求
    /// </summary>
    public static class SecurityContext
    {
        #region 靜態屬性

        /// <summary>
        /// 當前使用者工作階段
        /// </summary>
        public static UserSession CurrentSession { get; } = new UserSession();

        /// <summary>
        /// 自動登出時間（分鐘，預設 30）
        /// </summary>
        public static int AutoLogoutMinutes { get; set; } = 30; // 🔥 改為 30 分鐘

        /// <summary>
        /// 是否啟用自動登出功能
        /// </summary>
        public static bool EnableAutoLogout { get; set; } = true;

        #endregion

        #region 事件定義

        /// <summary>
        /// 登入成功事件
        /// </summary>
        public static event EventHandler<UserAccount>? LoginSuccess;

        /// <summary>
        /// 登出事件
        /// </summary>
        public static event EventHandler? LogoutOccurred;

        /// <summary>
        /// 權限變更事件 (用於更新 UI)
        /// </summary>
        public static event EventHandler? AccessLevelChanged;

        #endregion

        #region 登入/登出

        /// <summary>
        /// 使用者登入 - 使用 SQLite 資料庫驗證
        /// </summary>
        /// <param name="userId">使用者帳號</param>
        /// <param name="password">密碼</param>
        /// <returns>是否登入成功</returns>
        public static bool Login(string userId, string password)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 🔥 寫入檔案日誌（確保能看到）
            var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_debug.log");
            void WriteLog(string message)
            {
                try
                {
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] {message}");
                }
                catch { }
            }
            
            WriteLog($"========================================");
            WriteLog($"Login START: {userId}");
            WriteLog($"Mode: SQLite Database Authentication");
            WriteLog($"========================================");

            // 🔥 資料庫驗證
            try
            {
                WriteLog($"[Step 1] 嘗試資料庫驗證...");
                var userService = new Stackdose.UI.Core.Services.UserManagementService();
                var dbResult = userService.AuthenticateAsync(userId, password).Result;
                
                if (dbResult.Success && dbResult.User != null)
                {
                    WriteLog($"✅ 資料庫驗證成功");
                    WriteLog($"   UserId: {dbResult.User.UserId}");
                    WriteLog($"   DisplayName: {dbResult.User.DisplayName}");
                    WriteLog($"   AccessLevel: {dbResult.User.AccessLevel}");
                    
                    // 設定 Session
                    CurrentSession.CurrentUser = dbResult.User;
                    CurrentSession.LoginTime = DateTime.Now;
                    CurrentSession.LastActivityTime = DateTime.Now;
                    
                    // 記錄到 Audit Trail (不包含 "via Database" 字樣)
                    ComplianceContext.LogAuditTrail(
                        "User Login",
                        userId,
                        "Logged Out",
                        $"Logged In (Level {(int)dbResult.User.AccessLevel} - {dbResult.User.AccessLevel})",
                        $"Login from {Environment.MachineName}",  // 🔥 移除 "via Database"
                        showInUi: true
                    );

                    ComplianceContext.LogSystem(
                        $"✅ Login Success: {dbResult.User.DisplayName} ({dbResult.User.AccessLevel})",
                        LogLevel.Success,
                        showInUi: true
                    );

                    // 觸發事件
                    LoginSuccess?.Invoke(null, dbResult.User);
                    AccessLevelChanged?.Invoke(null, EventArgs.Empty);

                    // 啟動自動登出計時器
                    if (EnableAutoLogout)
                    {
                        StartAutoLogoutTimer();
                    }

                    stopwatch.Stop();
                    WriteLog($"========================================");
                    WriteLog($"✅ Login COMPLETED in {stopwatch.ElapsedMilliseconds}ms");
                    WriteLog($"========================================");
                    
                    return true;
                }
                else
                {
                    WriteLog($"❌ 資料庫驗證失敗: {dbResult.Message}");
                    
                    ComplianceContext.LogSystem(
                        $"Login Failed: {userId} - {dbResult.Message}",
                        LogLevel.Warning,
                        showInUi: true
                    );
                    
                    ComplianceContext.LogAuditTrail(
                        "User Login",
                        userId,
                        "N/A",
                        "Failed (Invalid Credentials)",
                        $"驗證超時，請檢查網路連線或憑證設定",
                        showInUi: false
                    );
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"❌ 資料庫驗證錯誤: {ex.Message}");
                
                ComplianceContext.LogSystem(
                    $"Authentication Error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
                
                return false;
            }
        }

        /// <summary>
        /// 快速登入（預設帳號，用於測試或初始化）
        /// </summary>
        /// <param name="level">權限等級</param>
        public static void QuickLogin(AccessLevel level = AccessLevel.Admin)
        {
            UserAccount? user = null;
            
            // 🔥 修正：根據權限等級決定要載入的使用者
            switch (level)
            {
                case AccessLevel.Admin:
                    // 🔥 嘗試從資料庫載入 admin01（改為新預設帳號）
                    user = LoadUserFromDatabase("admin01");
                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[SecurityContext] QuickLogin: admin01 not found in database, creating temporary user");
                        user = new UserAccount
                        {
                            Id = 1,
                            UserId = "admin01",
                            DisplayName = "系統管理員 (Admin)",
                            PasswordHash = HashPassword("admin01admin01"),
                            AccessLevel = AccessLevel.Admin,
                            IsActive = true,
                            CreatedBy = "System",
                            CreatedAt = DateTime.Now
                        };
                    }
                    break;
                    
                case AccessLevel.Guest:
                    // 🔥 建立 Guest 臨時帳號
                    System.Diagnostics.Debug.WriteLine("[SecurityContext] QuickLogin: Creating Guest user");
                    user = new UserAccount
                    {
                        Id = 0,
                        UserId = "Guest",
                        DisplayName = "訪客 (Guest)",
                        PasswordHash = string.Empty,
                        Salt = string.Empty,
                        AccessLevel = AccessLevel.Guest,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now
                    };
                    break;
                    
                default:
                    // 🔥 其他權限等級，建立臨時帳號
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] QuickLogin: Creating temporary {level} user");
                    user = new UserAccount
                    {
                        Id = (int)level,
                        UserId = level.ToString(),
                        DisplayName = $"{level} (Temporary)",
                        PasswordHash = string.Empty,
                        Salt = string.Empty,
                        AccessLevel = level,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedAt = DateTime.Now
                    };
                    break;
            }

            CurrentSession.CurrentUser = user;
            CurrentSession.LoginTime = DateTime.Now;
            CurrentSession.LastActivityTime = DateTime.Now;

            ComplianceContext.LogSystem(
                $"[QUICK] Quick Login: {user.DisplayName} (Level: {user.AccessLevel})",
                LogLevel.Info,
                showInUi: true
            );

            // 觸發事件
            LoginSuccess?.Invoke(null, user);
            AccessLevelChanged?.Invoke(null, EventArgs.Empty);

            // 🔥 修正：Guest 不需要自動登出計時器
            if (EnableAutoLogout && level != AccessLevel.Guest)
            {
                StartAutoLogoutTimer();
            }
        }

        /// <summary>
        /// 使用者登出
        /// </summary>
        /// <param name="isAutoLogout">是否為自動登出</param>
        public static void Logout(bool isAutoLogout = false)
        {
            if (!CurrentSession.IsLoggedIn)
                return;

            var user = CurrentSession.CurrentUser!;

            // 1. 記錄到 Audit Trail
            string reason = isAutoLogout ? "Auto-Logout (Timeout)" : "Manual Logout";
            ComplianceContext.LogAuditTrail(
                "User Logout",
                user.UserId,
                $"Logged In (Level {(int)user.AccessLevel} - {user.AccessLevel})",
                "Logged Out",
                reason,
                showInUi: true
            );

            ComplianceContext.LogSystem(
                $"[LOGOUT] Logout: {user.DisplayName} ({reason})",
                LogLevel.Warning,
                showInUi: true
            );

            // 2. 清除工作階段
            CurrentSession.CurrentUser = null;

            // 3. 觸發事件
            LogoutOccurred?.Invoke(null, EventArgs.Empty);
            AccessLevelChanged?.Invoke(null, EventArgs.Empty);

            // 4. 停止自動登出計時器
            StopAutoLogoutTimer();
        }

        /// <summary>
        /// 更新最後活動時間（由 UI 操作時呼叫）
        /// </summary>
        public static void UpdateActivity()
        {
            if (CurrentSession.IsLoggedIn)
            {
                CurrentSession.LastActivityTime = DateTime.Now;
            }
        }

        #endregion

        #region 權限檢查

        /// <summary>
        /// 檢查當前使用者是否有指定權限
        /// </summary>
        /// <param name="requiredLevel">所需權限等級</param>
        /// <returns>是否有權限</returns>
        public static bool HasAccess(AccessLevel requiredLevel)
        {
            return CurrentSession.HasAccess(requiredLevel);
        }

        /// <summary>
        /// 檢查權限，如果不足則顯示訊息並返回 false
        /// </summary>
        /// <param name="requiredLevel">所需權限等級</param>
        /// <param name="operationName">操作名稱</param>
        /// <returns>是否有權限</returns>
        public static bool CheckAccess(AccessLevel requiredLevel, string operationName = "此操作")
        {
            if (HasAccess(requiredLevel))
                return true;

            string message = $"[ERROR] 權限不足\n\n{operationName} 需要 {GetLevelDisplayName(requiredLevel)} 以上權限\n\n當前權限: {GetLevelDisplayName(CurrentSession.CurrentLevel)}";
            
            ComplianceContext.LogSystem(
                $"Access Denied: {operationName} requires {requiredLevel} (Current: {CurrentSession.CurrentLevel})",
                LogLevel.Warning,
                showInUi: true
            );

            // 記錄到 Audit Trail
            ComplianceContext.LogAuditTrail(
                "Access Denied",
                CurrentSession.CurrentUser?.UserId ?? "Guest",
                operationName,
                "Denied",
                $"Required Level: {requiredLevel}",
                showInUi: false
            );

            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(message, "權限不足 - Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            });

            return false;
        }

        #endregion

        #region 自動登出計時器

        private static System.Threading.Timer? _autoLogoutTimer;

        private static void StartAutoLogoutTimer()
        {
            StopAutoLogoutTimer();

            _autoLogoutTimer = new System.Threading.Timer(_ =>
            {
                if (!CurrentSession.IsLoggedIn || !EnableAutoLogout)
                    return;

                var idleTime = DateTime.Now - CurrentSession.LastActivityTime;
                if (idleTime.TotalMinutes >= AutoLogoutMinutes)
                {
                    Application.Current?.Dispatcher.Invoke(() => Logout(isAutoLogout: true));
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30)); // 每 30 秒檢查一次
        }

        private static void StopAutoLogoutTimer()
        {
            _autoLogoutTimer?.Dispose();
            _autoLogoutTimer = null;
        }

        #endregion

        #region 密碼加密

        /// <summary>
        /// SHA-256 密碼雜湊（不帶 Salt，僅用於舊系統相容）
        /// </summary>
        /// <param name="password">明文密碼</param>
        /// <returns>雜湊後的密碼</returns>
        [Obsolete("此方法不安全，僅用於舊系統相容。請使用 VerifyPassword 進行驗證。")]
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// 🔥 新增：驗證密碼（使用 Salt）
        /// </summary>
        /// <param name="password">明文密碼</param>
        /// <param name="storedHash">儲存的密碼雜湊值</param>
        /// <param name="salt">密碼鹽值</param>
        /// <returns>密碼是否正確</returns>
        private static bool VerifyPassword(string password, string storedHash, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                // 計算密碼 + Salt 的雜湊值
                var passwordWithSalt = Encoding.UTF8.GetBytes(password + salt);
                var hashBytes = sha256.ComputeHash(passwordWithSalt);
                string computedHash = Convert.ToBase64String(hashBytes);
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SecurityContext] VerifyPassword:");
                System.Diagnostics.Debug.WriteLine($"  Input: {password}");
                System.Diagnostics.Debug.WriteLine($"  Salt: {salt}");
                System.Diagnostics.Debug.WriteLine($"  Computed Hash: {computedHash}");
                System.Diagnostics.Debug.WriteLine($"  Stored Hash: {storedHash}");
                System.Diagnostics.Debug.WriteLine($"  Match: {computedHash == storedHash}");
                #endif
                
                return computedHash == storedHash;
            }
        }

        #endregion

        #region 資料庫存取

        /// <summary>
        /// 從資料庫載入使用者（用於 QuickLogin 測試）
        /// </summary>
        private static UserAccount? LoadUserFromDatabase(string userId)
        {
            try
            {
                // 從真實資料庫讀取（用於 QuickLogin 測試）
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                
                var user = conn.QueryFirstOrDefault<UserAccount>(
                    "SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1",
                    new { UserId = userId }
                );
                
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecurityContext] LoadUserFromDatabase Error: {ex.Message}");
                
                // 🔥 Fallback 到內建帳號（改為 admin01 / admin01admin01）
                var defaultAccounts = new Dictionary<string, UserAccount>
                {
                    ["admin01"] = new UserAccount
                    {
                        Id = 1,
                        UserId = "admin01",
                        DisplayName = "系統管理員 (Fallback)",
                        PasswordHash = HashPassword("admin01admin01"),
                        AccessLevel = AccessLevel.Admin,
                        IsActive = true,
                        CreatedBy = "System (Fallback)"
                    }
                };

                return defaultAccounts.TryGetValue(userId, out var account) ? account : null;
            }
        }

        #endregion

        #region 輔助方法

        /// <summary>
        /// 取得權限等級的顯示名稱
        /// </summary>
        private static string GetLevelDisplayName(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.Guest => "訪客 (Guest)",
                AccessLevel.Operator => "操作員 (Operator)",
                AccessLevel.Instructor => "指導員 (Instructor)",
                AccessLevel.Supervisor => "主管 (Supervisor)",
                AccessLevel.Admin => "管理員 (Admin)",
                _ => "未知"
            };
        }

        #endregion
    }
}
