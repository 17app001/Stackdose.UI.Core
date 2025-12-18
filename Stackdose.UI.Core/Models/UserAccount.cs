namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 使用者帳號 (User Account)
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// 唯一識別碼 (Unique ID)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱 (Display Name)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 密碼雜湊 (SHA-256)
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 權限等級
        /// </summary>
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Operator;

        /// <summary>
        /// 帳號是否啟用
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最後登入時間
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// 建立者 (Audit Trail)
        /// </summary>
        public string CreatedBy { get; set; } = "System";

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remarks { get; set; }
    }
}
