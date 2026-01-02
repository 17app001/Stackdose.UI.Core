using System;

namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 使用者帳號 (User Account)
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// 資料庫主鍵 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 唯一識別碼 (Unique ID / Login ID)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱 (Display Name)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 密碼雜湊值 (SHA-256)
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 密碼鹽值 (Salt)
        /// </summary>
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// 存取權限等級
        /// </summary>
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Operator;

        /// <summary>
        /// 帳號是否啟用 (軟刪除標記)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 創建時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 創建者 ID (Audit Trail)
        /// </summary>
        public int? CreatedByUserId { get; set; }

        /// <summary>
        /// 創建者名稱 (Audit Trail)
        /// </summary>
        public string CreatedBy { get; set; } = "System";

        /// <summary>
        /// 最後登入時間
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// 最後修改時間
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// 最後修改者 ID
        /// </summary>
        public int? LastModifiedByUserId { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 電子郵件
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 部門
        /// </summary>
        public string? Department { get; set; }
    }
}
