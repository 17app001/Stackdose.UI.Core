namespace Stackdose.UI.Core.Models
{
    /// <summary>
    /// 使用者工作階段 (User Session)
    /// </summary>
    public class UserSession
    {
        /// <summary>
        /// 當前登入的使用者
        /// </summary>
        public UserAccount? CurrentUser { get; set; }

        /// <summary>
        /// 登入時間
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 最後活動時間 (用於自動登出)
        /// </summary>
        public DateTime LastActivityTime { get; set; }

        /// <summary>
        /// 是否已登入
        /// </summary>
        public bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// 當前權限等級
        /// </summary>
        public AccessLevel CurrentLevel => CurrentUser?.AccessLevel ?? AccessLevel.Guest;

        /// <summary>
        /// 檢查是否有指定權限
        /// </summary>
        /// <param name="requiredLevel">所需權限等級</param>
        /// <returns>是否有權限</returns>
        public bool HasAccess(AccessLevel requiredLevel)
        {
            return CurrentLevel >= requiredLevel;
        }

        /// <summary>
        /// 取得當前使用者顯示名稱
        /// </summary>
        public string CurrentUserName => CurrentUser?.DisplayName ?? "Guest";

        /// <summary>
        /// 取得工作階段持續時間
        /// </summary>
        public TimeSpan SessionDuration => DateTime.Now - LoginTime;

        /// <summary>
        /// 取得閒置時間
        /// </summary>
        public TimeSpan IdleTime => DateTime.Now - LastActivityTime;
    }
}
