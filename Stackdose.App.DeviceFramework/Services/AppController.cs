using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 App 控制器 — 管理整個 App 生命週期。
/// App 層只需在 MainWindow 建立此控制器並呼叫 Start/Dispose。
/// </summary>
public class AppController : IDisposable
{
    private readonly MainContainer _mainShell;
    private readonly DevicePageService _devicePages;
    private readonly IShellNavigationService _navigationService = new ShellNavigationService();
    private readonly BootstrapService _bootstrapService;
    private readonly MetaRuntimeService _metaRuntimeService;
    private readonly RuntimeHost _runtimeHost;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly ICommand _navigationCommand;
    private readonly ICommand _machineSelectionCommand;

    private AppSession? _session;
    private bool _suppressHeaderMachineSelection;

    /// <summary>
    /// Settings 頁面 — App 端可設定自訂的 Settings 頁面。
    /// </summary>
    public UserControl SettingsPage { get; set; }

    /// <summary>
    /// Settings 頁面導航前的回呼 — App 端可在此注入 Runtime 資訊到 SettingsPage。
    /// 參數為 (SettingsPage, RuntimeContext, SelectedMachineId)。
    /// </summary>
    public Action<UserControl, RuntimeContext, string?>? OnSettingsNavigating { get; set; }

    public AppController(
        MainContainer mainShell,
        Dispatcher dispatcher,
        RuntimeHost? runtimeHost = null)
    {
        _mainShell = mainShell;
        _runtimeHost = runtimeHost ?? new RuntimeHost();
        _metaRuntimeService = new MetaRuntimeService(dispatcher);
        _metaRuntimeService.SnapshotChanged += OnMetaSnapshotChanged;
        _bootstrapService = new BootstrapService(_runtimeHost);
        _devicePages = new DevicePageService(_runtimeHost.Mapper);
        SettingsPage = new UserControl(); // 預設空頁面，App 層應覆寫

        _navigationCommand = new DelegateCommand(
            parameter => OnNavigationRequested(parameter as string ?? string.Empty),
            parameter => parameter is string target && !string.IsNullOrWhiteSpace(target));

        _machineSelectionCommand = new DelegateCommand(
            parameter => OnMachineSelectionRequested(parameter as string ?? string.Empty),
            parameter => parameter is string machineId && !string.IsNullOrWhiteSpace(machineId));
    }

    /// <summary>
    /// 設定頁面工廠 — App 層在 Start 前呼叫。
    /// </summary>
    public void ConfigurePageFactory(
        Func<Models.DeviceContext, UserControl> pageFactory,
        Action<UserControl, Models.DeviceContext>? applyContextAction = null)
    {
        _devicePages.PageFactory = pageFactory;
        _devicePages.ApplyContextAction = applyContextAction;
    }

    public virtual void Start()
    {
        if (_session is not null)
        {
            ComplianceContext.LogSystem("[Runtime] Skip duplicated MainWindow load initialization", Stackdose.Abstractions.Logging.LogLevel.Warning, showInUi: true);
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
            return;

        var navigationOrchestrator = new NavigationOrchestrator(
            _devicePages,
            bootstrapState.ShellPages,
            _logViewerPage,
            _userManagementPage,
            SettingsPage);

        _session = new AppSession(bootstrapState, navigationOrchestrator);

        ComplianceContext.LogSystem(
            $"[Runtime] Shell service mode: {_session.ServiceMode}",
            Stackdose.Abstractions.Logging.LogLevel.Info,
            showInUi: true);

        _session.Runtime.OverviewPage.MachineSelected += OnMachineSelected;
        _session.Runtime.OverviewPage.PlcScanUpdated += OnPlcScanUpdated;

        _suppressHeaderMachineSelection = true;
        _session.Shell.SetMachineOptions(_session.MachineOptions);
        _suppressHeaderMachineSelection = false;

        ApplyMetaSnapshot(_session.CurrentMetaSnapshot, updateCurrentPageTitle: false);
    }

    public virtual void Stop()
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
        _session?.NavigationOrchestrator.ShowMachineDetail(_session.Runtime, machineId);
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        if (_session?.Runtime.OverviewPage.MachineCards is null)
            return;

        _runtimeHost.Mapper.UpdateOverviewCards(manager, _session.Runtime.OverviewPage.MachineCards);
    }

    private void OnNavigationRequested(string target)
    {
        if (_session is null || string.IsNullOrWhiteSpace(target))
            return;

        _navigationService.TryNavigate(target);
    }

    private void ShowOverview()
    {
        if (_session is null) return;
        _session.NavigationOrchestrator.ShowOverview(_session.Runtime, _session.CurrentMetaSnapshot);
    }

    private void ShowCurrentOrFirstMachineDetail()
    {
        _session?.NavigationOrchestrator.ShowCurrentOrFirstMachineDetail(_session.Runtime);
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
        if (_session is null) return;
        OnSettingsNavigating?.Invoke(SettingsPage, _session.Runtime, _devicePages.SelectedMachineId);
        _session.NavigationOrchestrator.ShowSettings();
    }

    private void OnMachineSelectionRequested(string machineId)
    {
        if (_suppressHeaderMachineSelection || string.IsNullOrWhiteSpace(machineId) || _session is null)
            return;

        _session.NavigationOrchestrator.ShowMachineDetail(_session.Runtime, machineId);
    }

    private void OnMetaSnapshotChanged(object? sender, ShellMetaSnapshotChangedEventArgs<MetaSnapshot> e)
    {
        ApplyMetaSnapshot(e.Snapshot, updateCurrentPageTitle: true);
    }

    private void ApplyMetaSnapshot(MetaSnapshot snapshot, bool updateCurrentPageTitle)
    {
        if (_session is null) return;

        _session.CurrentMetaSnapshot = snapshot;
        _mainShell.HeaderDeviceName = snapshot.Meta.HeaderDeviceName;
        _runtimeHost.Mapper.ApplyMeta(_session.Runtime.OverviewPage, snapshot.Meta);
        _mainShell.NavigationItems = snapshot.NavigationItems;
        _navigationService.UpdateTitles(snapshot.NavigationTitles);

        if (updateCurrentPageTitle)
            _session.NavigationOrchestrator.UpdateCurrentPageTitle(snapshot);
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
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
