namespace Stackdose.UI.Core.Shell;

public sealed class ShellNavigationService : IShellNavigationService
{
    private readonly Dictionary<string, Action> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _titles = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterDefaultHandlers(
        Action showOverview,
        Action showMachineDetail,
        Action showLogViewer,
        Action showUserManagement,
        Action showSettings)
    {
        _handlers.Clear();
        _handlers[ShellNavigationTargets.Overview] = showOverview;
        _handlers[ShellNavigationTargets.Detail] = showMachineDetail;
        _handlers[ShellNavigationTargets.LogViewer] = showLogViewer;
        _handlers[ShellNavigationTargets.UserManagement] = showUserManagement;
        _handlers[ShellNavigationTargets.Settings] = showSettings;
    }

    public bool TryNavigate(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        if (!_handlers.TryGetValue(target, out var handler))
        {
            return false;
        }

        handler();
        return true;
    }

    public void UpdateTitles(IReadOnlyDictionary<string, string> incomingTitles)
    {
        _titles.Clear();
        foreach (var (key, value) in ShellRouteCatalog.DefaultTitles)
        {
            _titles[key] = value;
        }

        foreach (var (key, value) in incomingTitles)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            _titles[key] = value;
        }
    }

    public string GetTitle(string target, string fallback)
    {
        if (_titles.TryGetValue(target, out var title) && !string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return fallback;
    }

    public void Clear()
    {
        _handlers.Clear();
        _titles.Clear();
    }
}
