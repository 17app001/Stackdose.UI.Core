namespace Stackdose.UI.Core.Shell;

public interface IShellNavigationService
{
    void RegisterDefaultHandlers(
        Action showOverview,
        Action showMachineDetail,
        Action showLogViewer,
        Action showUserManagement,
        Action showSettings);

    bool TryNavigate(string? target);

    void UpdateTitles(IReadOnlyDictionary<string, string> incomingTitles);

    string GetTitle(string target, string fallback);

    void Clear();
}
