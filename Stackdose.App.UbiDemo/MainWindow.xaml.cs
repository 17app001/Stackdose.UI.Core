using Stackdose.Abstractions.Hardware;
using Stackdose.App.UbiDemo.Pages;
using Stackdose.App.UbiDemo.Services;
using Stackdose.UI.Templates.Pages;
using System.Windows;

namespace Stackdose.App.UbiDemo;

public partial class MainWindow : Window
{
    private UbiRuntimeContext? _runtime;
    private readonly LogViewerPage _logViewerPage = new();
    private readonly UserManagementPage _userManagementPage = new();
    private readonly SettingsPage _settingsPage = new();
    private readonly Dictionary<string, UbiDevicePage> _devicePages = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultPageTitle = "Machine Overview";
    private string? _selectedMachineId;
    private bool _suppressHeaderMachineSelection;

    public MainWindow()
    {
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

        _defaultPageTitle = MainShell.PageTitle;
        _runtime.OverviewPage.MachineSelected += OnMachineSelected;
        _runtime.OverviewPage.PlcScanUpdated  += OnPlcScanUpdated;
        MainShell.NavigationRequested        += OnNavigationRequested;
        MainShell.MachineSelectionRequested  += OnMachineSelectionRequested;

        _suppressHeaderMachineSelection = true;
        MainShell.MachineOptions = _runtime.Machines.Values
            .Select(machine => new KeyValuePair<string, string>(
                machine.Machine.Id,
                $"{machine.Machine.Name} ({machine.Machine.Id})"))
            .ToList();
        _suppressHeaderMachineSelection = false;

        MainShell.SelectNavigationTarget("MachineOverviewPage");
        UbiRuntimeMapper.BuildRuntimeMaps(_runtime.Machines);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_runtime is not null)
        {
            _runtime.OverviewPage.MachineSelected -= OnMachineSelected;
            _runtime.OverviewPage.PlcScanUpdated  -= OnPlcScanUpdated;
        }

        MainShell.NavigationRequested       -= OnNavigationRequested;
        MainShell.MachineSelectionRequested -= OnMachineSelectionRequested;
    }

    private void OnMachineSelected(string machineId)
    {
        NavigateToDevicePage(machineId);
        MainShell.SelectNavigationTarget("MachineDetailPage");
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
        if (_runtime is null)
        {
            return;
        }

        switch (target.ToUpperInvariant())
        {
            case "MACHINEOVERVIEWPAGE":
                MainShell.ShellContent = _runtime.OverviewPage;
                MainShell.PageTitle = _defaultPageTitle;
                MainShell.SelectNavigationTarget("MachineOverviewPage");
                if (!string.IsNullOrWhiteSpace(_selectedMachineId)
                    && _runtime.Machines.TryGetValue(_selectedMachineId, out var sel))
                {
                    MainShell.CurrentMachineDisplayName = sel.Machine.Name;
                }
                break;

            case "MACHINEDETAILPAGE":
                MainShell.SelectNavigationTarget("MachineDetailPage");
                var targetId = !string.IsNullOrWhiteSpace(_selectedMachineId)
                               && _runtime.Machines.ContainsKey(_selectedMachineId)
                    ? _selectedMachineId
                    : _runtime.Machines.Keys.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    NavigateToDevicePage(targetId);
                }
                break;

            case "LOGVIEWERPAGE":
                MainShell.ShellContent = _logViewerPage;
                MainShell.PageTitle = "Log Viewer";
                MainShell.SelectNavigationTarget("LogViewerPage");
                break;

            case "USERMANAGEMENTPAGE":
                MainShell.ShellContent = _userManagementPage;
                MainShell.PageTitle = "User Management";
                MainShell.SelectNavigationTarget("UserManagementPage");
                break;

            case "SETTINGSPAGE":
                _settingsPage.SetMonitorAddresses(_runtime.OverviewPage.PlcMonitorAddresses);
                MainShell.ShellContent = _settingsPage;
                MainShell.PageTitle = "Maintenance Mode";
                MainShell.SelectNavigationTarget("SettingsPage");
                break;
        }
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

        MainShell.ShellContent = devicePage;
        MainShell.CurrentMachineDisplayName = config.Machine.Name;
        MainShell.SelectedMachineId = machineId;
        MainShell.PageTitle = "Machine Detail";
    }

    private void OnMachineSelectionRequested(object? sender, string machineId)
    {
        if (_suppressHeaderMachineSelection || string.IsNullOrWhiteSpace(machineId))
        {
            return;
        }

        NavigateToDevicePage(machineId);
        MainShell.SelectNavigationTarget("MachineDetailPage");
    }
}
