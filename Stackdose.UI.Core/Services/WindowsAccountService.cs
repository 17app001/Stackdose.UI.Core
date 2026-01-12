using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// Windows 帳號管理服務（比照 WinFormsAD 的 AccountService）
    /// 用途：直接操作 Windows 本機/網域使用者和群組
    /// </summary>
    /// <remarks>
    /// 此服務需要管理員權限才能執行大部分操作
    /// </remarks>
    public class WindowsAccountService
    {
        #region Constants

        private const string APP_OPERATORS_GROUP = "App_Operators";
        private const string APP_INSTRUCTORS_GROUP = "App_Instructors";
        private const string APP_SUPERVISORS_GROUP = "App_Supervisors";
        private const string APP_ADMINS_GROUP = "App_Admins";

        #endregion

        #region Fields

        private readonly ContextType _contextType;

        #endregion

        #region Nested Classes

        /// <summary>
        /// 操作結果
        /// </summary>
        public class OperationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ErrorDetail { get; set; } = string.Empty;
        }

        /// <summary>
        /// 使用者資訊
        /// </summary>
        public class UserInfo
        {
            public string SamAccountName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool IsEnabled { get; set; }
            public string Description { get; set; } = string.Empty;
            public DateTime? LastLogon { get; set; }
            public DateTime? AccountExpirationDate { get; set; }
            public List<string> Groups { get; set; } = new List<string>();
        }

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="contextType">驗證模式（預設為本機）</param>
        public WindowsAccountService(ContextType contextType = ContextType.Machine)
        {
            _contextType = contextType;
        }

        #endregion

        #region List Users

        /// <summary>
        /// 列出所有屬於 App_ 群組的使用者
        /// </summary>
        /// <returns>使用者清單</returns>
        public List<UserInfo> ListAllAppUsers()
        {
            var allUsers = new List<UserInfo>();

            try
            {
                var groups = new[] { APP_OPERATORS_GROUP, APP_INSTRUCTORS_GROUP, APP_SUPERVISORS_GROUP, APP_ADMINS_GROUP };

                foreach (var groupName in groups)
                {
                    var users = ListUsersInGroup(groupName);
                    foreach (var user in users)
                    {
                        // 避免重複
                        if (!allUsers.Any(u => u.SamAccountName.Equals(user.SamAccountName, StringComparison.OrdinalIgnoreCase)))
                        {
                            allUsers.Add(user);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Retrieved {allUsers.Count} users from App_ groups");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] ListAllAppUsers Error: {ex.Message}");
            }

            return allUsers;
        }

        /// <summary>
        /// 列出指定群組中的所有使用者
        /// </summary>
        /// <param name="groupName">群組名稱</param>
        /// <returns>使用者清單</returns>
        public List<UserInfo> ListUsersInGroup(string groupName)
        {
            var users = new List<UserInfo>();

            try
            {
                using (var context = new PrincipalContext(_contextType))
                {
                    using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                    {
                        if (group == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Group '{groupName}' not found");
                            return users;
                        }

                        var members = group.GetMembers();

                        foreach (var member in members)
                        {
                            if (member is UserPrincipal userPrincipal)
                            {
                                var userInfo = new UserInfo
                                {
                                    SamAccountName = userPrincipal.SamAccountName ?? "N/A",
                                    DisplayName = userPrincipal.DisplayName ?? userPrincipal.SamAccountName ?? "N/A",
                                    IsEnabled = userPrincipal.Enabled ?? false,
                                    Description = userPrincipal.Description ?? string.Empty,
                                    LastLogon = userPrincipal.LastLogon,
                                    AccountExpirationDate = userPrincipal.AccountExpirationDate
                                };

                                // 取得使用者所屬群組
                                userInfo.Groups = GetUserGroups(userPrincipal.SamAccountName);

                                users.Add(userInfo);
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Retrieved {users.Count} users from '{groupName}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] ListUsersInGroup Error: {ex.Message}");
            }

            return users;
        }

        /// <summary>
        /// 取得使用者所屬的所有群組
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <returns>群組清單</returns>
        public List<string> GetUserGroups(string username)
        {
            var groups = new List<string>();

            try
            {
                // 移除 Domain\ 前綴（如果有）
                if (username.Contains("\\"))
                {
                    username = username.Split('\\')[1];
                }

                using (var context = new PrincipalContext(_contextType))
                {
                    using (var user = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (user != null)
                        {
                            var groupCollection = user.GetGroups();
                            foreach (Principal group in groupCollection)
                            {
                                groups.Add(group.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] GetUserGroups Error: {ex.Message}");
            }

            return groups;
        }

        #endregion

        #region Create User

        /// <summary>
        /// 建立新使用者並加入指定群組
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="password">密碼</param>
        /// <param name="displayName">顯示名稱</param>
        /// <param name="groupName">要加入的群組名稱（預設 App_Operators）</param>
        /// <param name="description">帳號描述</param>
        /// <returns>操作結果</returns>
        public OperationResult CreateUser(string username, string password, string displayName, string groupName = APP_OPERATORS_GROUP, string description = "")
        {
            var result = new OperationResult();

            try
            {
                // 檢查是否以系統管理員身分執行
                if (!IsRunningAsAdministrator())
                {
                    result.Message = "此操作需要系統管理員權限，請以系統管理員身分執行程式";
                    return result;
                }

                // 驗證輸入
                if (string.IsNullOrWhiteSpace(username))
                {
                    result.Message = "使用者名稱不可為空白";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    result.Message = "密碼不可為空白";
                    return result;
                }

                // 檢查使用者是否已存在
                using (var context = new PrincipalContext(_contextType))
                {
                    using (var existingUser = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (existingUser != null)
                        {
                            result.Message = $"使用者 '{username}' 已存在";
                            return result;
                        }
                    }

                    // 建立新使用者
                    using (var user = new UserPrincipal(context))
                    {
                        user.SamAccountName = username;
                        user.SetPassword(password);
                        user.DisplayName = displayName ?? username;
                        user.Description = description;
                        user.UserCannotChangePassword = false;
                        user.PasswordNeverExpires = true;
                        user.Enabled = true;

                        user.Save();

                        System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] User '{username}' created successfully");
                    }

                    // 加入群組
                    using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                    {
                        if (group == null)
                        {
                            result.Message = $"警告：使用者已建立，但找不到群組 '{groupName}'";
                            result.Success = true;
                            return result;
                        }

                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                group.Members.Add(user);
                                group.Save();

                                result.Success = true;
                                result.Message = $"已建立使用者 '{username}' 並加入群組 '{groupName}'";

                                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] User '{username}' added to group '{groupName}'");
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Message = "權限不足，請以系統管理員身分執行";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Unauthorized: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Message = "建立使用者失敗";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] CreateUser Error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Update User

        /// <summary>
        /// 更新使用者帳號狀態（啟用/停用）
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="enable">true=啟用, false=停用</param>
        /// <returns>操作結果</returns>
        public OperationResult UpdateUserStatus(string username, bool enable)
        {
            var result = new OperationResult();

            try
            {
                // 檢查是否以系統管理員身分執行
                if (!IsRunningAsAdministrator())
                {
                    result.Message = "此操作需要系統管理員權限";
                    return result;
                }

                using (var context = new PrincipalContext(_contextType))
                {
                    using (var user = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (user == null)
                        {
                            result.Message = $"找不到使用者: {username}";
                            return result;
                        }

                        user.Enabled = enable;
                        user.Save();

                        result.Success = true;
                        result.Message = enable 
                            ? $"已啟用使用者: {username}" 
                            : $"已停用使用者: {username}";

                        System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] User '{username}' status changed to {(enable ? "Enabled" : "Disabled")}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Message = "權限不足，請以系統管理員身分執行";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Unauthorized: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Message = "更新帳號狀態失敗";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] UpdateUserStatus Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 重設使用者密碼
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="newPassword">新密碼</param>
        /// <returns>操作結果</returns>
        public OperationResult ResetPassword(string username, string newPassword)
        {
            var result = new OperationResult();

            try
            {
                // 檢查是否以系統管理員身分執行
                if (!IsRunningAsAdministrator())
                {
                    result.Message = "此操作需要系統管理員權限";
                    return result;
                }

                using (var context = new PrincipalContext(_contextType))
                {
                    using (var user = UserPrincipal.FindByIdentity(context, username))
                    {
                        if (user == null)
                        {
                            result.Message = $"找不到使用者: {username}";
                            return result;
                        }

                        user.SetPassword(newPassword);
                        user.Save();

                        result.Success = true;
                        result.Message = $"已重設使用者密碼: {username}";

                        System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Password reset for user '{username}'");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Message = "權限不足，請以系統管理員身分執行";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] Unauthorized: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Message = "重設密碼失敗";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] ResetPassword Error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Group Management

        /// <summary>
        /// 將使用者加入群組
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="groupName">群組名稱</param>
        /// <returns>操作結果</returns>
        public OperationResult AddUserToGroup(string username, string groupName)
        {
            var result = new OperationResult();

            try
            {
                if (!IsRunningAsAdministrator())
                {
                    result.Message = "此操作需要系統管理員權限";
                    return result;
                }

                using (var context = new PrincipalContext(_contextType))
                {
                    using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                    {
                        if (group == null)
                        {
                            result.Message = $"找不到群組: {groupName}";
                            return result;
                        }

                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user == null)
                            {
                                result.Message = $"找不到使用者: {username}";
                                return result;
                            }

                            // 檢查是否已在群組中
                            if (user.IsMemberOf(group))
                            {
                                result.Message = $"使用者 '{username}' 已在群組 '{groupName}' 中";
                                result.Success = true;
                                return result;
                            }

                            group.Members.Add(user);
                            group.Save();

                            result.Success = true;
                            result.Message = $"已將使用者 '{username}' 加入群組 '{groupName}'";

                            System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] User '{username}' added to group '{groupName}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = "加入群組失敗";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] AddUserToGroup Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 將使用者從群組移除
        /// </summary>
        /// <param name="username">使用者名稱</param>
        /// <param name="groupName">群組名稱</param>
        /// <returns>操作結果</returns>
        public OperationResult RemoveUserFromGroup(string username, string groupName)
        {
            var result = new OperationResult();

            try
            {
                if (!IsRunningAsAdministrator())
                {
                    result.Message = "此操作需要系統管理員權限";
                    return result;
                }

                using (var context = new PrincipalContext(_contextType))
                {
                    using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                    {
                        if (group == null)
                        {
                            result.Message = $"找不到群組: {groupName}";
                            return result;
                        }

                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user == null)
                            {
                                result.Message = $"找不到使用者: {username}";
                                return result;
                            }

                            group.Members.Remove(user);
                            group.Save();

                            result.Success = true;
                            result.Message = $"已將使用者 '{username}' 從群組 '{groupName}' 移除";

                            System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] User '{username}' removed from group '{groupName}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = "移除群組失敗";
                result.ErrorDetail = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[WindowsAccountService] RemoveUserFromGroup Error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 檢查是否以系統管理員身分執行
        /// </summary>
        /// <returns>true=管理員權限, false=一般權限</returns>
        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
