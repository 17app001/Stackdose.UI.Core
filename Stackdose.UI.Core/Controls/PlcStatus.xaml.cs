using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.Hardware.Plc;
using Stackdose.Mitsubishi.Plc;
using Stackdose.UI.Core.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcStatus : UserControl, IDisposable
    {
        private const int RetryDelayMs = 2000;
        private const int WatchdogPollingIntervalMs = 3000;

        private IPlcManager? _plcManager;
        private bool _isBusy = false;

        // 🔥 用來控制看門狗是否應該繼續運行
        private CancellationTokenSource? _watchdogCts;
        
        // 🔥 追蹤是否已訂閱事件
        private bool _isEventSubscribed = false;

        // 🔥 追蹤本次連線已註冊的 monitor 區塊，避免重複註冊
        private readonly HashSet<string> _registeredMonitorKeys = new(StringComparer.OrdinalIgnoreCase);

        // 🔥 預建立靜態 Brush/Effect，避免每次 scan 都 new（GC 壓力 + UI 執行緒負擔）
        private static readonly SolidColorBrush s_limeGreenBrush = CreateFrozenBrush(Colors.LimeGreen);
        private static readonly SolidColorBrush s_orangeBrush = CreateFrozenBrush(Colors.Orange);
        private static readonly SolidColorBrush s_redBrush = CreateFrozenBrush(Colors.Red);
        private static readonly SolidColorBrush s_grayBrush = CreateFrozenBrush(Colors.Gray);
        private static readonly DropShadowEffect s_greenGlow = CreateFrozenGlow(Colors.LimeGreen, 15);
        private static readonly DropShadowEffect s_redGlow = CreateFrozenGlow(Colors.Red, 10);

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static DropShadowEffect CreateFrozenGlow(Color color, double blurRadius)
        {
            var effect = new DropShadowEffect { Color = color, BlurRadius = blurRadius, ShadowDepth = 0 };
            effect.Freeze();
            return effect;
        }

        // 🔥 節流：避免每次 scan 都觸發 UI 更新
        private string _lastScanInfo = "";

        public IPlcManager? CurrentManager => _plcManager;
        
        // 🔥 當 PLC 連線成功時觸發的事件
        public event Action<IPlcManager>? ConnectionEstablished;

        // 🔥 當 PLC 斷線（看門狗偵測）時觸發的事件
        public event Action? ConnectionLost;

        public event Action<IPlcManager>? ScanUpdated;

        public PlcStatus()
        {
            InitializeComponent();
            this.Loaded += PlcStatus_Loaded;
            this.Unloaded += PlcStatus_Unloaded;
            this.Cursor = Cursors.Hand;
            this.MouseLeftButtonDown += PlcStatus_MouseLeftButtonDown;
        }

        #region Dependency Properties

        public static readonly DependencyProperty IpAddressProperty =
             DependencyProperty.Register("IpAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata("127.0.0.1"));
        public string IpAddress { get { return (string)GetValue(IpAddressProperty); } set { SetValue(IpAddressProperty, value); } }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(int), typeof(PlcStatus), new PropertyMetadata(502));
        public int Port { get { return (int)GetValue(PortProperty); } set { SetValue(PortProperty, value); } }

        public static readonly DependencyProperty AutoConnectProperty =
            DependencyProperty.Register("AutoConnect", typeof(bool), typeof(PlcStatus), new PropertyMetadata(false));
        public bool AutoConnect { get { return (bool)GetValue(AutoConnectProperty); } set { SetValue(AutoConnectProperty, value); } }

        public static readonly DependencyProperty ScanIntervalProperty =
            DependencyProperty.Register("ScanInterval", typeof(int), typeof(PlcStatus), new PropertyMetadata(150));
        public int ScanInterval { get { return (int)GetValue(ScanIntervalProperty); } set { SetValue(ScanIntervalProperty, value); } }

        public static readonly DependencyProperty MonitorAddressProperty =
            DependencyProperty.Register("MonitorAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata(null, OnMonitorAddressChanged));
        public string MonitorAddress { get { return (string)GetValue(MonitorAddressProperty); } set { SetValue(MonitorAddressProperty, value); } }

        public static readonly DependencyProperty MonitorLengthProperty =
            DependencyProperty.Register("MonitorLength", typeof(int), typeof(PlcStatus), new PropertyMetadata(1, OnMonitorLengthChanged));
        public int MonitorLength { get { return (int)GetValue(MonitorLengthProperty); } set { SetValue(MonitorLengthProperty, value); } }

        public static readonly DependencyProperty IsGlobalProperty =
            DependencyProperty.Register("IsGlobal", typeof(bool), typeof(PlcStatus), new PropertyMetadata(true, OnIsGlobalChanged));
        public bool IsGlobal { get { return (bool)GetValue(IsGlobalProperty); } set { SetValue(IsGlobalProperty, value); } }

        public static readonly DependencyProperty MaxRetryCountProperty =
            DependencyProperty.Register("MaxRetryCount", typeof(int), typeof(PlcStatus), new PropertyMetadata(3));
        public int MaxRetryCount { get { return (int)GetValue(MaxRetryCountProperty); } set { SetValue(MaxRetryCountProperty, value); } }

        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register("ShowBorder", typeof(bool), typeof(PlcStatus), new PropertyMetadata(true));
        public bool ShowBorder { get { return (bool)GetValue(ShowBorderProperty); } set { SetValue(ShowBorderProperty, value); } }

        private static void OnIsGlobalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatus plcStatus && (bool)e.NewValue) PlcContext.GlobalStatus = plcStatus;
        }

        private static void OnMonitorAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatus plcStatus)
            {
                plcStatus.TryRegisterConfiguredMonitorsAndRefresh();
            }
        }

        private static void OnMonitorLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatus plcStatus)
            {
                plcStatus.TryRegisterConfiguredMonitorsAndRefresh();
            }
        }

        #endregion

        private void TryRegisterConfiguredMonitorsAndRefresh()
        {
            if (_plcManager?.Monitor == null || !_plcManager.IsConnected)
            {
                return;
            }

            RegisterAllMonitors();
            ScanUpdated?.Invoke(_plcManager);
        }

        private async void PlcStatus_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Loaded - IsGlobal={IsGlobal}, AutoConnect={AutoConnect}, this={this.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Current GlobalStatus={PlcContext.GlobalStatus?.GetHashCode()}, IsConnected={PlcContext.GlobalStatus?.CurrentManager?.IsConnected}");

            if (IsGlobal)
            {
                var existingGlobal = PlcContext.GlobalStatus;

                // 🔥 情況1: 已有全局實例且已連線，且不是自己 → 接管連線
                if (existingGlobal != null && existingGlobal != this && existingGlobal.CurrentManager?.IsConnected == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlcStatus] Reusing existing connection from {existingGlobal.GetHashCode()}");

                    // 🔥 關鍵：接管 PlcManager（共用同一個連線）
                    _plcManager = existingGlobal.CurrentManager;

                    // 🔥 關鍵：將自己設為新的全局實例（這樣 PlcLabel 才能正確綁定到我）
                    PlcContext.GlobalStatus = this;

                    // 🔥 訂閱 PlcManager 的 ScanElapsedChanged 事件
                    SubscribeToPlcManager();

                    // 更新 UI 狀態
                    UpdateUiState(ConnectionState.Connected);

                    ComplianceContext.LogSystem("[PlcStatus] Took over existing PLC connection", LogLevel.Info, showInUi: false);

                    // 🔥 立即觸發一次 ScanUpdated，讓所有 PlcLabel 能夠讀取數據
                    ScanUpdated?.Invoke(_plcManager);

                    // 🔥 啟動看門狗
                    StartConnectionWatchdog();

                    return;
                }

                // 🔥 情況2: 沒有全局實例，或全局實例未連線
                PlcContext.GlobalStatus = this;
                ComplianceContext.LogSystem("System initialized. Main PLC set.", LogLevel.Info);
            }

            // 🔥 如果是 AutoConnect，則自動連線；否則顯示等待點擊的狀態
            if (AutoConnect && (_plcManager == null || !_plcManager.IsConnected))
            {
                await ConnectAsync();
            }
            else if (!AutoConnect)
            {
                UpdateUiState(ConnectionState.Failed);
                StatusText.Text = "Click to Connect";
            }
        }

        /// <summary>
        /// 🔥 訂閱 PlcManager 的 ScanElapsedChanged 事件
        /// </summary>
        private void SubscribeToPlcManager()
        {
            if (_plcManager is PlcManager pm && !_isEventSubscribed)
            {
                pm.ScanElapsedChanged += OnScanElapsedChanged;
                _isEventSubscribed = true;
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] ScanElapsedChanged event subscribed (this={this.GetHashCode()})");
            }
        }

        /// <summary>
        /// 🔥 取消訂閱 PlcManager 的 ScanElapsedChanged 事件
        /// </summary>
        private void UnsubscribeFromPlcManager()
        {
            if (_plcManager is PlcManager pm && _isEventSubscribed)
            {
                pm.ScanElapsedChanged -= OnScanElapsedChanged;
                _isEventSubscribed = false;
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] ScanElapsedChanged event unsubscribed (this={this.GetHashCode()})");
            }
        }

        /// <summary>
        /// 🔥 處理 PlcManager 的 ScanElapsedChanged 事件
        /// </summary>
        private void OnScanElapsedChanged(string scanInfo)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;

                // 🔥 節流：避免每次 scan 都觸發 UI 更新
                if (_lastScanInfo == scanInfo) return;
                _lastScanInfo = scanInfo;

                Dispatcher.BeginInvoke(() =>
                {
                    if (Dispatcher.HasShutdownStarted) return;

                    StatusText.Text = $"ONLINE ({scanInfo})";
                    StatusLight.Fill = s_limeGreenBrush;
                    StatusLight.Effect = s_greenGlow;
                    StatusText.Foreground = s_limeGreenBrush;

                    // 在 UI 執行緒上觸發 ScanUpdated
                    ScanUpdated?.Invoke(_plcManager!);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] OnScanElapsedChanged error: {ex.Message}");
            }
        }

        private async void PlcStatus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => await ToggleConnectionAsync();

        private async Task ToggleConnectionAsync()
        {
            if (_isBusy) return;
            try
            {
                _isBusy = true;
                if (_plcManager != null && _plcManager.IsConnected)
                {
                    CancelWatchdog();
                    await DisconnectAsync();
                }
                else
                {
                    await ConnectAsync();
                }
            }
            finally { _isBusy = false; }
        }

        private async Task ConnectAsync()
        {
            await Dispatcher.InvokeAsync(() => UpdateUiState(ConnectionState.Connecting));

            EnsurePlcManager();
            SubscribeToPlcManager();

            var success = await TryConnectWithRetriesAsync();
            await Dispatcher.InvokeAsync(() => UpdateUiState(success ? ConnectionState.Connected : ConnectionState.Failed));
        }

        private void EnsurePlcManager()
        {
            if (_plcManager != null)
            {
                return;
            }

            IPlcClient client = new FX3UPlcClient(null);
            _plcManager = new PlcManager(client, null);
        }

        private async Task<bool> TryConnectWithRetriesAsync()
        {
            var success = false;
            var attempt = 0;

            while (!success && attempt <= MaxRetryCount)
            {
                attempt++;
                try
                {
                    success = await ExecuteConnectAttemptAsync(attempt);
                }
                catch (Exception ex)
                {
                    await HandleConnectExceptionAsync(ex, attempt);
                }
            }

            return success;
        }

        private async Task<bool> ExecuteConnectAttemptAsync(int attempt)
        {
            string retryMsg = attempt > 1 ? $" (Attempt {attempt}/{MaxRetryCount})" : "";
            ComplianceContext.LogSystem($"Connecting to PLC ({IpAddress}:{Port}){retryMsg}...", LogLevel.Info);

            if (attempt > 1)
            {
                await Dispatcher.InvokeAsync(() => StatusText.Text = $"RETRYING ({attempt}/{MaxRetryCount})...");
            }

            var connected = await _plcManager!.InitializeAsync(IpAddress, Port, ScanInterval);
            if (connected)
            {
                await HandleConnectSuccessAsync();
                return true;
            }

            await HandleConnectFailureAsync(attempt);
            return false;
        }

        private async Task HandleConnectSuccessAsync()
        {
            System.Diagnostics.Debug.WriteLine("[PlcStatus] Connection successful!");

            await Dispatcher.InvokeAsync(() =>
            {
                UpdateUiState(ConnectionState.Connected);
                StatusText.Text = "CONNECTED";
            });

            ComplianceContext.LogSystem($"PLC Connection Established ({IpAddress})", LogLevel.Success);
            _registeredMonitorKeys.Clear();
            RegisterAllMonitors();

            ConnectionEstablished?.Invoke(_plcManager!);
            ScanUpdated?.Invoke(_plcManager!);
            StartConnectionWatchdog();
        }

        private async Task HandleConnectFailureAsync(int attempt)
        {
            if (attempt <= MaxRetryCount)
            {
                ComplianceContext.LogSystem($"Connection failed. Retrying in 2s... ({attempt}/{MaxRetryCount})", LogLevel.Warning);
                await Task.Delay(RetryDelayMs);
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                UpdateUiState(ConnectionState.Failed);
                StatusText.Text = "DISCONNECTED";
            });
            ComplianceContext.LogSystem($"PLC Connection Failed after {MaxRetryCount} attempts.", LogLevel.Error);
        }

        private async Task HandleConnectExceptionAsync(Exception ex, int attempt)
        {
            ComplianceContext.LogSystem($"PLC Error: {ex.Message}", LogLevel.Error);
            if (attempt <= MaxRetryCount)
            {
                await Task.Delay(RetryDelayMs);
            }
        }

        /// <summary>
        /// 🔥 註冊所有監控位址（統一方法）
        /// </summary>
        private void RegisterAllMonitors()
        {
            if (_plcManager?.Monitor == null) return;

            // 🔥 1. 註冊預設監控位址 D0，確保 Monitor 有東西可以輪詢
            RegisterMonitorAddress("D0", 1);

            // 🔥 2. 註冊手動設定的 MonitorAddress（如果有）
            if (!string.IsNullOrWhiteSpace(MonitorAddress))
            {
                RegisterMonitors(MonitorAddress);
            }

            RegisterAutoMonitorAddresses();

            System.Diagnostics.Debug.WriteLine("[PlcStatus] All monitors registered");
        }

        /// <summary>
        /// 🔥 公開方法：重新註冊監控位址（當新的 PlcLabel 載入時呼叫）
        /// </summary>
        public void RefreshMonitors()
        {
            if (_plcManager?.Monitor == null || !_plcManager.IsConnected) return;

            // 🔥 如果是全域且已給了整包 MonitorAddress，跳過動態刷新
            if (IsGlobal && !string.IsNullOrWhiteSpace(MonitorAddress))
            {
                System.Diagnostics.Debug.WriteLine("[PlcStatus] Skipping RefreshMonitors due to static global configuration.");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[PlcStatus] RefreshMonitors called");

            // 重新註冊來自 PlcLabelContext 的監控位址
            string labelAddresses = PlcLabelContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(labelAddresses))
            {
                RegisterMonitors(labelAddresses);
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] Refreshed PlcLabel monitors: {labelAddresses}");
            }

            ScanUpdated?.Invoke(_plcManager);
        }

        private async Task DisconnectAsync()
        {
            CancelWatchdog();
            UnsubscribeFromPlcManager();

            if (_plcManager != null) await _plcManager.DisconnectAsync();
            UpdateUiState(ConnectionState.Failed);
            StatusText.Text = "Click to Connect";
            ComplianceContext.LogSystem($"PLC Disconnected by User", LogLevel.Warning);
        }

        private void StartConnectionWatchdog()
        {
            CancelWatchdog();

            _watchdogCts = new CancellationTokenSource();
            var token = _watchdogCts.Token;

            Task.Run(() => WatchdogLoopAsync(token), token);
        }

        private async Task WatchdogLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(WatchdogPollingIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (IsConnectionLost())
                {
                    await HandleConnectionLostAsync();
                    break;
                }
            }
        }

        private bool IsConnectionLost()
        {
            return _plcManager != null && !_plcManager.IsConnected;
        }

        private async Task HandleConnectionLostAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ComplianceContext.LogSystem("Connection lost detected! Attempting to reconnect...", LogLevel.Error);
                CancelWatchdog();
                UpdateUiState(ConnectionState.Failed);
                StatusText.Text = "RECONNECTING...";
                ConnectionLost?.Invoke();
            });

            await ConnectAsync();
        }

        private void CancelWatchdog()
        {
            _watchdogCts?.Cancel();
            _watchdogCts = null;
        }

        private void RegisterMonitors(string config)
        {
            if (_plcManager?.Monitor == null) return;
            if (config.Contains(","))
            {
                var parts = config.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    string current = parts[i].Trim();
                    if (int.TryParse(current, out _)) continue;
                    int length = MonitorLength;
                    if (i + 1 < parts.Length)
                    {
                        string nextToken = parts[i + 1].Trim();
                        if (int.TryParse(nextToken, out int parsedLen)) { length = parsedLen; i++; }
                    }
                    RegisterMonitorAddress(current, length);
                }
            }
            else
            {
                RegisterMonitorAddress(config, MonitorLength);
            }
        }

        private void RegisterAutoMonitorAddresses()
        {
            // 🔥 如果是全域模式且已經手動綁定了大包 MonitorAddress (如 UbiDemo 的做法)
            // 就不需要依賴動態 AutoRegister，因為所有的住址應該都已經預先載入過了
            if (IsGlobal && !string.IsNullOrWhiteSpace(MonitorAddress))
            {
                ComplianceContext.LogSystem($"[PlcStatus] Skipping dynamic AutoMonitor sources because global config is provided.", LogLevel.Info, showInUi: false);
                return;
            }

            foreach (var source in GetAutoMonitorSources())
            {
                if (string.IsNullOrWhiteSpace(source.Addresses))
                {
                    continue;
                }

                RegisterMonitors(source.Addresses);
                ComplianceContext.LogSystem($"[AutoRegister] {source.Name}: {source.Addresses}", LogLevel.Info, showInUi: false);
            }
        }

        private static IEnumerable<(string Name, string Addresses)> GetAutoMonitorSources()
        {
            yield return ("Sensor", SensorContext.GenerateMonitorAddresses());
            yield return ("PlcLabel", PlcLabelContext.GenerateMonitorAddresses());
            yield return ("PlcEvent", PlcEventContext.GenerateMonitorAddresses());
            yield return ("Recipe", RecipeContext.GenerateMonitorAddresses());
        }

        private void RegisterMonitorAddress(string address, int length)
        {
            if (_plcManager?.Monitor == null)
            {
                return;
            }

            var normalizedAddress = (address ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedAddress))
            {
                return;
            }

            var normalizedLength = length <= 0 ? 1 : length;
            var registrationKey = $"{normalizedAddress}:{normalizedLength}";
            if (!_registeredMonitorKeys.Add(registrationKey))
            {
                return;
            }

            try
            {
                _plcManager.Monitor.Register(normalizedAddress, normalizedLength);
            }
            catch (Exception ex)
            {
                _registeredMonitorKeys.Remove(registrationKey);
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] Register monitor failed: {normalizedAddress}, len={normalizedLength}, error={ex.Message}");
            }
        }

        private void PlcStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Unloaded - this={this.GetHashCode()}, IsGlobal={IsGlobal}");
            
            // 🔥 如果是全局模式且是當前全局實例，不要清理，保持連線
            if (ShouldKeepGlobalConnection())
            {
                // 🔥 不取消事件訂閱，不 Dispose，保持連線
                // 下一個 PlcStatus 實例會接管這個連線
                ComplianceContext.LogSystem("[PlcStatus] Global instance unloaded, keeping connection for next instance", LogLevel.Info, showInUi: false);
                return;
            }
            
            // 非全局模式才 Dispose
            Dispose();
        }
        
        public void Dispose()
        {
            if (!ShouldKeepGlobalConnection())
            {
                CancelWatchdog();
                UnsubscribeFromPlcManager();
                
                if (_plcManager != null) 
                { 
                    _plcManager.Dispose(); 
                    _plcManager = null; 
                }
            }
        }

        private bool ShouldKeepGlobalConnection()
        {
            return IsGlobal && PlcContext.GlobalStatus == this;
        }

        private enum ConnectionState { Connecting, Connected, Failed }
        private void UpdateUiState(ConnectionState state)
        {
            // 🔥 確保在 UI 執行緒上執行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateUiState(state));
                return;
            }
            
            switch (state)
            {
                case ConnectionState.Connecting: 
                    StatusLight.Fill = s_orangeBrush; 
                    StatusLight.Effect = null; 
                    StatusText.Text = "CONNECTING..."; 
                    StatusText.Foreground = s_grayBrush; 
                    break;
                case ConnectionState.Connected: 
                    StatusLight.Fill = s_limeGreenBrush; 
                    StatusLight.Effect = s_greenGlow; 
                    StatusText.Text = "CONNECTED"; 
                    StatusText.Foreground = s_limeGreenBrush; 
                    break;
                case ConnectionState.Failed: 
                    StatusLight.Fill = s_redBrush; 
                    StatusLight.Effect = s_redGlow; 
                    StatusText.Foreground = s_redBrush; 
                    break;
            }
        }
    }
}
