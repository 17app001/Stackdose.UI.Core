using Stackdose.Abstractions.Logging;
using Stackdose.Abstractions.Print;
using Stackdose.PrintHead.Feiyang;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PrintHead 連線狀態控制項
    /// 功能：連線管理、溫度監控、狀態顯示
    /// </summary>
    public partial class PrintHeadStatus : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ConfigFilePathProperty =
            DependencyProperty.Register("ConfigFilePath", typeof(string), typeof(PrintHeadStatus),
                new PropertyMetadata("Config/feiyang_head1.json"));

        public string ConfigFilePath
        {
            get => (string)GetValue(ConfigFilePathProperty);
            set => SetValue(ConfigFilePathProperty, value);
        }

        public static readonly DependencyProperty HeadNameProperty =
            DependencyProperty.Register("HeadName", typeof(string), typeof(PrintHeadStatus),
                new PropertyMetadata("PrintHead 1"));

        public string HeadName
        {
            get => (string)GetValue(HeadNameProperty);
            set => SetValue(HeadNameProperty, value);
        }

        public static readonly DependencyProperty HeadIndexProperty =
            DependencyProperty.Register("HeadIndex", typeof(int), typeof(PrintHeadStatus),
                new PropertyMetadata(0));

        public int HeadIndex
        {
            get => (int)GetValue(HeadIndexProperty);
            set => SetValue(HeadIndexProperty, value);
        }

        public static readonly DependencyProperty AutoConnectProperty =
            DependencyProperty.Register("AutoConnect", typeof(bool), typeof(PrintHeadStatus),
                new PropertyMetadata(false));

        public bool AutoConnect
        {
            get => (bool)GetValue(AutoConnectProperty);
            set => SetValue(AutoConnectProperty, value);
        }

        #endregion

        #region Fields

        private PrintHeadConfig? _config;
        private IPrintHead? _printHead;
        private CancellationTokenSource? _temperatureMonitorCts;
        private bool _isConnected = false;
        private bool _isExpanded = false;
        private double _collapsedHeight = double.NaN;

        #endregion

        #region Events

        public event Action? ConnectionEstablished;
        public event Action<string>? ConnectionLost;

        #endregion

        public PrintHeadStatus()
        {
            InitializeComponent();

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
        }

        #region 初始化

        private async void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (!LoadConfiguration())
                return;

            ResetStatusDisplay();

            if (AutoConnect && !_isConnected && _printHead == null)
            {
                await Task.Delay(500);
                await ConnectAsync();
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            // 保持連線與監控，Tab 切換不重啟
        }

        private void ResetStatusDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                TemperatureText.Text = "N/A";
                EncoderText.Text = "N/A";
                PrintIndexText.Text = "N/A";
                VoltagesPanel.ItemsSource = new[] { "N/A" };
            });

            ComplianceContext.LogSystem(
                "[PrintHead] Status display reset to N/A",
                LogLevel.Info,
                showInUi: false
            );
        }

        #endregion

        #region 展開/收合

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            AnimateExpand(_isExpanded);
        }

        private void AnimateExpand(bool expand)
        {
            var rotateAnimation = new DoubleAnimation
            {
                To = expand ? 180 : 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var rotation = ExpandIcon.RenderTransform as RotateTransform;
            rotation?.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

            StatusDataPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;

            if (expand)
            {
                _collapsedHeight = double.IsNaN(Height) ? ActualHeight : Height;
                Height = double.NaN;
            }
            else
            {
                Height = _collapsedHeight;
            }
        }

        #endregion

        #region 配置載入

        private bool LoadConfiguration()
        {
            try
            {
                string fullPath = ResolveConfigPath(ConfigFilePath);

                if (!File.Exists(fullPath))
                {
                    UpdateStatus(false);
                    ComplianceContext.LogSystem(
                        $"[PrintHead] Config file not found: {fullPath}",
                        LogLevel.Error,
                        showInUi: false
                    );
                    return false;
                }

                string jsonContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                _config = LoadSingleOrMultiHeadConfig(jsonContent);

                if (_config == null)
                {
                    UpdateStatus(false);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_config.Firmware.WaveformPath) && !Path.IsPathRooted(_config.Firmware.WaveformPath))
                {
                    _config.Firmware.WaveformPath = ResolveWavePath(_config.Firmware.WaveformPath);
                    if (!File.Exists(_config.Firmware.WaveformPath))
                        ComplianceContext.LogSystem(
                            $"[PrintHead] Waveform file not found: {_config.Firmware.WaveformPath}. Place .data file in Config/waves/.",
                            LogLevel.Warning,
                            showInUi: false
                        );
                }

                Dispatcher.Invoke(() =>
                {
                    BoardAddressText.Text = $"{_config.BoardIP}:{_config.BoardPort}";
                });

                ComplianceContext.LogSystem(
                    $"[PrintHead] Config loaded: {_config.Name} ({_config.DriverType})",
                    LogLevel.Info,
                    showInUi: false
                );

                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus(false);
                ComplianceContext.LogSystem(
                    $"[PrintHead] Config load error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return false;
            }
        }

        private PrintHeadConfig? LoadSingleOrMultiHeadConfig(string jsonContent)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var single = JsonSerializer.Deserialize<PrintHeadConfig>(jsonContent, options);
            if (single != null && !string.IsNullOrWhiteSpace(single.BoardIP))
                return single;

            var rootNode = JsonNode.Parse(jsonContent) as JsonObject;
            var commonNode = rootNode?["Common"] as JsonObject;
            var headsNode = rootNode?["Heads"] as JsonArray;
            if (headsNode == null || headsNode.Count == 0)
                return null;

            JsonObject? selectedHead = null;
            if (!string.IsNullOrWhiteSpace(HeadName))
            {
                selectedHead = headsNode
                    .OfType<JsonObject>()
                    .FirstOrDefault(head => string.Equals(head?["Name"]?.GetValue<string>(), HeadName, StringComparison.OrdinalIgnoreCase));
            }

            selectedHead ??= headsNode
                .OfType<JsonObject>()
                .Where(head => head?["Enable"]?.GetValue<bool>() != false)
                .Skip(Math.Max(0, HeadIndex))
                .FirstOrDefault();

            selectedHead ??= headsNode
                .OfType<JsonObject>()
                .ElementAtOrDefault(Math.Clamp(HeadIndex, 0, headsNode.Count - 1));

            if (selectedHead == null)
                return null;

            var normalized = new JsonObject
            {
                ["Model"]    = commonNode?["Model"]?.GetValue<string>() ?? "M1536",
                ["Waveform"] = commonNode?["Waveform"]?.GetValue<string>() ?? string.Empty,
                ["Name"]     = selectedHead["Name"]?.GetValue<string>() ?? $"Head-{HeadIndex + 1}",
                ["BoardIP"]  = selectedHead["BoardIP"]?.GetValue<string>() ?? string.Empty,
                ["BoardPort"] = selectedHead["BoardPort"]?.GetValue<int>() ?? 0,
                ["PcIP"]     = selectedHead["PcIP"]?.GetValue<string>() ?? string.Empty,
                ["PcPort"]   = selectedHead["PcPort"]?.GetValue<int>() ?? 0,
                ["Firmware"] = selectedHead["Firmware"]?.DeepClone(),
                ["PrintMode"] = selectedHead["PrintMode"]?.DeepClone()
            };

            return normalized.Deserialize<PrintHeadConfig>(options);
        }

        #endregion

        #region 連線管理

        private async void PowerButton_Checked(object sender, RoutedEventArgs e)
        {
            await ConnectAsync();
        }

        private void PowerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            DisconnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (_config == null)
            {
                UpdateStatus(false);
                return;
            }

            if (_isConnected)
            {
                UpdateStatus(true);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                PowerButton.IsEnabled = false;
                UpdateStatusLight(Color.FromRgb(255, 193, 7));
            });

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHead] Connecting to {_config.Name} ({_config.BoardIP}:{_config.BoardPort})...",
                    LogLevel.Info,
                    showInUi: true
                );

                string fullPath = ResolveConfigPath(ConfigFilePath);
                var printHead = new FeiyangPrintHead(fullPath);
                printHead.Log = (msg) => ComplianceContext.LogSystem(msg, LogLevel.Info, showInUi: false);
                printHead.ConnectionStateChanged += OnPrintHeadStateChanged;

                bool connected = await printHead.Connect();

                if (!connected)
                {
                    string errorMsg = printHead.LastErrorMessage ?? "Unknown error";
                    UpdateStatus(false);
                    ComplianceContext.LogSystem(
                        $"[PrintHead] Connection failed: {errorMsg}",
                        LogLevel.Error,
                        showInUi: true
                    );
                    ConnectionLost?.Invoke(errorMsg);
                    return;
                }

                ComplianceContext.LogSystem(
                    $"[PrintHead] Socket connected, configuring Firmware...",
                    LogLevel.Info,
                    showInUi: true
                );

                var (firmwareSuccess, firmwareMsg) = await Task.Run(() => printHead.Setup());

                if (!firmwareSuccess)
                {
                    UpdateStatus(false);
                    ComplianceContext.LogSystem(
                        $"[PrintHead] Firmware config failed: {firmwareMsg}",
                        LogLevel.Error,
                        showInUi: true
                    );
                    ConnectionLost?.Invoke(firmwareMsg);
                    return;
                }

                ComplianceContext.LogSystem(
                    $"[PrintHead] Firmware configured, setting print mode...",
                    LogLevel.Info,
                    showInUi: true
                );

                var (printModeSuccess, printModeMsg) = await printHead.ConfigurePrintModeAsync();

                if (!printModeSuccess)
                {
                    ComplianceContext.LogSystem(
                        $"[PrintHead] PrintMode config failed: {printModeMsg}",
                        LogLevel.Warning,
                        showInUi: true
                    );
                }

                _printHead = printHead;
                _isConnected = true;

                UpdateStatus(true);

                ComplianceContext.LogSystem(
                    $"[PrintHead] Connection established: {_config.Name}",
                    LogLevel.Success,
                    showInUi: true
                );

                PrintHeadContext.RegisterPrintHead(_config.Name, _printHead);
                ConnectionEstablished?.Invoke();
                StartTemperatureMonitoring();
            }
            catch (Exception ex)
            {
                UpdateStatus(false);
                ComplianceContext.LogSystem(
                    $"[PrintHead] Connection error: {ex.Message}\nStack: {ex.StackTrace}",
                    LogLevel.Error,
                    showInUi: true
                );
                ConnectionLost?.Invoke(ex.Message);
            }
            finally
            {
                Dispatcher.Invoke(() => PowerButton.IsEnabled = true);
            }
        }

        private static string ResolveConfigPath(string configPath)
        {
            if (Path.IsPathRooted(configPath) && File.Exists(configPath))
                return configPath;

            var appRelative = Path.Combine(AppContext.BaseDirectory, configPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(appRelative))
                return appRelative;

            var resourcePath = ResourcePathHelper.GetResourceFilePath(configPath);
            if (File.Exists(resourcePath))
                return resourcePath;

            return appRelative;
        }

        private static string ResolveWavePath(string waveformPath)
        {
            var normalized = waveformPath.Replace('/', Path.DirectorySeparatorChar);
            var fromBase = Path.Combine(AppContext.BaseDirectory, normalized);
            if (File.Exists(fromBase)) return fromBase;

            var fromConfigs = Path.Combine(AppContext.BaseDirectory, "Configs", normalized);
            if (File.Exists(fromConfigs)) return fromConfigs;

            var fromConfig = Path.Combine(AppContext.BaseDirectory, "Config", normalized);
            if (File.Exists(fromConfig)) return fromConfig;

            return fromConfigs;
        }

        private void DisconnectAsync()
        {
            if (!_isConnected)
                return;

            StopTemperatureMonitoring();

            try
            {
                _printHead?.Disconnect();

                if (_config != null)
                    PrintHeadContext.UnregisterPrintHead(_config.Name);

                _printHead = null;
                _isConnected = false;

                ResetStatusDisplay();
                UpdateStatus(false);

                ComplianceContext.LogSystem(
                    $"[PrintHead] Disconnected: {_config?.Name}",
                    LogLevel.Info,
                    showInUi: true
                );
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[PrintHead] Disconnect error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
            }
        }

        #endregion

        #region 狀態監控

        private void StartTemperatureMonitoring()
        {
            StopTemperatureMonitoring();

            _temperatureMonitorCts = new CancellationTokenSource();
            var token = _temperatureMonitorCts.Token;

            ComplianceContext.LogSystem(
                "[PrintHead] Status monitoring started",
                LogLevel.Info,
                showInUi: true
            );

            Task.Run(async () =>
            {
                int successCount = 0;
                int errorCount = 0;
                bool firstReadSuccess = false;

                while (!token.IsCancellationRequested && _isConnected)
                {
                    try
                    {
                        if (_printHead == null) break;

                        // 溫度透過介面方法取得（型別安全）
                        var temps = _printHead.GetTemperatures();

                        // 其餘狀態仍透過 GetStatus()（SDK 裸型，dynamic 不可避免）
                        var rawStatus = _printHead.GetStatus();

                        successCount++;

                        if (!firstReadSuccess)
                        {
                            firstReadSuccess = true;
                            ComplianceContext.LogSystem(
                                "[PrintHead] First status read successful",
                                LogLevel.Success,
                                showInUi: true
                            );
                        }

                        Dispatcher.Invoke(() => UpdateStatusDisplay(temps, rawStatus));
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        if (errorCount <= 3)
                        {
                            ComplianceContext.LogSystem(
                                $"[PrintHead] Status read error #{errorCount}: {ex.Message}",
                                LogLevel.Warning,
                                showInUi: true
                            );
                        }
                    }

                    await Task.Delay(500, token);
                }

                ComplianceContext.LogSystem(
                    $"[PrintHead] Status monitoring stopped (success: {successCount}, errors: {errorCount})",
                    LogLevel.Info,
                    showInUi: true
                );
            }, token);
        }

        private void StopTemperatureMonitoring()
        {
            _temperatureMonitorCts?.Cancel();
            _temperatureMonitorCts?.Dispose();
            _temperatureMonitorCts = null;
        }

        private void UpdateStatusDisplay(float[] temps, object? rawStatus)
        {
            // 溫度 — 強型別
            if (temps.Length > 0 && temps[0] >= 0 && temps[0] <= 100)
                TemperatureText.Text = $"{temps[0]:F1}°C";

            if (rawStatus == null) return;

            // 電壓/編碼器/PrintIndex — SDK 裸型，dynamic 是唯一選項
            try
            {
                dynamic d = rawStatus;

                var voltages = new System.Collections.Generic.List<string>();
                if (d.VoltageActualA != null)
                    voltages.Add($"V1: {(double)d.VoltageActualA:F1}V");
                if (d.DriveVoltages != null && d.DriveVoltages.Length >= 4)
                {
                    voltages.Add($"V2: {d.DriveVoltages[1]:F1}V");
                    voltages.Add($"V3: {d.DriveVoltages[2]:F1}V");
                    voltages.Add($"V4: {d.DriveVoltages[3]:F1}V");
                }
                if (voltages.Count > 0)
                    VoltagesPanel.ItemsSource = voltages;

                if (d.GratingCount != null)
                    EncoderText.Text = ((double)d.GratingCount).ToString("F0");

                if (d.PrintIndex != null)
                    PrintIndexText.Text = ((uint)d.PrintIndex).ToString();
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[PrintHead] UpdateStatusDisplay error: {ex.Message}",
                    LogLevel.Warning,
                    showInUi: false
                );
            }
        }

        #endregion

        #region UI 更新

        private void UpdateStatus(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = connected;
                Color statusColor = connected
                    ? Color.FromRgb(46, 204, 113)
                    : Color.FromRgb(255, 71, 87);
                UpdateStatusLight(statusColor);
                PowerButton.IsChecked = connected;
            });
        }

        private void UpdateStatusLight(Color color)
        {
            StatusLight.Fill = new SolidColorBrush(color);
            if (StatusGlow != null)
                StatusGlow.Color = color;
        }

        private void OnPrintHeadStateChanged(PrintHeadConnectionState newState)
        {
            Dispatcher.Invoke(() => UpdateConnectionStateDisplay(newState));
        }

        private void UpdateConnectionStateDisplay(PrintHeadConnectionState state)
        {
            // StateText 是 XAML x:Name 產生的欄位，直接使用
            (string text, Color color) = state switch
            {
                PrintHeadConnectionState.Idle       => ("IDLE",     Color.FromRgb(200, 200, 200)),
                PrintHeadConnectionState.Ready      => ("READY",    Color.FromRgb(46,  204, 113)),
                PrintHeadConnectionState.Printing   => ("PRINTING", Color.FromRgb(0,   230, 118)),
                PrintHeadConnectionState.Spit       => ("SPITTING", Color.FromRgb(255, 235,  59)),
                PrintHeadConnectionState.Error      => ("ERROR",    Color.FromRgb(244,  67,  54)),
                _                                   => (state.ToString().ToUpper(), Color.FromRgb(158, 158, 158)),
            };

            StateText.Text = text;
            StateText.Foreground = new SolidColorBrush(color);
        }

        #endregion
    }
}
