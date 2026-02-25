namespace Stackdose.UI.Core.Shell;

public static class ShellRouteCatalog
{
    private static readonly HashSet<string> SupportedTargetsSet =
    [
        ShellNavigationTargets.Overview,
        ShellNavigationTargets.Detail,
        ShellNavigationTargets.LogViewer,
        ShellNavigationTargets.UserManagement,
        ShellNavigationTargets.Settings
    ];

    public static IReadOnlyDictionary<string, string> DefaultTitles { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [ShellNavigationTargets.Overview] = "Machine Overview",
        [ShellNavigationTargets.Detail] = "Machine Detail",
        [ShellNavigationTargets.LogViewer] = "Log Viewer",
        [ShellNavigationTargets.UserManagement] = "User Management",
        [ShellNavigationTargets.Settings] = "Maintenance Mode"
    };

    public static bool IsSupportedTarget(string? target)
        => !string.IsNullOrWhiteSpace(target) && SupportedTargetsSet.Contains(target);
}
