using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Core.Helpers;
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
    private readonly UbiDevicePageService _devicePages;
    private bool _suppressHeaderMachineSelection;
    private readonly UbiNavigationService _navigationService;
    private readonly UbiMainWindowBootstrapService _bootstrapService;
    private readonly ICommand _navigationCommand;
    private readonly ICommand _machineSelectionCommand;
    private readonly UbiMetaRuntimeService _metaRuntimeService;
    private UbiMetaSnapshot _currentMetaSnapshot = UbiMetaSnapshot.Empty;
    private UbiShellPageService? _shellPages;
    private bool _frameworkServicesEnabled;

    public MainWindow()
    {
        _navigationCommand = new DelegateCommand(
            parameter => OnNavigationRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string target && !string.IsNullOrWhiteSpace(target));

        _machineSelectionCommand = new DelegateCommand(
            parameter => OnMachineSelectionRequested(this, parameter as string ?? string.Empty),
            parameter => parameter is string machineId && !string.IsNullOrWhiteSpace(machineId));

        _devicePages = new UbiDevicePageService();
        _navigationService = new UbiNavigationService();
        _bootstrapService = new UbiMainWindowBootstrapService();
        InitializeComponent();
        _metaRuntimeService = new UbiMetaRuntimeService(Dispatcher);
        _metaRuntimeService.SnapshotChanged += OnMetaSnapshotChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var bootstrapState = _bootstrapService.Start(
            MainShell,
            _metaRuntimeService,
            _navigationService,
            _navigationCommand,
            _machineSelectionCommand,
            ShowOverview,
            ShowCurrentOrFirstMachineDetail,
            ShowLogViewer,
            ShowUserManagement,
            ShowSettings);
        if (bootstrapState is null)
        {
            return;
        }

        _runtime = bootstrapState.Runtime;
        _shell = bootstrapState.Shell;
        _shellPages = bootstrapState.ShellPages;
        _currentMetaSnapshot = bootstrapState.InitialMetaSnapshot;
        _frameworkServicesEnabled = bootstrapState.FrameworkServicesEnabled;

        ComplianceContext.LogSystem(
            $"[UbiRuntime] Framework shell services mode: {(_frameworkServicesEnabled ? "enabled" : "disabled")}",
            LogLevel.Info,
            showInUi: true);

        _runtime.OverviewPage.MachineSelected += OnMachineSelected;
        _runtime.OverviewPage.PlcScanUpdated  += OnPlcScanUpdated;

        _suppressHeaderMachineSelection = true;
        _shell.SetMachineOptions(bootstrapState.MachineOptions);
        _suppressHeaderMachineSelection = false;

        ApplyMetaSnapshot(_currentMetaSnapshot, updateCurrentPageTitle: false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.OverviewPage.MachineSelected -= OnMachineSelected;
            _runtime.OverviewPage.PlcScanUpdated  -= OnPlcScanUpdated;
        }

        _bootstrapService.Stop(MainShell, _metaRuntimeService, _navigationService, _devicePages);
        _shellPages = null;
        _shell = null;
        _runtime = null;
        _frameworkServicesEnabled = false;
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

        _navigationService.TryNavigate(target);
    }

    private void ShowOverview()
    {
        if (_runtime is null || _shell is null)
        {
            return;
        }

        _shellPages?.ShowOverview(
            _runtime.OverviewPage,
            _runtime.Machines,
            _devicePages.SelectedMachineId,
            _currentMetaSnapshot.Meta.DefaultPageTitle);
    }

    private void ShowCurrentOrFirstMachineDetail()
    {
        if (_runtime is null)
        {
            return;
        }

        var targetId = _devicePages.GetCurrentOrFirstMachineId(_runtime.Machines);

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            ShowMachineDetail(targetId);
        }
    }

    private void ShowLogViewer()
    {
        _shellPages?.ShowLogViewer(_logViewerPage);
    }

    private void ShowUserManagement()
    {
        _shellPages?.ShowUserManagement(_userManagementPage);
    }

    private void ShowSettings()
    {
        if (_runtime is null || _shellPages is null)
        {
            return;
        }

        _settingsPage.SetMonitorAddresses(_runtime.OverviewPage.PlcMonitorAddresses);
        _shellPages.ShowSettings(_settingsPage);
    }

    private void ShowMachineDetail(string machineId)
    {
        NavigateToDevicePage(machineId);
    }

    private void NavigateToDevicePage(string machineId)
    {
        if (_runtime is null)
        {
            return;
        }

        if (_devicePages.TryGetDetailPage(machineId, _runtime.Machines, out var devicePage, out var machineName)
            && devicePage is not null)
        {
            _shellPages?.ShowMachineDetail(devicePage, machineId, machineName);
        }
    }

    private void OnMachineSelectionRequested(object? sender, string machineId)
    {
        if (_suppressHeaderMachineSelection || string.IsNullOrWhiteSpace(machineId))
        {
            return;
        }

        ShowMachineDetail(machineId);
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
        _navigationService.UpdateTitles(snapshot.NavigationTitles);

        if (updateCurrentPageTitle)
        {
            UpdateCurrentPageTitle();
        }
    }

    private void UpdateCurrentPageTitle()
    {
        _shellPages?.UpdateCurrentPageTitle(_currentMetaSnapshot.Meta.DefaultPageTitle);
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
