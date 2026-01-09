using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

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
        /// 自動登出時間（分鐘，預設 15）
        /// </summary>
        public static int AutoLogoutMinutes { get; set; } = 15;

        /// <summary>
        /// 是否啟用自動登出功能
        /// </summary>
        public static bool EnableAutoLogout { get; set; } = true;

        /// <summary>
        /// 🔥 新增：是否啟用 AD 驗證（預設 true）
        /// </summary>
        #if DEBUG
        public static bool EnableAdAuthentication { get; set; } = true; // 🔥 DEBUG 也啟用（但用本機驗證）
        #else
        public static bool EnableAdAuthentication { get; set; } = true;
        #endif

        /// <summary>
        /// 🔥 新增：是否僅使用本機 Windows 驗證（不連網域，速度快）
        /// </summary>
        public static bool UseLocalMachineOnly { get; set; } = true; // 🔥 預設使用本機驗證

        /// <summary>
        /// 🔥 新增：AD 驗證服務實例
        /// </summary>
        private static AdAuthenticationService? _adService;

        /// <summary>
        /// 🔥 新增：取得 AD 驗證服務實例
        /// </summary>
        public static AdAuthenticationService AdService
        {
            get
            {
                if (_adService == null)
                {
                    // 🔥 根據設定決定使用 Domain 或 LocalMachine
                    _adService = new AdAuthenticationService(
                        domainName: null, 
                        useLocalMachine: UseLocalMachineOnly // ← 使用本機驗證
                    );
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] AdAuthenticationService initialized (LocalMachine: {UseLocalMachineOnly})");
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] Current Windows User: {AdAuthenticationService.GetCurrentWindowsUserWithDomain()}");
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] AD Available: {_adService.IsAvailable()}");
                    #endif
                }
                return _adService;
            }
        }

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
        /// 使用者登入
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
            WriteLog($"========================================");

            // 🔥 步驟 1：優先嘗試 AD 驗證（如果啟用）
            bool adVerified = false;
            AdUserInfo? adUserInfo = null;

            if (EnableAdAuthentication)
            {
                try
                {
                    WriteLog($"AD Authentication enabled (LocalMachine: {UseLocalMachineOnly})");
                    var adStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    adVerified = AdService.ValidateCredentials(userId, password);
                    
                    adStopwatch.Stop();
                    WriteLog($"AD Authentication took {adStopwatch.ElapsedMilliseconds}ms - Result: {adVerified}");
                    
                    if (adVerified)
                    {
                        adUserInfo = AdService.GetUserInfo(userId);
                        
                        WriteLog($"AD Authentication SUCCESS: {userId}");
                        WriteLog($"AD DisplayName: {adUserInfo?.DisplayName}");
                        
                        ComplianceContext.LogSystem(
                            $"[AD] Authentication Success: {userId} (Windows AD)",
                            LogLevel.Success,
                            showInUi: true
                        );
                    }
                    else
                    {
                        WriteLog($"AD Authentication FAILED: {userId}");
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"AD Authentication Error: {ex.Message}");
                    
                    ComplianceContext.LogSystem(
                        $"[AD] Authentication Error: {ex.Message}",
                        LogLevel.Warning,
                        showInUi: false
                    );
                }
            }
            else
            {
                WriteLog($"AD Authentication disabled");
            }

            WriteLog($"Loading user from database...");
            var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 🔥 步驟 2：從本地資料庫查詢使用者（必須存在，才能取得權限）
            var user = LoadUserFromDatabase(userId);
            
            dbStopwatch.Stop();
            WriteLog($"Database load took {dbStopwatch.ElapsedMilliseconds}ms - User found: {user != null}");
            
            if (user == null)
            {
                // 🔥 如果 AD 驗證通過，但本地資料庫沒有該使用者，提示管理員建立
                if (adVerified && adUserInfo != null)
                {
                    WriteLog($"Login Failed: AD verified but user not in database");
                    
                    ComplianceContext.LogSystem(
                        $"[AD] Login Failed: User '{userId}' verified by AD but not found in local database. Please contact administrator to create account.",
                        LogLevel.Warning,
                        showInUi: true
                    );
                    
                    ComplianceContext.LogAuditTrail(
                        "User Login",
                        userId,
                        "N/A",
                        "Failed (Not in local database)",
                        "AD verified but account not created",
                        showInUi: false
                    );
                    
                    return false;
                }
                
                // 原有邏輯：使用者不存在
                WriteLog($"Login Failed: User not found");
                
                ComplianceContext.LogSystem(
                    $"Login Failed: User '{userId}' not found",
                    LogLevel.Warning,
                    showInUi: true
                );
                
                ComplianceContext.LogAuditTrail(
                    "User Login",
                    userId,
                    "N/A",
                    "Failed (User not found)",
                    "Account does not exist",
                    showInUi: false
                );
                return false;
            }
            
            if (!user.IsActive)
            {
                WriteLog($"Login Failed: User inactive");
                
                ComplianceContext.LogSystem(
                    $"Login Failed: User '{userId}' is inactive",
                    LogLevel.Warning,
                    showInUi: true
                );
                
                ComplianceContext.LogAuditTrail(
                    "User Login",
                    userId,
                    "N/A",
                    "Failed (Account inactive)",
                    "Account has been disabled",
                    showInUi: false
                );
                return false;
            }

            // 🔥 步驟 3：驗證密碼
            bool passwordValid = false;
            string authMethod = "Unknown";

            WriteLog($"Verifying password...");
            var pwdStopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (adVerified)
            {
                // AD 驗證成功，直接通過
                passwordValid = true;
                authMethod = "Windows AD";
                
                // 🔥 更新本地使用者資訊（從 AD 同步）
                if (adUserInfo != null)
                {
                    user.DisplayName = adUserInfo.DisplayName;
                    user.Email = adUserInfo.Email;
                }
                
                WriteLog($"Password validated via AD");
            }
            else
            {
                // 使用本地密碼驗證
                passwordValid = VerifyPassword(password, user.PasswordHash, user.Salt);
                authMethod = "Local Database";
                
                pwdStopwatch.Stop();
                WriteLog($"Password verification took {pwdStopwatch.ElapsedMilliseconds}ms - Result: {passwordValid}");
                
                if (!passwordValid)
                {
                    WriteLog($"Login Failed: Invalid password");
                    
                    ComplianceContext.LogSystem(
                        $"Login Failed: Invalid password for user '{userId}'",
                        LogLevel.Warning,
                        showInUi: true
                    );
                    
                    ComplianceContext.LogAuditTrail(
                        "User Login",
                        userId,
                        "N/A",
                        "Failed (Wrong password)",
                        "Invalid password",
                        showInUi: false
                    );
                    return false;
                }
            }

            stopwatch.Stop();
            WriteLog($"========================================");
            WriteLog($"Login COMPLETED in {stopwatch.ElapsedMilliseconds}ms");
            WriteLog($"Auth Method: {authMethod}");
            WriteLog($"========================================");

            // 🔥 步驟 4：登入成功
            CurrentSession.CurrentUser = user;
            CurrentSession.LoginTime = DateTime.Now;
            CurrentSession.LastActivityTime = DateTime.Now;
            user.LastLoginAt = DateTime.Now;

            // 記錄到 Audit Trail
            ComplianceContext.LogAuditTrail(
                "User Login",
                userId,
                "Logged Out",
                $"Logged In (Level {(int)user.AccessLevel} - {user.AccessLevel})",
                $"Login from {Environment.MachineName} via {authMethod}",
                showInUi: true
            );

            ComplianceContext.LogSystem(
                $"[OK] Login Success: {user.DisplayName} ({user.AccessLevel}) via {authMethod}",
                LogLevel.Success,
                showInUi: true
            );

            // 觸發事件
            LoginSuccess?.Invoke(null, user);
            AccessLevelChanged?.Invoke(null, EventArgs.Empty);

            // 啟動自動登出計時器
            if (EnableAutoLogout)
            {
                StartAutoLogoutTimer();
            }

            #if DEBUG
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"[SecurityContext] Login END: {userId} - Duration: {stopwatch.ElapsedMilliseconds}ms");
            #endif

            return true;
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
                    // 嘗試從資料庫載入 Admin
                    user = LoadUserFromDatabase("Admin");
                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[SecurityContext] QuickLogin: Admin not found in database, creating temporary user");
                        user = new UserAccount
                        {
                            Id = 1,
                            UserId = "Admin",
                            DisplayName = "系統管理員 (Admin)",
                            PasswordHash = HashPassword("admin123"),
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

        #region 資料庫存取 (Placeholder)

        /// <summary>
        /// 從資料庫載入使用者
        /// </summary>
        private static UserAccount? LoadUserFromDatabase(string userId)
        {
            try
            {
                // 🔥 從真實資料庫讀取
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
                
                // 🔥 Fallback 到內建帳號
                var defaultAccounts = new Dictionary<string, UserAccount>
                {
                    ["Admin"] = new UserAccount
                    {
                        Id = 1,
                        UserId = "Admin",
                        DisplayName = "系統管理員",
                        PasswordHash = HashPassword("admin123"),
                        AccessLevel = AccessLevel.Admin,
                        IsActive = true,
                        CreatedBy = "System"
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
