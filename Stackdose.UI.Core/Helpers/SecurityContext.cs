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
        /// 使用者登入 - 純 Windows AD 驗證（不需要資料庫帳號）
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
            WriteLog($"Mode: Pure Windows AD (No Database Check)");
            WriteLog($"========================================");

            // 🔥 步驟 1：Windows AD 驗證（使用完整的 Authenticate 取得群組資訊）
            AuthenticationResult? adResult = null;

            if (EnableAdAuthentication)
            {
                try
                {
                    WriteLog($"AD Authentication enabled (LocalMachine: {UseLocalMachineOnly})");
                    var adStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // 🔥 呼叫 AD 驗證
                    adResult = AdService.Authenticate(userId, password);
                    
                    adStopwatch.Stop();
                    WriteLog($"AD Authentication took {adStopwatch.ElapsedMilliseconds}ms - Result: {adResult.IsSuccess}");
                    
                    if (adResult.IsSuccess)
                    {
                        WriteLog($"✅ AD Authentication SUCCESS: {userId}");
                        WriteLog($"   DisplayName: {adResult.DisplayName}");
                        WriteLog($"   Permission Level: {adResult.PermissionLevel}");
                        WriteLog($"   Groups: {string.Join(", ", adResult.UserGroups)}");
                        
                        ComplianceContext.LogSystem(
                            $"[AD] Authentication Success: {userId} - Groups: {string.Join(", ", adResult.UserGroups)}",
                            LogLevel.Success,
                            showInUi: true
                        );
                    }
                    else
                    {
                        WriteLog($"❌ AD Authentication FAILED: {userId}");
                        WriteLog($"   Error: {adResult.ErrorMessage}");
                        
                        ComplianceContext.LogSystem(
                            $"[AD] Login Failed: {userId} - {adResult.ErrorMessage}",
                            LogLevel.Warning,
                            showInUi: true
                        );
                        
                        ComplianceContext.LogAuditTrail(
                            "User Login",
                            userId,
                            "N/A",
                            "Failed (Invalid Credentials)",
                            $"Windows AD: {adResult.ErrorMessage}",
                            showInUi: false
                        );
                        
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"❌ AD Authentication Error: {ex.Message}");
                    WriteLog($"   Stack: {ex.StackTrace}");
                    
                    ComplianceContext.LogSystem(
                        $"[AD] Authentication Error: {ex.Message}",
                        LogLevel.Error,
                        showInUi: true
                    );
                    
                    return false;
                }
            }
            else
            {
                WriteLog($"❌ AD Authentication is DISABLED");
                ComplianceContext.LogSystem(
                    "[AD] Authentication is disabled - cannot login",
                    LogLevel.Error,
                    showInUi: true
                );
                return false;
            }

            // 🔥 步驟 2：檢查 AD 群組（必須屬於 App_ 群組之一）
            if (adResult == null || !adResult.IsSuccess)
            {
                WriteLog($"❌ Login Failed: AD verification failed");
                return false;
            }

            // 🔥 判斷 AccessLevel
            var accessLevel = UserManagementService.DetermineAccessLevelFromAdGroups(adResult.UserGroups);
            
            WriteLog($"✅ AccessLevel determined: {accessLevel}");
            WriteLog($"   Based on groups: {string.Join(", ", adResult.UserGroups)}");

            // 🔥 檢查是否屬於任何 App_ 群組
            if (accessLevel == AccessLevel.Guest)
            {
                WriteLog($"❌ Login Failed: User is not in any App_ group");
                WriteLog($"   User groups: {string.Join(", ", adResult.UserGroups)}");
                WriteLog($"   Required: App_Operators, App_Instructors, App_Supervisors, or App_Admins");
                
                ComplianceContext.LogSystem(
                    $"[AD] Login Failed: User '{userId}' is not in any App_ group. Current groups: {string.Join(", ", adResult.UserGroups)}",
                    LogLevel.Warning,
                    showInUi: true
                );
                
                ComplianceContext.LogAuditTrail(
                    "User Login",
                    userId,
                    "N/A",
                    "Failed (No App_ Group)",
                    $"User is not in App_Operators, App_Instructors, App_Supervisors, or App_Admins. Groups: {string.Join(", ", adResult.UserGroups)}",
                    showInUi: false
                );
                
                return false;
            }

            // 🔥 步驟 3：建立 UserAccount 物件（從 AD 資訊）
            var user = new UserAccount
            {
                Id = adResult.UserGroups.GetHashCode(), // 🔥 使用 HashCode 作為臨時 ID
                UserId = userId,
                DisplayName = adResult.DisplayName,
                Email = adResult.Email,
                AccessLevel = accessLevel,
                IsActive = true,
                CreatedBy = "Windows AD",
                CreatedAt = DateTime.Now,
                Department = string.Join(", ", adResult.UserGroups),
                Remarks = $"Windows AD User - Groups: {string.Join(", ", adResult.UserGroups)}"
            };

            WriteLog($"✅ UserAccount created from AD:");
            WriteLog($"   UserId: {user.UserId}");
            WriteLog($"   DisplayName: {user.DisplayName}");
            WriteLog($"   AccessLevel: {user.AccessLevel}");
            WriteLog($"   Email: {user.Email}");

            stopwatch.Stop();
            WriteLog($"========================================");
            WriteLog($"✅ Login COMPLETED in {stopwatch.ElapsedMilliseconds}ms");
            WriteLog($"   Auth Method: Windows AD");
            WriteLog($"   AccessLevel: {user.AccessLevel}");
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
                $"Login from {Environment.MachineName} via Windows AD (Groups: {string.Join(", ", adResult.UserGroups)})",
                showInUi: true
            );

            ComplianceContext.LogSystem(
                $"✅ Login Success: {user.DisplayName} ({user.AccessLevel}) via Windows AD",
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

        #region 資料庫存取 (已停用 - 改用純 Windows AD)

        /// <summary>
        /// 🔥 已停用：從資料庫載入使用者（改用純 Windows AD）
        /// </summary>
        /// <remarks>
        /// 此方法僅保留給 QuickLogin() 使用（測試用途）
        /// 正常登入流程不會使用資料庫，所有使用者資訊來自 Windows AD
        /// </remarks>
        [Obsolete("No longer used in production - all user data comes from Windows AD")]
        private static UserAccount? LoadUserFromDatabase(string userId)
        {
            try
            {
                // 🔥 從真實資料庫讀取（僅 QuickLogin 測試用）
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                
                var user = conn.QueryFirstOrDefault<UserAccount>(
                    "SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1",
                    new { UserId = userId }
                );
                
                #if DEBUG
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] ⚠️ LoadUserFromDatabase: Found legacy user in DB: {userId}");
                    System.Diagnostics.Debug.WriteLine($"[SecurityContext] Note: Normal login uses Windows AD, not database");
                }
                #endif
                
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
