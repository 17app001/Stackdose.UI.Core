using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Templates.Pages;
using System.Windows;
using System.Windows.Input;

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
    private readonly UbiMetaRuntimeService _metaRuntimeService;
    private UbiMetaSnapshot _currentMetaSnapshot = UbiMetaSnapshot.Empty;

    public MainWindow()
    {
        _navigationCommand = new DelegateCommand(
            parameter => OnNavigationRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string target && !string.IsNullOrWhiteSpace(target));

        _machineSelectionCommand = new DelegateCommand(
            parameter => OnMachineSelectionRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string machineId && !string.IsNullOrWhiteSpace(machineId));

        InitializeComponent();
        _metaRuntimeService = new UbiMetaRuntimeService(Dispatcher);
        _metaRuntimeService.SnapshotChanged += OnMetaSnapshotChanged;
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

        _currentMetaSnapshot = _metaRuntimeService.Start(_runtime.ConfigDirectory, _runtime.MetaFilePath, _runtime.AppMeta);

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

        ApplyMetaSnapshot(_currentMetaSnapshot, updateCurrentPageTitle: false);

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
        _metaRuntimeService.Stop();
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
        var fallbackTitle = string.IsNullOrWhiteSpace(_currentMetaSnapshot.Meta.DefaultPageTitle)
            ? MainShell.PageTitle
            : _currentMetaSnapshot.Meta.DefaultPageTitle;
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

    private string GetNavigationTitle(string target, string fallback)
    {
        if (_navigationTitles.TryGetValue(target, out var title) && !string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return fallback;
    }

    private void OnMetaSnapshotChanged(object? sender, UbiMetaSnapshotChangedEventArgs e)
    {
        ApplyMetaSnapshot(e.Snapshot, updateCurrentPageTitle: true);
    }

    private void ApplyMetaSnapshot(UbiMetaSnapshot snapshot, bool updateCurrentPageTitle)
    {
        if (_runtime is null)
        {
            return;
        }

        _currentMetaSnapshot = snapshot;
        MainShell.HeaderDeviceName = snapshot.Meta.HeaderDeviceName;
        UbiRuntimeMapper.ApplyMeta(_runtime.OverviewPage, snapshot.Meta);
        MainShell.NavigationItems = snapshot.NavigationItems;

        _navigationTitles.Clear();
        foreach (var (key, value) in snapshot.NavigationTitles)
        {
            _navigationTitles[key] = value;
        }

        if (updateCurrentPageTitle)
        {
            UpdateCurrentPageTitle();
        }
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
            var fallback = string.IsNullOrWhiteSpace(_currentMetaSnapshot.Meta.DefaultPageTitle)
                ? MainShell.PageTitle
                : _currentMetaSnapshot.Meta.DefaultPageTitle;
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
