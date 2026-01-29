using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// 使用者管理服務介面 (符合 FDA 21 CFR Part 11)
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// ?? 新增：資料庫密碼驗證（用於本地創建的使用者）
        /// </summary>
        Task<(bool Success, string Message, UserAccount? User)> AuthenticateAsync(string userId, string password);

        /// <summary>
        /// 創建新使用者
        /// </summary>
        Task<(bool Success, string Message, UserAccount? User)> CreateUserAsync(
            string userId,
            string displayName,
            string password,
            AccessLevel accessLevel,
            int creatorUserId,
            string? email = null,
            string? department = null,
            string? remarks = null);

        /// <summary>
        /// 軟刪除使用者
        /// </summary>
        Task<(bool Success, string Message)> SoftDeleteUserAsync(int targetUserId, int operatorUserId);

        /// <summary>
        /// 啟用使用者
        /// </summary>
        Task<(bool Success, string Message)> ActivateUserAsync(int targetUserId, int operatorUserId);

        /// <summary>
        /// 更新使用者資料
        /// </summary>
        Task<(bool Success, string Message)> UpdateUserAsync(
            int targetUserId,
            int operatorUserId,
            string? displayName = null,
            string? email = null,
            string? department = null,
            string? remarks = null,
            AccessLevel? newAccessLevel = null);

        /// <summary>
        /// 重設密碼
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(
            int targetUserId,
            int operatorUserId,
            string newPassword);

        /// <summary>
        /// 取得可管理的使用者列表
        /// </summary>
        Task<List<UserAccount>> GetManagedUsersAsync(int operatorUserId);

        /// <summary>
        /// 取得所有使用者列表 (Admin only)
        /// </summary>
        Task<List<UserAccount>> GetAllUsersAsync();

        /// <summary>
        /// 根據 ID 取得使用者
        /// </summary>
        Task<UserAccount?> GetUserByIdAsync(int userId);

        /// <summary>
        /// 檢查是否可刪除目標使用者
        /// </summary>
        bool CanDeleteUser(int operatorUserId, int targetUserId, AccessLevel operatorLevel);

        /// <summary>
        /// 檢查是否可管理目標使用者
        /// </summary>
        bool CanManageUser(AccessLevel operatorLevel, AccessLevel targetLevel);

        /// <summary>
        /// 取得稽核記錄
        /// </summary>
        Task<List<UserAuditLog>> GetAuditLogsAsync(int? targetUserId = null, int pageSize = 100);
    }
}
