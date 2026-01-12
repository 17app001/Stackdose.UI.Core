using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// Windows AD 驗證服務
    /// </summary>
    /// <remarks>
    /// <para>提供 Windows Active Directory 以下功能：</para>
    /// <list type="bullet">
    /// <item>驗證 AD 使用者帳密</item>
    /// <item>取得 AD 使用者資訊（顯示名稱、Email、群組等）</item>
    /// <item>支援 Domain 與 LocalMachine 兩種驗證模式</item>
    /// <item>自動偵測當前登入的 Windows 使用者</item>
    /// <item>支援 App_ 群組權限檢測（App_Operators、App_Instructors、App_Supervisors、App_Admins）</item>
    /// <item>內建超時控制（5秒）防止驗證卡死</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法：
    /// <code>
    /// var adService = new AdAuthenticationService();
    /// 
    /// // 驗證 AD 使用者
    /// var result = adService.Authenticate("username", "password");
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine($"Welcome, {result.DisplayName}!");
    ///     Console.WriteLine($"Permission: {result.PermissionLevel}");
    ///     Console.WriteLine($"Groups: {string.Join(", ", result.UserGroups)}");
    /// }
    /// </code>
    /// </example>
    public class AdAuthenticationService
    {
        #region Constants

        // 四個 App_ 群組常數
        private const string APP_OPERATORS_GROUP = "App_Operators";
        private const string APP_INSTRUCTORS_GROUP = "App_Instructors";
        private const string APP_SUPERVISORS_GROUP = "App_Supervisors";
        private const string APP_ADMINS_GROUP = "App_Admins";

        // 驗證超時設定（5秒）
        private const int VALIDATION_TIMEOUT_MS = 5000;

        #endregion

        #region Private Fields

        private readonly string? _domainName;
        private readonly ContextType _contextType;

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        /// <param name="domainName">AD 網域名稱（null 則自動偵測）</param>
        /// <param name="useLocalMachine">是否使用本機驗證（預設 false，使用 Domain）</param>
        public AdAuthenticationService(string? domainName = null, bool useLocalMachine = false)
        {
            _domainName = domainName;
            _contextType = useLocalMachine ? ContextType.Machine : ContextType.Domain;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] Initialized: Type={_contextType}, Domain={_domainName ?? "Auto-Detect"}");
            #endif
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 驗證使用者帳密 - 完整版本（回傳詳細資訊和群組）
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="password">密碼</param>
        /// <param name="contextType">驗證模式（Domain 或 Machine）</param>
        /// <returns>驗證結果</returns>
        public AuthenticationResult Authenticate(string username, string password, ContextType? contextType = null)
        {
            AuthenticationResult result = new AuthenticationResult();

            try
            {
                // 輸入驗證
                if (string.IsNullOrWhiteSpace(username))
                {
                    result.ErrorMessage = "使用者名稱不可為空";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    result.ErrorMessage = "密碼不可為空";
                    return result;
                }

                // 使用 Task 與 CancellationToken 實作超時機制
                using (var cts = new CancellationTokenSource(VALIDATION_TIMEOUT_MS))
                {
                    var authTask = Task.Run(() => PerformAuthentication(username, password, contextType ?? _contextType), cts.Token);

                    try
                    {
                        // 等待驗證完成或超時
                        if (authTask.Wait(VALIDATION_TIMEOUT_MS))
                        {
                            result = authTask.Result;
                        }
                        else
                        {
                            result.ErrorMessage = "驗證超時，請檢查網路連線或帳號設定";
                            System.Diagnostics.Debug.WriteLine("[WARNING] Authentication timeout");
                        }
                    }
                    catch (AggregateException ae)
                    {
                        // 處理 Task 內部的例外
                        var innerException = ae.InnerException;
                        result.ErrorMessage = "驗證過程發生錯誤";
                        result.ExceptionMessage = innerException?.Message ?? ae.Message;
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Authentication failed: {innerException?.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "系統錯誤";
                result.ExceptionMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 快速驗證 - 只回傳成功/失敗（簡化版，不提供詳細資訊）
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="password">密碼</param>
        /// <returns>驗證是否成功</returns>
        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[AdAuthenticationService] ValidateCredentials: Empty username or password");
                #endif
                return false;
            }

            try
            {
                using (var cts = new CancellationTokenSource(VALIDATION_TIMEOUT_MS))
                {
                    var validationTask = Task.Run(() =>
                    {
                        using (PrincipalContext context = new PrincipalContext(_contextType))
                        {
                            return context.ValidateCredentials(username, password);
                        }
                    }, cts.Token);

                    if (validationTask.Wait(VALIDATION_TIMEOUT_MS))
                    {
                        return validationTask.Result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[WARNING] Validation timeout");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取得 AD 使用者資訊
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <returns>使用者資訊（找不到時回傳 null）</returns>
        public AdUserInfo? GetUserInfo(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            try
            {
                using (var context = CreatePrincipalContext())
                using (var user = UserPrincipal.FindByIdentity(context, username))
                {
                    if (user == null)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] GetUserInfo: User '{username}' not found");
                        #endif
                        return null;
                    }

                    var userInfo = new AdUserInfo
                    {
                        Username = user.SamAccountName ?? username,
                        DisplayName = user.DisplayName ?? username,
                        Email = user.EmailAddress,
                        GivenName = user.GivenName,
                        Surname = user.Surname,
                        Description = user.Description,
                        IsEnabled = user.Enabled ?? true
                    };

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] GetUserInfo: {username} => {userInfo.DisplayName}");
                    #endif

                    return userInfo;
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] GetUserInfo Error: {ex.Message}");
                #endif
                return null;
            }
        }

        /// <summary>
        /// 取得當前登入的 Windows 使用者名稱
        /// </summary>
        /// <returns>使用者名稱（不含 Domain）</returns>
        public static string GetCurrentWindowsUser()
        {
            try
            {
                string fullName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                
                // 移除 Domain 前綴（例如：DOMAIN\username -> username）
                if (fullName.Contains("\\"))
                {
                    return fullName.Split('\\').Last();
                }
                
                return fullName;
            }
            catch
            {
                return Environment.UserName;
            }
        }

        /// <summary>
        /// 取得當前登入的 Windows 使用者完整名稱（含 Domain）
        /// </summary>
        /// <returns>完整使用者名稱（例如：DOMAIN\username）</returns>
        public static string GetCurrentWindowsUserWithDomain()
        {
            try
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            catch
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }
        }

        /// <summary>
        /// 檢查 AD 服務是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public bool IsAvailable()
        {
            try
            {
                using (var context = CreatePrincipalContext())
                {
                    // 嘗試查詢當前使用者，以測試 AD 連線
                    var currentUser = GetCurrentWindowsUser();
                    using (var user = UserPrincipal.FindByIdentity(context, currentUser))
                    {
                        return user != null;
                    }
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] IsAvailable Error: {ex.Message}");
                #endif
                return false;
            }
        }

        /// <summary>
        /// 檢查是否在網域環境中
        /// </summary>
        /// <returns>如果在網域環境則回傳 true，否則回傳 false</returns>
        public static bool IsInDomain()
        {
            try
            {
                return !string.IsNullOrEmpty(Environment.UserDomainName) &&
                       !Environment.UserDomainName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 取得建議的驗證模式 - 根據環境自動判斷
        /// </summary>
        /// <returns>建議的 ContextType</returns>
        public static ContextType GetRecommendedContextType()
        {
            return IsInDomain() ? ContextType.Domain : ContextType.Machine;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 執行驗證的核心方法（背景執行）
        /// </summary>
        private AuthenticationResult PerformAuthentication(string username, string password, ContextType contextType)
        {
            AuthenticationResult result = new AuthenticationResult();

            try
            {
                // 建立 Principal Context
                using (PrincipalContext context = new PrincipalContext(contextType))
                {
                    // 快速驗證帳密
                    System.Diagnostics.Debug.WriteLine($"[INFO] Validating credentials for user: {username}");
                    
                    bool isValid = context.ValidateCredentials(username, password);

                    if (isValid)
                    {
                        System.Diagnostics.Debug.WriteLine("[INFO] Credentials validated successfully");

                        // 驗證成功，取得使用者詳細資訊
                        using (UserPrincipal? userPrincipal = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (userPrincipal != null)
                            {
                                // 取得網域或機器名稱
                                string domain = contextType == ContextType.Domain
                                    ? Environment.UserDomainName
                                    : Environment.MachineName;

                                // 設定驗證結果
                                result.IsSuccess = true;
                                result.FullUsername = $"{domain}\\{userPrincipal.SamAccountName}";
                                result.DisplayName = userPrincipal.DisplayName ?? username;
                                result.Email = userPrincipal.EmailAddress ?? "N/A";
                                
                                // 取得使用者群組資訊
                                result.UserGroups = GetUserGroups(userPrincipal);
                                
                                // 判斷權限等級
                                result.PermissionLevel = DeterminePermissionLevel(result.UserGroups);

                                System.Diagnostics.Debug.WriteLine($"[INFO] User authenticated: {result.FullUsername}");
                                System.Diagnostics.Debug.WriteLine($"[INFO] Permission Level: {result.PermissionLevel}");
                                System.Diagnostics.Debug.WriteLine($"[INFO] Groups: {string.Join(", ", result.UserGroups)}");
                            }
                            else
                            {
                                // 備援：無法取得 UserPrincipal 時使用基本資訊
                                result.IsSuccess = true;
                                result.FullUsername = $"{Environment.MachineName}\\{username}";
                                result.DisplayName = username;
                                result.Email = "N/A";
                                result.UserGroups = new List<string> { "Users" };
                                result.PermissionLevel = "Standard User";
                            }
                        }
                    }
                    else
                    {
                        // 驗證失敗 - 快速回傳
                        result.ErrorMessage = "帳號或密碼錯誤";
                        System.Diagnostics.Debug.WriteLine($"[WARNING] Invalid credentials for user: {username}");
                    }
                }
            }
            catch (PrincipalServerDownException ex)
            {
                result.ErrorMessage = "無法連線驗證伺服器";
                result.ExceptionMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERROR] Server down: {ex.Message}");
            }
            catch (PrincipalOperationException ex)
            {
                result.ErrorMessage = "驗證操作錯誤，請檢查帳號設定";
                result.ExceptionMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERROR] Operation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "系統錯誤";
                result.ExceptionMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 取得使用者所屬的群組清單
        /// </summary>
        /// <param name="userPrincipal">使用者主體</param>
        /// <returns>群組名稱清單</returns>
        private List<string> GetUserGroups(UserPrincipal userPrincipal)
        {
            List<string> groups = new List<string>();

            try
            {
                // 取得使用者所屬的群組
                var groupCollection = userPrincipal.GetGroups();
                
                foreach (Principal group in groupCollection)
                {
                    groups.Add(group.Name);
                }

                // 如果沒有取得任何群組，至少加入預設群組
                if (groups.Count == 0)
                {
                    groups.Add("Users");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] Failed to retrieve user groups: {ex.Message}");
                groups.Add("Users"); // 預設群組
            }

            return groups;
        }

        /// <summary>
        /// 根據群組成員關係判斷使用者的權限等級
        /// </summary>
        /// <param name="groups">使用者所屬群組</param>
        /// <returns>權限等級描述</returns>
        private string DeterminePermissionLevel(List<string> groups)
        {
            // 將群組名稱轉為小寫以便忽略大小寫差異
            var groupsLower = groups.Select(g => g.ToLower()).ToList();

            // 判斷權限等級（從高到低）
            // L4: App_Admins
            if (groupsLower.Contains(APP_ADMINS_GROUP.ToLower()))
            {
                return "Admin (L4)";
            }
            // L3: App_Supervisors
            else if (groupsLower.Contains(APP_SUPERVISORS_GROUP.ToLower()))
            {
                return "Supervisor (L3)";
            }
            // L2: App_Instructors
            else if (groupsLower.Contains(APP_INSTRUCTORS_GROUP.ToLower()))
            {
                return "Instructor (L2)";
            }
            // L1: App_Operators
            else if (groupsLower.Contains(APP_OPERATORS_GROUP.ToLower()))
            {
                return "Operator (L1)";
            }
            // Domain/Local Admins
            else if (groupsLower.Any(g => g.Contains("domain admins") || g.Contains("enterprise admins")))
            {
                return "Domain Administrator";
            }
            else if (groupsLower.Any(g => g.Contains("administrators") || g.Contains("admin")))
            {
                return "Administrator";
            }
            // Standard Users
            else if (groupsLower.Any(g => g.Contains("users")))
            {
                return "Standard User";
            }
            else
            {
                return "Standard User"; // 預設權限
            }
        }

        /// <summary>
        /// 建立 PrincipalContext
        /// </summary>
        private PrincipalContext CreatePrincipalContext()
        {
            try
            {
                if (_contextType == ContextType.Domain && !string.IsNullOrWhiteSpace(_domainName))
                {
                    return new PrincipalContext(ContextType.Domain, _domainName);
                }
                else
                {
                    return new PrincipalContext(_contextType);
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] CreatePrincipalContext Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] Falling back to LocalMachine context");
                #endif
                
                // Fallback 到本機驗證
                return new PrincipalContext(ContextType.Machine);
            }
        }

        #endregion
    }

    #region AuthenticationResult Class

    /// <summary>
    /// 驗證結果類別 - 回傳完整的驗證資訊與錯誤訊息
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>驗證是否成功</summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>完整使用者名稱 (Domain\Username)</summary>
        public string FullUsername { get; set; } = string.Empty;
        
        /// <summary>使用者顯示名稱</summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>使用者電子郵件</summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>使用者所屬群組清單</summary>
        public List<string> UserGroups { get; set; } = new List<string>();
        
        /// <summary>使用者權限等級（根據群組判定）</summary>
        public string PermissionLevel { get; set; } = "Standard User";
        
        /// <summary>錯誤訊息（驗證失敗時）</summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>例外訊息（發生例外時）</summary>
        public string ExceptionMessage { get; set; } = string.Empty;
    }

    #endregion

    #region AdUserInfo Class

    /// <summary>
    /// AD 使用者資訊
    /// </summary>
    public class AdUserInfo
    {
        /// <summary>使用者名稱（SamAccountName）</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>顯示名稱</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Email</summary>
        public string? Email { get; set; }

        /// <summary>名字</summary>
        public string? GivenName { get; set; }

        /// <summary>姓氏</summary>
        public string? Surname { get; set; }

        /// <summary>描述</summary>
        public string? Description { get; set; }

        /// <summary>是否啟用</summary>
        public bool IsEnabled { get; set; } = true;
    }

    #endregion
}
