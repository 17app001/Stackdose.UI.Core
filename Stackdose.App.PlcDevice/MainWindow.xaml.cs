using Stackdose.Abstractions.Hardware;
using Stackdose.App.PlcDevice.Models;
using Stackdose.App.PlcDevice.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Stackdose.App.PlcDevice;

public partial class MainWindow : Window
{
    private MainContainer? _mainContainer;
    private readonly Dictionary<string, PlcMachineConfig> _machineConfigs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PlcMachineRuntime> _machineRuntimes = new(StringComparer.OrdinalIgnoreCase);
    private string? _selectedMachineId;
    private MachineOverviewPage? _overviewPage;
    private MachineDetailPage? _detailPage;
    private LogViewerPage? _logViewerPage;
    private UserManagementPage? _userManagementPage;
    private DispatcherTimer? _uiTimer;
    private IPlcManager? _plcManager;
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _mainContainer = FindMainContainer(this);
        if (_mainContainer == null)
        {
            MessageBox.Show("MainContainer not found.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        foreach (var config in LoadMachineConfigs())
        {
            _machineConfigs[config.Machine.Id] = config;
            _machineRuntimes[config.Machine.Id] = new PlcMachineRuntime(config);
        }

        if (_machineConfigs.Count == 0)
        {
            MessageBox.Show("No machine config found.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _selectedMachineId = _machineConfigs.Keys.First();
        var primaryConfig = _machineConfigs[_selectedMachineId];

        SecurityContext.QuickLogin(AccessLevel.SuperAdmin);

        _overviewPage = new MachineOverviewPage
        {
            PlcIpAddress = primaryConfig.Plc.Ip,
            PlcPort = primaryConfig.Plc.Port,
            PlcScanInterval = primaryConfig.Plc.PollIntervalMs,
            PlcAutoConnect = true,
            PlcMonitorAddresses = PlcMonitorAddressBuilder.Build(_machineConfigs.Values)
        };
        _overviewPage.PlcScanUpdated += OnPlcScanUpdated;
        _overviewPage.MachineSelected += OnMachineSelected;

        _detailPage = new MachineDetailPage();
        _detailPage.StartRequested += (_, _) => ExecuteStart();
        _detailPage.StopRequested += (_, _) => ExecuteStop();
        _detailPage.ResetRequested += (_, _) => ExecuteReset();

        _logViewerPage = new LogViewerPage();
        _userManagementPage = new UserManagementPage();

        _mainContainer.SetShellMode(true);
        _mainContainer.SetCurrentMachineDisplayName(primaryConfig.Machine.Name);
        NavigateTo("MachineOverviewPage");

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(100, primaryConfig.Plc.PollIntervalMs))
        };
        _uiTimer.Tick += (_, _) => RefreshUi();
        _uiTimer.Start();

        ComplianceContext.LogSystem("PLC starter project initialized", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _uiTimer?.Stop();
    }

    private IEnumerable<PlcMachineConfig> LoadMachineConfigs()
    {
        var configDirectory = Path.Combine(AppContext.BaseDirectory, "Config");
        if (!Directory.Exists(configDirectory))
        {
            yield break;
        }

        var configFiles = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in configFiles)
        {
            PlcMachineConfig? config = null;
            try
            {
                config = PlcMachineConfigLoader.Load(file);
            }
            catch
            {
                // Skip invalid config files to keep startup resilient.
            }

            if (config != null && config.Machine.Enable)
            {
                yield return config;
            }
        }
    }

    private void RefreshUi()
    {
        if (_machineConfigs.Count == 0 || _machineRuntimes.Count == 0)
        {
            return;
        }

        if (_machineRuntimes.Values.All(runtime => !runtime.IsConnected))
        {
            foreach (var runtime in _machineRuntimes.Values)
            {
                runtime.Tick();
            }
        }

        if (_overviewPage != null)
        {
            _overviewPage.MachineCards =
            [
                .. _machineConfigs.Values.Select(config => BuildMachineCard(config, _machineRuntimes[config.Machine.Id]))
            ];
        }

        UpdateDetailPageBySelection();
    }

    private static MachineOverviewCard BuildMachineCard(PlcMachineConfig config, PlcMachineRuntime runtime)
    {
        var nozzleUnit = config.Tags.Process.TryGetValue("nozzleTemp", out var nozzleTag) && !string.IsNullOrWhiteSpace(nozzleTag.Unit)
            ? nozzleTag.Unit
            : "C";

        var statusBrush = runtime.IsAlarm
            ? Brushes.IndianRed
            : runtime.IsRunning
                ? Brushes.SeaGreen
                : Brushes.SlateGray;

        return new MachineOverviewCard
        {
            MachineId = config.Machine.Id,
            Title = config.Machine.Name,
            BatchValue = runtime.BatchNumber,
            RecipeText = runtime.RecipeName,
            StatusText = runtime.MachineState,
            StatusBrush = statusBrush,
            LeftTopLabel = "Heartbeat",
            LeftTopValue = runtime.Heartbeat.ToString(),
            LeftBottomLabel = "Alarm",
            LeftBottomValue = runtime.AlarmState,
            RightTopLabel = "Nozzle",
            RightTopValue = $"{runtime.NozzleTempC:F1} {nozzleUnit}",
            RightBottomLabel = "Mode",
            RightBottomValue = runtime.IsRunning ? "Auto" : "Manual"
        };
    }

    private void OnMachineSelected(string machineId)
    {
        if (!_machineConfigs.ContainsKey(machineId))
        {
            return;
        }

        _selectedMachineId = machineId;
        NavigateToMachineDetail(machineId);
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        _plcManager = manager;

        foreach (var config in _machineConfigs.Values)
        {
            var runtime = _machineRuntimes[config.Machine.Id];
            var isConnected = ReadBool(manager, config.Tags.Status, "isConnected") ?? manager.IsConnected;
            var isRunning = ReadBool(manager, config.Tags.Status, "isRunning") ?? false;
            var isAlarm = ReadBool(manager, config.Tags.Status, "isAlarm") ?? false;
            var heartbeat = ReadInt(manager, config.Tags.Status, "heartbeat") ?? 0;
            var modeRaw = ReadInt(manager, config.Tags.Status, "mode") ?? 0;
            var nozzleRaw = ReadInt(manager, config.Tags.Process, "nozzleTemp");
            var batchNo = ReadString(manager, config.Tags.Process, "batchNo");
            var recipeNo = ReadString(manager, config.Tags.Process, "recipeNo");
            var nozzleTemp = nozzleRaw.HasValue
                ? nozzleRaw.Value * (config.Tags.Process.TryGetValue("nozzleTemp", out var nozzleTag) ? nozzleTag.Scale : 1.0)
                : (double?)null;

            var modeText = modeRaw == 1 ? "Auto" : "Manual";
            runtime.ApplySnapshot(isConnected, isRunning, isAlarm, heartbeat, modeText, nozzleTemp, batchNo, recipeNo);
        }
    }

    private static bool? ReadBool(IPlcManager manager, Dictionary<string, PlcTagConfig> section, string key)
    {
        if (!section.TryGetValue(key, out var tag))
        {
            return null;
        }

        return manager.ReadBit(tag.Address);
    }

    private static int? ReadInt(IPlcManager manager, Dictionary<string, PlcTagConfig> section, string key)
    {
        if (!section.TryGetValue(key, out var tag))
        {
            return null;
        }

        return tag.Type.Equals("int16", StringComparison.OrdinalIgnoreCase)
            ? manager.ReadWord(tag.Address)
            : manager.ReadDWord(tag.Address);
    }

    private static string? ReadString(IPlcManager manager, Dictionary<string, PlcTagConfig> section, string key)
    {
        if (!section.TryGetValue(key, out var tag))
        {
            return null;
        }

        var parsed = ParseAddress(tag.Address);
        if (parsed == null)
        {
            return null;
        }

        var (prefix, startAddress) = parsed.Value;
        var length = Math.Max(1, tag.Length);
        var buffer = new List<byte>(length * 2);

        for (var i = 0; i < length; i++)
        {
            var word = manager.ReadWord($"{prefix}{startAddress + i}");
            if (!word.HasValue)
            {
                break;
            }

            var low = (byte)(word.Value & 0xFF);
            var high = (byte)((word.Value >> 8) & 0xFF);
            buffer.Add(low);
            buffer.Add(high);
        }

        if (buffer.Count == 0)
        {
            return null;
        }

        var text = Encoding.ASCII.GetString(buffer.ToArray()).TrimEnd('\0', ' ');
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static (string Prefix, int Address)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[2].Value, out var index))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), index);
    }

    private async void ExecuteStart()
    {
        if (!TryGetSelectedMachine(out var config, out var runtime))
        {
            return;
        }

        var ok = runtime.Start();
        if (!ok)
        {
            MessageBox.Show("Cannot start while alarm is active.", "Command Blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_plcManager != null && config.Tags.Command.TryGetValue("start", out var startTag))
        {
            await WritePulseAsync(_plcManager, startTag);
        }
    }

    private async void ExecuteStop()
    {
        if (!TryGetSelectedMachine(out var config, out var runtime))
        {
            return;
        }

        runtime.Stop();
        if (_plcManager != null && config.Tags.Command.TryGetValue("stop", out var stopTag))
        {
            await WritePulseAsync(_plcManager, stopTag);
        }
    }

    private async void ExecuteReset()
    {
        if (!TryGetSelectedMachine(out var config, out var runtime))
        {
            return;
        }

        runtime.Reset();
        if (_plcManager != null && config.Tags.Command.TryGetValue("reset", out var resetTag))
        {
            await WritePulseAsync(_plcManager, resetTag);
        }
    }

    private bool TryGetSelectedMachine(out PlcMachineConfig config, out PlcMachineRuntime runtime)
    {
        config = null!;
        runtime = null!;

        if (string.IsNullOrWhiteSpace(_selectedMachineId))
        {
            return false;
        }

        if (!_machineConfigs.TryGetValue(_selectedMachineId, out var foundConfig) || foundConfig is null)
        {
            return false;
        }

        if (!_machineRuntimes.TryGetValue(_selectedMachineId, out var foundRuntime) || foundRuntime is null)
        {
            return false;
        }

        config = foundConfig;
        runtime = foundRuntime;

        return true;
    }

    private static async Task WritePulseAsync(IPlcManager manager, PlcTagConfig commandTag)
    {
        var pulseMs = commandTag.PulseMs > 0 ? commandTag.PulseMs : 200;
        await manager.WriteAsync($"{commandTag.Address},0,1");
        await Task.Delay(pulseMs);
        await manager.WriteAsync($"{commandTag.Address},0,0");
    }

    private void NavigateTo(string target)
    {
        if (_mainContainer == null)
        {
            return;
        }

        switch (target)
        {
            case "MachineOverviewPage":
                if (_overviewPage != null)
                {
                    _mainContainer.SetContent(_overviewPage, "Machine Overview");
                }
                break;
            case "MachineDetailPage":
                if (string.IsNullOrWhiteSpace(_selectedMachineId))
                {
                    _selectedMachineId = _machineConfigs.Keys.FirstOrDefault();
                }

                if (!string.IsNullOrWhiteSpace(_selectedMachineId))
                {
                    NavigateToMachineDetail(_selectedMachineId);
                }
                break;
            case "LogViewerPage":
                if (_logViewerPage != null)
                {
                    _mainContainer.SetContent(_logViewerPage, "Logs - System Events");
                }
                break;
            case "UserManagementPage":
                if (_userManagementPage != null)
                {
                    _mainContainer.SetContent(_userManagementPage, "Users - Access Control");
                }
                break;
            case "SettingsPage":
                MessageBox.Show("Settings template page is reserved for project-specific implementation.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            default:
                break;
        }
    }

    private void NavigateToMachineDetail(string machineId)
    {
        if (_mainContainer == null || _detailPage == null)
        {
            return;
        }

        if (!_machineConfigs.TryGetValue(machineId, out var config))
        {
            return;
        }

        _selectedMachineId = machineId;
        _mainContainer.SetCurrentMachineDisplayName(config.Machine.Name);
        UpdateDetailPageBySelection();
        _mainContainer.SetContent(_detailPage, $"{config.Machine.Name} - Machine Detail");
    }

    private void UpdateDetailPageBySelection()
    {
        if (_detailPage == null || string.IsNullOrWhiteSpace(_selectedMachineId))
        {
            return;
        }

        if (!_machineConfigs.TryGetValue(_selectedMachineId, out var config)
            || !_machineRuntimes.TryGetValue(_selectedMachineId, out var runtime))
        {
            return;
        }

        _detailPage.MachineTitle = config.Machine.Name;
        _detailPage.BatchNumber = runtime.BatchNumber;
        _detailPage.RecipeName = runtime.RecipeName;
        _detailPage.MachineState = runtime.MachineState;
        _detailPage.AlarmState = runtime.AlarmState;
        var nozzleUnit = config.Tags.Process.TryGetValue("nozzleTemp", out var nozzleTag) && !string.IsNullOrWhiteSpace(nozzleTag.Unit)
            ? nozzleTag.Unit
            : "C";
        _detailPage.NozzleTempText = $"{runtime.NozzleTempC:F1} {nozzleUnit}";
    }

    private void OnNavigate(object sender, string target)
    {
        NavigateTo(target);
    }

    private void OnLogout(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnClose(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnMinimize(object? sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private static MainContainer? FindMainContainer(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is MainContainer container)
            {
                return container;
            }

            var nested = FindMainContainer(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
