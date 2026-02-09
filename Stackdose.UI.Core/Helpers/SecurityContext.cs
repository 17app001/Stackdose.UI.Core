using Stackdose.UI.Core.Models;
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
        private static readonly string _loginDebugLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_debug.log");

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

            WriteLoginDebug("========================================");
            WriteLoginDebug($"Login START: {userId}");
            WriteLoginDebug("Mode: SQLite Database Authentication");
            WriteLoginDebug("========================================");

            // 🔥 資料庫驗證
            try
            {
                WriteLoginDebug("[Step 1] 嘗試資料庫驗證...");
                var userService = new Stackdose.UI.Core.Services.UserManagementService();
                var dbResult = userService.AuthenticateAsync(userId, password).Result;
                
                if (dbResult.Success && dbResult.User != null)
                {
                    WriteLoginDebug("✅ 資料庫驗證成功");
                    WriteLoginDebug($"   UserId: {dbResult.User.UserId}");
                    WriteLoginDebug($"   DisplayName: {dbResult.User.DisplayName}");
                    WriteLoginDebug($"   AccessLevel: {dbResult.User.AccessLevel}");

                    SessionCoordinator.BeginSession(dbResult.User);
                    
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
                        AutoLogoutCoordinator.Start();
                    }

                    stopwatch.Stop();
                    WriteLoginDebug("========================================");
                    WriteLoginDebug($"✅ Login COMPLETED in {stopwatch.ElapsedMilliseconds}ms");
                    WriteLoginDebug("========================================");
                    
                    return true;
                }
                else
                {
                    WriteLoginDebug($"❌ 資料庫驗證失敗: {dbResult.Message}");
                    
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
                WriteLoginDebug($"❌ 資料庫驗證錯誤: {ex.Message}");
                
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
            var user = AuthenticationCoordinator.CreateQuickLoginUser(level);
            SessionCoordinator.BeginSession(user);

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
                AutoLogoutCoordinator.Start();
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
            SessionCoordinator.EndSession();

            // 3. 觸發事件
            LogoutOccurred?.Invoke(null, EventArgs.Empty);
            AccessLevelChanged?.Invoke(null, EventArgs.Empty);

            // 4. 停止自動登出計時器
            AutoLogoutCoordinator.Stop();
        }

        /// <summary>
        /// 更新最後活動時間（由 UI 操作時呼叫）
        /// </summary>
        public static void UpdateActivity()
        {
            SessionCoordinator.TouchActivity();
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
            AutoLogoutCoordinator.Start();
        }

        private static void StopAutoLogoutTimer()
        {
            AutoLogoutCoordinator.Stop();
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

        private static void WriteLoginDebug(string message)
        {
            try
            {
                File.AppendAllText(_loginDebugLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecurityContext] Login debug log write failed: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[SecurityContext] {message}");
        }

        private static class SessionCoordinator
        {
            public static void BeginSession(UserAccount user)
            {
                CurrentSession.CurrentUser = user;
                CurrentSession.LoginTime = DateTime.Now;
                CurrentSession.LastActivityTime = DateTime.Now;
            }

            public static void EndSession()
            {
                CurrentSession.CurrentUser = null;
            }

            public static void TouchActivity()
            {
                if (CurrentSession.IsLoggedIn)
                {
                    CurrentSession.LastActivityTime = DateTime.Now;
                }
            }
        }

        private static class AuthenticationCoordinator
        {
            public static UserAccount CreateQuickLoginUser(AccessLevel level)
            {
                switch (level)
                {
                    case AccessLevel.Admin:
                    {
                        var adminUser = LoadUserFromDatabase("admin01");
                        if (adminUser != null)
                        {
                            return adminUser;
                        }

                        System.Diagnostics.Debug.WriteLine("[SecurityContext] QuickLogin: admin01 not found in database, creating temporary user");
                        return new UserAccount
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

                    case AccessLevel.Guest:
                        System.Diagnostics.Debug.WriteLine("[SecurityContext] QuickLogin: Creating Guest user");
                        return new UserAccount
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

                    default:
                        System.Diagnostics.Debug.WriteLine($"[SecurityContext] QuickLogin: Creating temporary {level} user");
                        return new UserAccount
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
                }
            }
        }

        private static class AutoLogoutCoordinator
        {
            public static void Start()
            {
                Stop();

                _autoLogoutTimer = new System.Threading.Timer(_ =>
                {
                    if (!CurrentSession.IsLoggedIn || !EnableAutoLogout)
                    {
                        return;
                    }

                    var idleTime = DateTime.Now - CurrentSession.LastActivityTime;
                    if (idleTime.TotalMinutes >= AutoLogoutMinutes)
                    {
                        Application.Current?.Dispatcher.Invoke(() => Logout(isAutoLogout: true));
                    }
                }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }

            public static void Stop()
            {
                _autoLogoutTimer?.Dispose();
                _autoLogoutTimer = null;
            }
        }

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
