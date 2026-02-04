using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Services
{
    /// <summary>
    /// User Management Service Interface (FDA 21 CFR Part 11 Compliant)
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Database password authentication (for locally created users)
        /// </summary>
        Task<(bool Success, string Message, UserAccount? User)> AuthenticateAsync(string userId, string password);

        /// <summary>
        /// Create new user
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
        /// Soft delete user
        /// </summary>
        Task<(bool Success, string Message)> SoftDeleteUserAsync(int targetUserId, int operatorUserId);

        /// <summary>
        /// Activate user
        /// </summary>
        Task<(bool Success, string Message)> ActivateUserAsync(int targetUserId, int operatorUserId);

        /// <summary>
        /// Update user information
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
        /// Reset password
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(
            int targetUserId,
            int operatorUserId,
            string newPassword);

        /// <summary>
        /// Get manageable users list
        /// </summary>
        Task<List<UserAccount>> GetManagedUsersAsync(int operatorUserId);

        /// <summary>
        /// Get all users list (Admin only)
        /// </summary>
        Task<List<UserAccount>> GetAllUsersAsync();

        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<UserAccount?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Check if can delete target user
        /// </summary>
        bool CanDeleteUser(int operatorUserId, int targetUserId, AccessLevel operatorLevel);

        /// <summary>
        /// Check if can manage target user
        /// </summary>
        bool CanManageUser(AccessLevel operatorLevel, AccessLevel targetLevel);

        /// <summary>
        /// Get audit logs
        /// </summary>
        Task<List<UserAuditLog>> GetAuditLogsAsync(int? targetUserId = null, int pageSize = 100);

        /// <summary>
        /// Generate next available User ID (UID-XXXXXX format)
        /// </summary>
        string GenerateNextUserId();
    }
}
