using Stackdose.UI.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

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
            // 1. 從資料庫查詢使用者
            var user = LoadUserFromDatabase(userId);
            if (user == null || !user.IsActive)
            {
                ComplianceContext.LogSystem(
                    $"Login Failed: User '{userId}' not found or inactive",
                    LogLevel.Warning,
                    showInUi: true
                );
                return false;
            }

            // 2. 驗證密碼
            string passwordHash = HashPassword(password);
            if (user.PasswordHash != passwordHash)
            {
                ComplianceContext.LogSystem(
                    $"Login Failed: Invalid password for user '{userId}'",
                    LogLevel.Warning,
                    showInUi: true
                );
                
                // ?? Audit Trail：登入失敗
                ComplianceContext.LogAuditTrail(
                    "User Login",
                    userId,
                    "N/A",
                    "Failed",
                    "Invalid password",
                    showInUi: false
                );
                return false;
            }

            // 3. 登入成功
            CurrentSession.CurrentUser = user;
            CurrentSession.LoginTime = DateTime.Now;
            CurrentSession.LastActivityTime = DateTime.Now;
            user.LastLoginAt = DateTime.Now;

            // 4. 記錄到 Audit Trail
            ComplianceContext.LogAuditTrail(
                "User Login",
                userId,
                "Logged Out",
                $"Logged In (Level {(int)user.AccessLevel} - {user.AccessLevel})",
                $"Login from {Environment.MachineName}",
                showInUi: true
            );

            ComplianceContext.LogSystem(
                $"[OK] Login Success: {user.DisplayName} ({user.AccessLevel})",
                LogLevel.Success,
                showInUi: true
            );

            // 5. 觸發事件
            LoginSuccess?.Invoke(null, user);
            AccessLevelChanged?.Invoke(null, EventArgs.Empty);

            // 6. 啟動自動登出計時器
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
            var user = new UserAccount
            {
                UserId = level.ToString().ToLower(),
                DisplayName = GetLevelDisplayName(level),
                PasswordHash = HashPassword("1234"),
                AccessLevel = level,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.Now
            };

            CurrentSession.CurrentUser = user;
            CurrentSession.LoginTime = DateTime.Now;
            CurrentSession.LastActivityTime = DateTime.Now;

            ComplianceContext.LogSystem(
                $"[QUICK] Quick Login: {user.DisplayName}",
                LogLevel.Info,
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
        /// SHA-256 密碼雜湊
        /// </summary>
        /// <param name="password">明文密碼</param>
        /// <returns>雜湊後的密碼</returns>
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        #endregion

        #region 資料庫存取 (Placeholder)

        /// <summary>
        /// 從資料庫載入使用者（暫時使用預設帳號）
        /// </summary>
        private static UserAccount? LoadUserFromDatabase(string userId)
        {
            // ?? TODO: 從 SQLite 讀取
            // 暫時回傳預設測試帳號
            var defaultAccounts = new Dictionary<string, UserAccount>
            {
                ["admin"] = new UserAccount
                {
                    UserId = "admin",
                    DisplayName = "系統管理員",
                    PasswordHash = HashPassword("1234"),
                    AccessLevel = AccessLevel.Admin,
                    IsActive = true,
                    CreatedBy = "System"
                },
                ["engineer"] = new UserAccount
                {
                    UserId = "engineer",
                    DisplayName = "工程師",
                    PasswordHash = HashPassword("1234"),
                    AccessLevel = AccessLevel.Admin,
                    IsActive = true,
                    CreatedBy = "System"
                },
                ["supervisor"] = new UserAccount
                {
                    UserId = "supervisor",
                    DisplayName = "主管",
                    PasswordHash = HashPassword("1234"),
                    AccessLevel = AccessLevel.Supervisor,
                    IsActive = true,
                    CreatedBy = "System"
                },
                ["instructor"] = new UserAccount
                {
                    UserId = "instructor",
                    DisplayName = "指導員",
                    PasswordHash = HashPassword("1234"),
                    AccessLevel = AccessLevel.Instructor,
                    IsActive = true,
                    CreatedBy = "System"
                },
                ["operator"] = new UserAccount
                {
                    UserId = "operator",
                    DisplayName = "操作員",
                    PasswordHash = HashPassword("1234"),
                    AccessLevel = AccessLevel.Operator,
                    IsActive = true,
                    CreatedBy = "System"
                }
            };

            return defaultAccounts.TryGetValue(userId.ToLower(), out var account) ? account : null;
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
