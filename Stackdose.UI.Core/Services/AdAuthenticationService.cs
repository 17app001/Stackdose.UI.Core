using System;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// Windows AD 身份驗證服務
    /// </summary>
    /// <remarks>
    /// <para>提供 Windows Active Directory 整合功能：</para>
    /// <list type="bullet">
    /// <item>驗證 AD 使用者憑證</item>
    /// <item>取得 AD 使用者資訊（顯示名稱、Email、部門等）</item>
    /// <item>支援 Domain 和 LocalMachine 兩種驗證模式</item>
    /// <item>自動偵測當前登入的 Windows 使用者</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法：
    /// <code>
    /// var adService = new AdAuthenticationService();
    /// 
    /// // 驗證 AD 使用者
    /// if (adService.ValidateCredentials("username", "password"))
    /// {
    ///     var userInfo = adService.GetUserInfo("username");
    ///     Console.WriteLine($"Welcome, {userInfo.DisplayName}!");
    /// }
    /// </code>
    /// </example>
    public class AdAuthenticationService
    {
        #region Private Fields

        private readonly string? _domainName;
        private readonly ContextType _contextType;

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        /// <param name="domainName">AD 網域名稱（null 為自動偵測）</param>
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
        /// 驗證使用者憑證
        /// </summary>
        /// <param name="username">使用者名稱（不含 Domain）</param>
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
                using (var context = CreatePrincipalContext())
                {
                    bool isValid = context.ValidateCredentials(username, password);

                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] ValidateCredentials: {username} = {(isValid ? "SUCCESS" : "FAILED")}");
                    #endif

                    return isValid;
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdAuthenticationService] ValidateCredentials Error: {ex.Message}");
                #endif
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
                    // 嘗試查詢當前使用者，以驗證 AD 連線
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

        #endregion

        #region Private Methods

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
                
                // Fallback 到本機模式
                return new PrincipalContext(ContextType.Machine);
            }
        }

        #endregion
    }

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
