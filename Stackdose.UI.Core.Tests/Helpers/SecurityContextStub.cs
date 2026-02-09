namespace Stackdose.UI.Core.Helpers
{
    public static class SecurityContext
    {
        public static SessionInfo? CurrentSession { get; set; }
    }

    public class SessionInfo
    {
        public UserInfo? CurrentUser { get; set; }
        public string CurrentUserName => CurrentUser?.DisplayName ?? "";
    }

    public class UserInfo
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
