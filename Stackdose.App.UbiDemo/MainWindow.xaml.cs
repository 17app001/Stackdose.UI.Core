using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.App.UbiDemo.Models;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Controls;
using Stackdose.UI.Templates.Pages;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private UbiRuntimeContext? _runtime;
    private UbiShellCoordinator? _shell;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly SettingsPage _settingsPage = new();
    private readonly Dictionary<string, UbiDevicePage> _devicePages = new(StringComparer.OrdinalIgnoreCase);
    private string? _selectedMachineId;
    private bool _suppressHeaderMachineSelection;
    private readonly Dictionary<string, Action> _navigationHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _navigationTitles = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICommand _navigationCommand;
    private readonly ICommand _machineSelectionCommand;
    private readonly DispatcherTimer _metaReloadTimer;
    private FileSystemWatcher? _metaWatcher;
    private DateTime _lastMetaWriteUtc;
    private UbiAppMeta _currentMeta = new();

    public MainWindow()
    {
        _navigationCommand = new DelegateCommand(
            parameter => OnNavigationRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string target && !string.IsNullOrWhiteSpace(target));

        _machineSelectionCommand = new DelegateCommand(
            parameter => OnMachineSelectionRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string machineId && !string.IsNullOrWhiteSpace(machineId));

        _metaReloadTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _metaReloadTimer.Tick += OnMetaReloadTimerTick;

        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _runtime = UbiRuntimeHost.Start(MainShell);
        if (_runtime is null)
        {
            return;
        }

        ComplianceContext.LogSystem($"[UbiRuntime] Config directory: {_runtime.ConfigDirectory}", LogLevel.Info, showInUi: true);
        ComplianceContext.LogSystem($"[UbiRuntime] App meta file: {_runtime.MetaFilePath}", LogLevel.Info, showInUi: true);

        _currentMeta = _runtime.AppMeta;

        _shell = new UbiShellCoordinator(MainShell, MainShell.PageTitle);
        _runtime.OverviewPage.MachineSelected += OnMachineSelected;
        _runtime.OverviewPage.PlcScanUpdated  += OnPlcScanUpdated;

        MainShell.NavigationCommand = _navigationCommand;
        MainShell.MachineSelectionCommand = _machineSelectionCommand;

        _suppressHeaderMachineSelection = true;
        _shell.SetMachineOptions(_runtime.Machines.Values
            .Select(machine => new KeyValuePair<string, string>(
                machine.Machine.Id,
                $"{machine.Machine.Name} ({machine.Machine.Id})"))
            .ToList());
        _suppressHeaderMachineSelection = false;

        var externalNavigationItems = CreateNavigationItems(_currentMeta.NavigationItems);
        if (externalNavigationItems is not null)
        {
            MainShell.NavigationItems = externalNavigationItems;
        }
        else
        {
            MainShell.NavigationItems = null;
        }

        PopulateNavigationTitles(_currentMeta.NavigationItems);
        StartMetaWatcher();

        BuildNavigationHandlers();
        _shell.SelectNavigation("MachineOverviewPage");
        UbiRuntimeMapper.BuildRuntimeMaps(_runtime.Machines);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.OverviewPage.MachineSelected -= OnMachineSelected;
            _runtime.OverviewPage.PlcScanUpdated  -= OnPlcScanUpdated;
        }

        MainShell.NavigationCommand = null;
        MainShell.MachineSelectionCommand = null;
        StopMetaWatcher();
        _metaReloadTimer.Stop();
        _navigationHandlers.Clear();
        _shell = null;
    }

    private void OnMachineSelected(string machineId)
    {
        ShowMachineDetail(machineId);
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        if (_runtime?.OverviewPage.MachineCards is null)
        {
            return;
        }

        UbiRuntimeMapper.UpdateOverviewCards(manager, _runtime.OverviewPage.MachineCards);
    }

    private void OnNavigationRequested(object? sender, string target)
    {
        if (_runtime is null || string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (_navigationHandlers.TryGetValue(target, out var handler))
        {
            handler();
        }
    }

    private void BuildNavigationHandlers()
    {
        _navigationHandlers.Clear();
        _navigationHandlers["MACHINEOVERVIEWPAGE"] = ShowOverview;
        _navigationHandlers["MACHINEDETAILPAGE"] = ShowCurrentOrFirstMachineDetail;
        _navigationHandlers["LOGVIEWERPAGE"] = ShowLogViewer;
        _navigationHandlers["USERMANAGEMENTPAGE"] = ShowUserManagement;
        _navigationHandlers["SETTINGSPAGE"] = ShowSettings;
    }

    private void ShowOverview()
    {
        if (_runtime is null || _shell is null)
        {
            return;
        }

        var machineDisplayName = string.Empty;
        if (!string.IsNullOrWhiteSpace(_selectedMachineId)
            && _runtime.Machines.TryGetValue(_selectedMachineId, out var selectedMachine))
        {
            machineDisplayName = selectedMachine.Machine.Name;
        }

        _shell.ShowOverview(_runtime.OverviewPage, machineDisplayName);
        var fallbackTitle = string.IsNullOrWhiteSpace(_currentMeta.DefaultPageTitle)
            ? MainShell.PageTitle
            : _currentMeta.DefaultPageTitle;
        MainShell.PageTitle = GetNavigationTitle("MachineOverviewPage", fallbackTitle);
    }

    private void ShowCurrentOrFirstMachineDetail()
    {
        if (_runtime is null)
        {
            return;
        }

        var targetId = !string.IsNullOrWhiteSpace(_selectedMachineId)
                       && _runtime.Machines.ContainsKey(_selectedMachineId)
            ? _selectedMachineId
            : _runtime.Machines.Keys.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            ShowMachineDetail(targetId);
        }
    }

    private void ShowLogViewer()
    {
        _shell?.ShowLogViewer(_logViewerPage);
        MainShell.PageTitle = GetNavigationTitle("LogViewerPage", MainShell.PageTitle);
    }

    private void ShowUserManagement()
    {
        _shell?.ShowUserManagement(_userManagementPage);
        MainShell.PageTitle = GetNavigationTitle("UserManagementPage", MainShell.PageTitle);
    }

    private void ShowSettings()
    {
        if (_runtime is null || _shell is null)
        {
            return;
        }

        _settingsPage.SetMonitorAddresses(_runtime.OverviewPage.PlcMonitorAddresses);
        _shell.ShowSettings(_settingsPage);
        MainShell.PageTitle = GetNavigationTitle("SettingsPage", MainShell.PageTitle);
    }

    private void ShowMachineDetail(string machineId)
    {
        NavigateToDevicePage(machineId);
        _shell?.SelectNavigation("MachineDetailPage");
        MainShell.PageTitle = GetNavigationTitle("MachineDetailPage", MainShell.PageTitle);
    }

    private void NavigateToDevicePage(string machineId)
    {
        if (_runtime is null || !_runtime.Machines.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;
        if (!_devicePages.TryGetValue(machineId, out var devicePage))
        {
            devicePage = new UbiDevicePage();
            _devicePages[machineId] = devicePage;
        }

        devicePage.SetDeviceContext(UbiRuntimeMapper.CreateDeviceContext(config));

        _shell?.ShowMachineDetail(devicePage, machineId, config.Machine.Name);
    }

    private void OnMachineSelectionRequested(object? sender, string machineId)
    {
        if (_suppressHeaderMachineSelection || string.IsNullOrWhiteSpace(machineId))
        {
            return;
        }

        ShowMachineDetail(machineId);
    }

    private static ObservableCollection<NavigationItem>? CreateNavigationItems(IReadOnlyList<UbiNavigationMetaItem>? source)
    {
        if (source is null || source.Count == 0)
        {
            return null;
        }

        var items = new ObservableCollection<NavigationItem>();
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.NavigationTarget))
            {
                continue;
            }

            var level = Enum.TryParse<AccessLevel>(item.RequiredLevel, true, out var parsedLevel)
                ? parsedLevel
                : AccessLevel.Operator;

            items.Add(new NavigationItem
            {
                Title = item.Title,
                NavigationTarget = item.NavigationTarget,
                RequiredLevel = level
            });
        }

        return items.Count > 0 ? items : null;
    }

    private void PopulateNavigationTitles(IReadOnlyList<UbiNavigationMetaItem>? source)
    {
        _navigationTitles.Clear();
        _navigationTitles["MachineOverviewPage"] = "Machine Overview";
        _navigationTitles["MachineDetailPage"] = "Machine Detail";
        _navigationTitles["LogViewerPage"] = "Log Viewer";
        _navigationTitles["UserManagementPage"] = "User Management";
        _navigationTitles["SettingsPage"] = "Maintenance Mode";

        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.NavigationTarget) || string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            _navigationTitles[item.NavigationTarget] = item.Title;
        }
    }

    private string GetNavigationTitle(string target, string fallback)
    {
        if (_navigationTitles.TryGetValue(target, out var title) && !string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return fallback;
    }

    private void StartMetaWatcher()
    {
        if (_runtime is null || string.IsNullOrWhiteSpace(_runtime.MetaFilePath))
        {
            return;
        }

        var metaDirectory = Path.GetDirectoryName(_runtime.MetaFilePath);
        var metaFileName = Path.GetFileName(_runtime.MetaFilePath);
        if (string.IsNullOrWhiteSpace(metaDirectory) || string.IsNullOrWhiteSpace(metaFileName) || !Directory.Exists(metaDirectory))
        {
            return;
        }

        StopMetaWatcher();
        _lastMetaWriteUtc = File.Exists(_runtime.MetaFilePath)
            ? File.GetLastWriteTimeUtc(_runtime.MetaFilePath)
            : DateTime.MinValue;

        _metaWatcher = new FileSystemWatcher(metaDirectory, metaFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _metaWatcher.Changed += OnMetaFileChanged;
        _metaWatcher.Created += OnMetaFileChanged;
        _metaWatcher.Renamed += OnMetaFileRenamed;
    }

    private void StopMetaWatcher()
    {
        if (_metaWatcher is null)
        {
            return;
        }

        _metaWatcher.EnableRaisingEvents = false;
        _metaWatcher.Changed -= OnMetaFileChanged;
        _metaWatcher.Created -= OnMetaFileChanged;
        _metaWatcher.Renamed -= OnMetaFileRenamed;
        _metaWatcher.Dispose();
        _metaWatcher = null;
    }

    private void OnMetaFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _metaReloadTimer.Stop();
            _metaReloadTimer.Start();
        }));
    }

    private void OnMetaFileRenamed(object sender, RenamedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _metaReloadTimer.Stop();
            _metaReloadTimer.Start();
        }));
    }

    private void OnMetaReloadTimerTick(object? sender, EventArgs e)
    {
        _metaReloadTimer.Stop();
        ReloadMetaIfChanged();
    }

    private void ReloadMetaIfChanged()
    {
        if (_runtime is null || string.IsNullOrWhiteSpace(_runtime.MetaFilePath) || !File.Exists(_runtime.MetaFilePath))
        {
            return;
        }

        try
        {
            var writeUtc = File.GetLastWriteTimeUtc(_runtime.MetaFilePath);
            if (writeUtc <= _lastMetaWriteUtc)
            {
                return;
            }

            var meta = UbiRuntimeLoader.LoadMeta(_runtime.MetaFilePath);
            _lastMetaWriteUtc = writeUtc;
            ApplyMetaToRuntime(meta);
            ComplianceContext.LogSystem($"[UbiRuntime] Reloaded app meta: {_runtime.MetaFilePath}", LogLevel.Info, showInUi: true);
        }
        catch (Exception ex)
        {
            ComplianceContext.LogSystem($"[UbiRuntime] Failed to reload app meta: {ex.Message}", LogLevel.Error, showInUi: true);
        }
    }

    private void ApplyMetaToRuntime(UbiAppMeta meta)
    {
        if (_runtime is null)
        {
            return;
        }

        _currentMeta = meta;
        MainShell.HeaderDeviceName = meta.HeaderDeviceName;
        UbiRuntimeMapper.ApplyMeta(_runtime.OverviewPage, meta);

        var externalNavigationItems = CreateNavigationItems(meta.NavigationItems);
        MainShell.NavigationItems = externalNavigationItems;
        PopulateNavigationTitles(meta.NavigationItems);

        UpdateCurrentPageTitle();
    }

    private void UpdateCurrentPageTitle()
    {
        var target = MainShell.ShellContent switch
        {
            MachineOverviewPage => "MachineOverviewPage",
            UbiDevicePage => "MachineDetailPage",
            LogViewerPage => "LogViewerPage",
            UserManagementPage => "UserManagementPage",
            SettingsPage => "SettingsPage",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (string.Equals(target, "MachineOverviewPage", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = string.IsNullOrWhiteSpace(_currentMeta.DefaultPageTitle)
                ? MainShell.PageTitle
                : _currentMeta.DefaultPageTitle;
            MainShell.PageTitle = GetNavigationTitle(target, fallback);
            return;
        }

        MainShell.PageTitle = GetNavigationTitle(target, MainShell.PageTitle);
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
