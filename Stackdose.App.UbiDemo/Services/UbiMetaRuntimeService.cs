using Stackdose.Abstractions.Logging;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;

namespace Stackdose.App.UbiDemo.Services;

public sealed class UbiMetaRuntimeService : IDisposable
{
    private static readonly HashSet<string> SupportedNavigationTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "MachineOverviewPage",
        "MachineDetailPage",
        "LogViewerPage",
        "UserManagementPage",
        "SettingsPage"
    };

    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _reloadTimer;
    private FileSystemWatcher? _watcher;
    private DateTime _lastMetaWriteUtc;
    private string _metaFilePath = string.Empty;
    private string _configDirectory = string.Empty;

    public UbiMetaRuntimeService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _reloadTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _reloadTimer.Tick += OnReloadTimerTick;
    }

    public event EventHandler<UbiMetaSnapshotChangedEventArgs>? SnapshotChanged;

    public UbiMetaSnapshot CurrentSnapshot { get; private set; } = UbiMetaSnapshot.Empty;

    public UbiMetaSnapshot Start(string configDirectory, string metaFilePath, UbiAppMeta initialMeta)
    {
        Stop();

        _configDirectory = configDirectory;
        _metaFilePath = metaFilePath;
        _lastMetaWriteUtc = File.Exists(metaFilePath)
            ? File.GetLastWriteTimeUtc(metaFilePath)
            : DateTime.MinValue;

        CurrentSnapshot = BuildSnapshot(initialMeta);

        ComplianceContext.LogSystem($"[UbiRuntime] Config directory: {_configDirectory}", LogLevel.Info, showInUi: true);
        ComplianceContext.LogSystem($"[UbiRuntime] App meta file: {_metaFilePath}", LogLevel.Info, showInUi: true);

        StartWatcher();
        return CurrentSnapshot;
    }

    public void Stop()
    {
        _reloadTimer.Stop();

        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnMetaFileChanged;
        _watcher.Created -= OnMetaFileChanged;
        _watcher.Renamed -= OnMetaFileRenamed;
        _watcher.Dispose();
        _watcher = null;
    }

    public void Dispose()
    {
        Stop();
        _reloadTimer.Tick -= OnReloadTimerTick;
    }

    private void StartWatcher()
    {
        var metaDirectory = Path.GetDirectoryName(_metaFilePath);
        var metaFileName = Path.GetFileName(_metaFilePath);
        if (string.IsNullOrWhiteSpace(metaDirectory)
            || string.IsNullOrWhiteSpace(metaFileName)
            || !Directory.Exists(metaDirectory))
        {
            return;
        }

        _watcher = new FileSystemWatcher(metaDirectory, metaFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnMetaFileChanged;
        _watcher.Created += OnMetaFileChanged;
        _watcher.Renamed += OnMetaFileRenamed;
    }

    private void OnMetaFileChanged(object sender, FileSystemEventArgs e)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            _reloadTimer.Stop();
            _reloadTimer.Start();
        }));
    }

    private void OnMetaFileRenamed(object sender, RenamedEventArgs e)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            _reloadTimer.Stop();
            _reloadTimer.Start();
        }));
    }

    private void OnReloadTimerTick(object? sender, EventArgs e)
    {
        _reloadTimer.Stop();
        ReloadIfChanged();
    }

    private void ReloadIfChanged()
    {
        if (string.IsNullOrWhiteSpace(_metaFilePath) || !File.Exists(_metaFilePath))
        {
            return;
        }

        try
        {
            var writeUtc = File.GetLastWriteTimeUtc(_metaFilePath);
            if (writeUtc <= _lastMetaWriteUtc)
            {
                return;
            }

            var meta = UbiRuntimeLoader.LoadMeta(_metaFilePath);
            _lastMetaWriteUtc = writeUtc;
            CurrentSnapshot = BuildSnapshot(meta);

            SnapshotChanged?.Invoke(this, new UbiMetaSnapshotChangedEventArgs(CurrentSnapshot));
            ComplianceContext.LogSystem($"[UbiRuntime] Reloaded app meta: {_metaFilePath}", LogLevel.Info, showInUi: true);
        }
        catch (Exception ex)
        {
            ComplianceContext.LogSystem($"[UbiRuntime] Failed to reload app meta: {ex.Message}", LogLevel.Error, showInUi: true);
        }
    }

    private UbiMetaSnapshot BuildSnapshot(UbiAppMeta meta)
    {
        var navigationItems = BuildNavigationItems(meta.NavigationItems);
        var titles = BuildNavigationTitles(meta.NavigationItems);
        return new UbiMetaSnapshot(meta, navigationItems, titles);
    }

    private static Dictionary<string, string> BuildNavigationTitles(IReadOnlyList<UbiNavigationMetaItem>? source)
    {
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MachineOverviewPage"] = "Machine Overview",
            ["MachineDetailPage"] = "Machine Detail",
            ["LogViewerPage"] = "Log Viewer",
            ["UserManagementPage"] = "User Management",
            ["SettingsPage"] = "Maintenance Mode"
        };

        if (source is null)
        {
            return titles;
        }

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title)
                || string.IsNullOrWhiteSpace(item.NavigationTarget)
                || !SupportedNavigationTargets.Contains(item.NavigationTarget))
            {
                continue;
            }

            titles[item.NavigationTarget] = item.Title;
        }

        return titles;
    }

    private static ObservableCollection<NavigationItem>? BuildNavigationItems(IReadOnlyList<UbiNavigationMetaItem>? source)
    {
        if (source is null || source.Count == 0)
        {
            return null;
        }

        var items = new ObservableCollection<NavigationItem>();
        for (var index = 0; index < source.Count; index++)
        {
            var item = source[index];
            if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.NavigationTarget))
            {
                ComplianceContext.LogSystem($"[UbiRuntime] Skip navigationItems[{index}]: title/target is empty", LogLevel.Warning, showInUi: true);
                continue;
            }

            if (!SupportedNavigationTargets.Contains(item.NavigationTarget))
            {
                ComplianceContext.LogSystem($"[UbiRuntime] Skip navigationItems[{index}]: unsupported target '{item.NavigationTarget}'", LogLevel.Warning, showInUi: true);
                continue;
            }

            var level = AccessLevel.Operator;
            if (!string.IsNullOrWhiteSpace(item.RequiredLevel)
                && Enum.TryParse<AccessLevel>(item.RequiredLevel, true, out var parsedLevel))
            {
                level = parsedLevel;
            }
            else if (!string.IsNullOrWhiteSpace(item.RequiredLevel))
            {
                ComplianceContext.LogSystem($"[UbiRuntime] navigationItems[{index}] invalid requiredLevel '{item.RequiredLevel}', fallback to Operator", LogLevel.Warning, showInUi: true);
            }

            items.Add(new NavigationItem
            {
                Title = item.Title,
                NavigationTarget = item.NavigationTarget,
                RequiredLevel = level
            });
        }

        return items.Count > 0 ? items : null;
    }
}

public sealed class UbiMetaSnapshotChangedEventArgs : EventArgs
{
    public UbiMetaSnapshotChangedEventArgs(UbiMetaSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public UbiMetaSnapshot Snapshot { get; }
}

public sealed class UbiMetaSnapshot
{
    public static readonly UbiMetaSnapshot Empty = new(new UbiAppMeta(), null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public UbiMetaSnapshot(UbiAppMeta meta, ObservableCollection<NavigationItem>? navigationItems, Dictionary<string, string> navigationTitles)
    {
        Meta = meta;
        NavigationItems = navigationItems;
        NavigationTitles = navigationTitles;
    }

    public UbiAppMeta Meta { get; }
    public ObservableCollection<NavigationItem>? NavigationItems { get; }
    public Dictionary<string, string> NavigationTitles { get; }
}
