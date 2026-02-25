using System.Collections.Generic;
using Stackdose.UI.Core.Shell;

namespace Stackdose.App.UbiDemo.Services;

public sealed class UbiNavigationService : IShellNavigationService
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
        _handlers["MachineOverviewPage"] = showOverview;
        _handlers["MachineDetailPage"] = showMachineDetail;
        _handlers["LogViewerPage"] = showLogViewer;
        _handlers["UserManagementPage"] = showUserManagement;
        _handlers["SettingsPage"] = showSettings;
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
        _titles["MachineOverviewPage"] = "Machine Overview";
        _titles["MachineDetailPage"] = "Machine Detail";
        _titles["LogViewerPage"] = "Log Viewer";
        _titles["UserManagementPage"] = "User Management";
        _titles["SettingsPage"] = "Maintenance Mode";

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
