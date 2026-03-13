using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Windows.Input;
using System.Windows.Threading;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiAppController : IDisposable
{
    private readonly MainContainer _mainShell;
    private readonly UbiDevicePageService _devicePages = new();
    private readonly IShellNavigationService _navigationService = new ShellNavigationService();
    private readonly UbiMainWindowBootstrapService _bootstrapService = new();
    private readonly UbiMetaRuntimeService _metaRuntimeService;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly SettingsPage _settingsPage = new();
    private readonly ICommand _navigationCommand;
    private readonly ICommand _machineSelectionCommand;

    private UbiAppSession? _session;
    private bool _suppressHeaderMachineSelection;

    public UbiAppController(MainContainer mainShell, Dispatcher dispatcher)
    {
        _mainShell = mainShell;
        _metaRuntimeService = new UbiMetaRuntimeService(dispatcher);
        _metaRuntimeService.SnapshotChanged += OnMetaSnapshotChanged;

        _navigationCommand = new DelegateCommand(
            parameter => OnNavigationRequested(parameter as string ?? string.Empty),
            parameter => parameter is string target && !string.IsNullOrWhiteSpace(target));

        _machineSelectionCommand = new DelegateCommand(
            parameter => OnMachineSelectionRequested(parameter as string ?? string.Empty),
            parameter => parameter is string machineId && !string.IsNullOrWhiteSpace(machineId));
    }

    public void Start()
    {
        if (_session is not null)
        {
            ComplianceContext.LogSystem("[UbiRuntime] Skip duplicated MainWindow load initialization", LogLevel.Warning, showInUi: true);
            return;
        }

        var bootstrapState = _bootstrapService.Start(
            _mainShell,
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

        var navigationOrchestrator = new UbiNavigationOrchestrator(
            _devicePages,
            bootstrapState.ShellPages,
            _logViewerPage,
            _userManagementPage,
            _settingsPage);

        _session = new UbiAppSession(bootstrapState, navigationOrchestrator);

        ComplianceContext.LogSystem(
            $"[UbiRuntime] Shell service mode: {_session.ServiceMode}",
            LogLevel.Info,
            showInUi: true);

        _session.Runtime.OverviewPage.MachineSelected += OnMachineSelected;
        _session.Runtime.OverviewPage.PlcScanUpdated += OnPlcScanUpdated;

        _suppressHeaderMachineSelection = true;
        _session.Shell.SetMachineOptions(_session.MachineOptions);
        _suppressHeaderMachineSelection = false;

        ApplyMetaSnapshot(_session.CurrentMetaSnapshot, updateCurrentPageTitle: false);
    }

    public void Stop()
    {
        if (_session is not null)
        {
            _session.Runtime.OverviewPage.MachineSelected -= OnMachineSelected;
            _session.Runtime.OverviewPage.PlcScanUpdated -= OnPlcScanUpdated;
        }

        _bootstrapService.Stop(_mainShell, _metaRuntimeService, _navigationService, _devicePages);
        _session = null;
    }

    public void Dispose()
    {
        Stop();
        _metaRuntimeService.SnapshotChanged -= OnMetaSnapshotChanged;
        _metaRuntimeService.Dispose();
    }

    private void OnMachineSelected(string machineId)
    {
        if (_session is null)
        {
            return;
        }

        _session.NavigationOrchestrator.ShowMachineDetail(_session.Runtime, machineId);
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        if (_session?.Runtime.OverviewPage.MachineCards is null)
        {
            return;
        }

        UbiRuntimeMapper.UpdateOverviewCards(manager, _session.Runtime.OverviewPage.MachineCards);
    }

    private void OnNavigationRequested(string target)
    {
        if (_session is null || string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        _navigationService.TryNavigate(target);
    }

    private void ShowOverview()
    {
        if (_session is null)
        {
            return;
        }

        _session.NavigationOrchestrator.ShowOverview(_session.Runtime, _session.CurrentMetaSnapshot);
    }

    private void ShowCurrentOrFirstMachineDetail()
    {
        if (_session is null)
        {
            return;
        }

        _session.NavigationOrchestrator.ShowCurrentOrFirstMachineDetail(_session.Runtime);
    }

    private void ShowLogViewer()
    {
        _session?.NavigationOrchestrator.ShowLogViewer();
    }

    private void ShowUserManagement()
    {
        _session?.NavigationOrchestrator.ShowUserManagement();
    }

    private void ShowSettings()
    {
        if (_session is null)
        {
            return;
        }

        _session.NavigationOrchestrator.ShowSettings(_session.Runtime);
    }

    private void OnMachineSelectionRequested(string machineId)
    {
        if (_suppressHeaderMachineSelection || string.IsNullOrWhiteSpace(machineId) || _session is null)
        {
            return;
        }

        _session.NavigationOrchestrator.ShowMachineDetail(_session.Runtime, machineId);
    }

    private void OnMetaSnapshotChanged(object? sender, ShellMetaSnapshotChangedEventArgs<UbiMetaSnapshot> e)
    {
        ApplyMetaSnapshot(e.Snapshot, updateCurrentPageTitle: true);
    }

    private void ApplyMetaSnapshot(UbiMetaSnapshot snapshot, bool updateCurrentPageTitle)
    {
        if (_session is null)
        {
            return;
        }

        _session.CurrentMetaSnapshot = snapshot;
        _mainShell.HeaderDeviceName = snapshot.Meta.HeaderDeviceName;
        UbiRuntimeMapper.ApplyMeta(_session.Runtime.OverviewPage, snapshot.Meta);
        _mainShell.NavigationItems = snapshot.NavigationItems;
        _navigationService.UpdateTitles(snapshot.NavigationTitles);

        if (updateCurrentPageTitle)
        {
            _session.NavigationOrchestrator.UpdateCurrentPageTitle(snapshot);
        }
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
