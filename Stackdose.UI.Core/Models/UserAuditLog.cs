using System;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 使用者管理稽核記錄 (符合 FDA 21 CFR Part 11)
    /// </summary>
    public class UserAuditLog
    {
        /// <summary>
        /// 唯一識別碼
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 時間戳記
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作者 ID
        /// </summary>
        public int OperatorUserId { get; set; }

        /// <summary>
        /// 操作者名稱
        /// </summary>
        public string OperatorUserName { get; set; } = string.Empty;

        /// <summary>
        /// 操作類型
        /// </summary>
        public UserAuditAction Action { get; set; }

        /// <summary>
        /// 目標使用者 ID
        /// </summary>
        public int? TargetUserId { get; set; }

        /// <summary>
        /// 目標使用者名稱
        /// </summary>
        public string? TargetUserName { get; set; }

        /// <summary>
        /// 變更明細 (JSON 格式)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// IP 位址
        /// </summary>
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// 使用者稽核操作類型
    /// </summary>
    public enum UserAuditAction
    {
        /// <summary>
        /// 創建使用者
        /// </summary>
        CreateUser = 0,

        /// <summary>
        /// 修改使用者
        /// </summary>
        ModifyUser = 1,

        /// <summary>
        /// 刪除使用者 (軟刪除)
        /// </summary>
        DeleteUser = 2,

        /// <summary>
        /// 啟用使用者
        /// </summary>
        ActivateUser = 3,

        /// <summary>
        /// 重設密碼
        /// </summary>
        ResetPassword = 4,

        /// <summary>
        /// 變更權限等級
        /// </summary>
        ChangeAccessLevel = 5
    }
}
