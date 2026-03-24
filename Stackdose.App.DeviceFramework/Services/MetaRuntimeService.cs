using Stackdose.Abstractions.Logging;
using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Meta Runtime Service — 監控 app-meta.json 變更並即時更新。
/// </summary>
public sealed class MetaRuntimeService : IShellMetaRuntimeService<AppMeta, MetaSnapshot>, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _reloadTimer;
    private FileSystemWatcher? _watcher;
    private DateTime _lastMetaWriteUtc;
    private string _metaFilePath = string.Empty;
    private string _configDirectory = string.Empty;
    private bool _enableMetaHotReload;

    public MetaRuntimeService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _reloadTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _reloadTimer.Tick += OnReloadTimerTick;
    }

    public event EventHandler<ShellMetaSnapshotChangedEventArgs<MetaSnapshot>>? SnapshotChanged;

    public MetaSnapshot CurrentSnapshot { get; private set; } = MetaSnapshot.Empty;

    public MetaSnapshot Start(string configDirectory, string metaFilePath, AppMeta initialMeta, bool enableMetaHotReload)
    {
        Stop();

        _configDirectory = configDirectory;
        _metaFilePath = metaFilePath;
        _enableMetaHotReload = enableMetaHotReload;
        _lastMetaWriteUtc = File.Exists(metaFilePath)
            ? File.GetLastWriteTimeUtc(metaFilePath)
            : DateTime.MinValue;

        CurrentSnapshot = BuildSnapshot(initialMeta);

        ComplianceContext.LogSystem($"[Runtime] Config directory: {_configDirectory}", LogLevel.Info, showInUi: true);
        ComplianceContext.LogSystem($"[Runtime] App meta file: {_metaFilePath}", LogLevel.Info, showInUi: true);

        if (_enableMetaHotReload)
        {
            StartWatcher();
            ComplianceContext.LogSystem("[Runtime] App meta hot reload enabled", LogLevel.Info, showInUi: true);
        }

        return CurrentSnapshot;
    }

    public void Stop()
    {
        _reloadTimer.Stop();

        if (_watcher is null)
            return;

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
            return;

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
        _dispatcher.BeginInvoke(new Action(() => { _reloadTimer.Stop(); _reloadTimer.Start(); }));
    }

    private void OnMetaFileRenamed(object sender, RenamedEventArgs e)
    {
        _dispatcher.BeginInvoke(new Action(() => { _reloadTimer.Stop(); _reloadTimer.Start(); }));
    }

    private void OnReloadTimerTick(object? sender, EventArgs e)
    {
        _reloadTimer.Stop();
        ReloadIfChanged();
    }

    private void ReloadIfChanged()
    {
        if (string.IsNullOrWhiteSpace(_metaFilePath) || !File.Exists(_metaFilePath))
            return;

        try
        {
            var writeUtc = File.GetLastWriteTimeUtc(_metaFilePath);
            if (writeUtc <= _lastMetaWriteUtc)
                return;

            var meta = ConfigLoader.LoadMeta(_metaFilePath);
            _lastMetaWriteUtc = writeUtc;
            CurrentSnapshot = BuildSnapshot(meta);

            SnapshotChanged?.Invoke(this, new ShellMetaSnapshotChangedEventArgs<MetaSnapshot>(CurrentSnapshot));
            ComplianceContext.LogSystem($"[Runtime] Reloaded app meta: {_metaFilePath}", LogLevel.Info, showInUi: true);
        }
        catch (Exception ex)
        {
            ComplianceContext.LogSystem($"[Runtime] Failed to reload app meta: {ex.Message}", LogLevel.Error, showInUi: true);
        }
    }

    private static MetaSnapshot BuildSnapshot(AppMeta meta)
    {
        var navigationItems = BuildNavigationItems(meta.NavigationItems);
        var titles = BuildNavigationTitles(meta.NavigationItems);
        return new MetaSnapshot(meta, navigationItems, titles);
    }

    private static Dictionary<string, string> BuildNavigationTitles(IReadOnlyList<NavigationMetaItem>? source)
    {
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MachineOverviewPage"] = "Machine Overview",
            ["MachineDetailPage"] = "Machine Detail",
            ["LogViewerPage"] = "Log Viewer",
            ["UserManagementPage"] = "User Management",
            ["SettingsPage"] = "Maintenance Mode"
        };

        if (source is null) return titles;

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title) || !ShellRouteCatalog.IsSupportedTarget(item.NavigationTarget))
                continue;
            titles[item.NavigationTarget] = item.Title;
        }

        return titles;
    }

    private static ObservableCollection<NavigationItem>? BuildNavigationItems(IReadOnlyList<NavigationMetaItem>? source)
    {
        if (source is null || source.Count == 0)
            return null;

        var items = new ObservableCollection<NavigationItem>();
        for (var index = 0; index < source.Count; index++)
        {
            var item = source[index];
            if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.NavigationTarget))
                continue;
            if (!ShellRouteCatalog.IsSupportedTarget(item.NavigationTarget))
                continue;

            var level = AccessLevel.Operator;
            if (!string.IsNullOrWhiteSpace(item.RequiredLevel)
                && Enum.TryParse<AccessLevel>(item.RequiredLevel, true, out var parsedLevel))
            {
                level = parsedLevel;
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

/// <summary>
/// Meta 快照 — 包含 AppMeta + 導航項目 + 導航標題。
/// </summary>
public sealed class MetaSnapshot : IShellMetaSnapshot
{
    public static readonly MetaSnapshot Empty = new(new AppMeta(), null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public MetaSnapshot(AppMeta meta, ObservableCollection<NavigationItem>? navigationItems, Dictionary<string, string> navigationTitles)
    {
        Meta = meta;
        NavigationItems = navigationItems;
        NavigationTitles = navigationTitles;
    }

    public AppMeta Meta { get; }
    public string HeaderDeviceName => Meta.HeaderDeviceName;
    public string DefaultPageTitle => Meta.DefaultPageTitle;
    public ObservableCollection<NavigationItem>? NavigationItems { get; }
    public Dictionary<string, string> NavigationTitles { get; }
    IReadOnlyDictionary<string, string> IShellMetaSnapshot.NavigationTitles => NavigationTitles;
}
