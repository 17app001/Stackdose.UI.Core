using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
using Stackdose.Hardware.Plc;
using Stackdose.Mitsubishi.Plc;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcStatus : UserControl, IDisposable
    {
        private IPlcManager? _plcManager;
        private bool _isBusy = false;

        // 🔥 用來控制看門狗是否應該繼續運行
        private CancellationTokenSource? _watchdogCts;
        
        // 🔥 追蹤是否已訂閱事件
        private bool _isEventSubscribed = false;

        public IPlcManager? CurrentManager => _plcManager;
        
        // 🔥 當 PLC 連線成功時觸發的事件
        public event Action<IPlcManager>? ConnectionEstablished;
        
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
            DependencyProperty.Register("MonitorAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata(null));
        public string MonitorAddress { get { return (string)GetValue(MonitorAddressProperty); } set { SetValue(MonitorAddressProperty, value); } }

        public static readonly DependencyProperty MonitorLengthProperty =
            DependencyProperty.Register("MonitorLength", typeof(int), typeof(PlcStatus), new PropertyMetadata(1));
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

        #endregion

        private async void PlcStatus_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Loaded - IsGlobal={IsGlobal}, AutoConnect={AutoConnect}, this={this.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Current GlobalStatus={PlcContext.GlobalStatus?.GetHashCode()}, IsConnected={PlcContext.GlobalStatus?.CurrentManager?.IsConnected}");

            if (IsGlobal)
            {
                var existingGlobal = PlcContext.GlobalStatus;
                
                // 🔥 情況1: 已有全局實例且已連線，且不是自己
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
                Dispatcher.BeginInvoke(() =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        // 🔥 scanInfo 已經是完整的字串（例如 "PLC Poll 耗時: 15 ms"），不需要再加 "ms"
                        StatusText.Text = $"ONLINE ({scanInfo})";
                        
                        // 🔥 確保 UI 狀態是連線狀態
                        StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen);
                        StatusLight.Effect = new DropShadowEffect { Color = Colors.LimeGreen, BlurRadius = 15, ShadowDepth = 0 };
                        StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
                    }
                });
                
                // 觸發 ScanUpdated 事件
                ScanUpdated?.Invoke(_plcManager!);
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
            // 🔥 確保在 UI 執行緒上更新
            await Dispatcher.InvokeAsync(() => UpdateUiState(ConnectionState.Connecting));

            if (_plcManager == null)
            {
                IPlcClient client = new FX3UPlcClient(null);
                _plcManager = new PlcManager(client, null);
            }
            
            // 🔥 確保訂閱事件
            SubscribeToPlcManager();

            bool success = false;
            int attempt = 0;

            while (!success && attempt <= MaxRetryCount)
            {
                attempt++;
                try
                {
                    string retryMsg = attempt > 1 ? $" (Attempt {attempt}/{MaxRetryCount})" : "";
                    ComplianceContext.LogSystem($"Connecting to PLC ({IpAddress}:{Port}){retryMsg}...", LogLevel.Info);

                    if (attempt > 1)
                    {
                        await Dispatcher.InvokeAsync(() => StatusText.Text = $"RETRYING ({attempt}/{MaxRetryCount})...");
                    }

                    success = await _plcManager.InitializeAsync(IpAddress, Port, ScanInterval);

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("[PlcStatus] Connection successful!");
                        
                        // 🔥 在 UI 執行緒上更新狀態
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UpdateUiState(ConnectionState.Connected);
                            StatusText.Text = "CONNECTED";
                        });
                        
                        ComplianceContext.LogSystem($"PLC Connection Established ({IpAddress})", LogLevel.Success);

                        // 🔥 註冊所有監控位址
                        RegisterAllMonitors();

                        ConnectionEstablished?.Invoke(_plcManager);
                        StartConnectionWatchdog();
                    }
                    else
                    {
                        if (attempt <= MaxRetryCount)
                        {
                            ComplianceContext.LogSystem($"Connection failed. Retrying in 2s... ({attempt}/{MaxRetryCount})", LogLevel.Warning);
                            await Task.Delay(2000);
                        }
                        else
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                UpdateUiState(ConnectionState.Failed);
                                StatusText.Text = "DISCONNECTED";
                            });
                            ComplianceContext.LogSystem($"PLC Connection Failed after {MaxRetryCount} attempts.", LogLevel.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ComplianceContext.LogSystem($"PLC Error: {ex.Message}", LogLevel.Error);
                    if (attempt <= MaxRetryCount) await Task.Delay(2000);
                }
            }

            // 🔥 最終狀態更新
            await Dispatcher.InvokeAsync(() => UpdateUiState(success ? ConnectionState.Connected : ConnectionState.Failed));
        }

        /// <summary>
        /// 🔥 註冊所有監控位址（統一方法）
        /// </summary>
        private void RegisterAllMonitors()
        {
            if (_plcManager?.Monitor == null) return;

            // 🔥 1. 註冊預設監控位址 D0，確保 Monitor 有東西可以輪詢
            try { _plcManager.Monitor.Register("D0", 1); } catch { }

            // 🔥 2. 註冊手動設定的 MonitorAddress（如果有）
            if (!string.IsNullOrWhiteSpace(MonitorAddress))
            {
                RegisterMonitors(MonitorAddress);
            }

            // 🔥 3. 自動註冊來自 SensorContext 的監控位址
            string sensorAddresses = SensorContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(sensorAddresses))
            {
                RegisterMonitors(sensorAddresses);
                ComplianceContext.LogSystem($"[AutoRegister] Sensor: {sensorAddresses}", LogLevel.Info, showInUi: false);
            }

            // 🔥 4. 自動註冊來自 PlcLabelContext 的監控位址
            string labelAddresses = PlcLabelContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(labelAddresses))
            {
                RegisterMonitors(labelAddresses);
                ComplianceContext.LogSystem($"[AutoRegister] PlcLabel: {labelAddresses}", LogLevel.Info, showInUi: false);
            }

            // 🔥 5. 自動註冊來自 PlcEventContext 的監控位址
            string eventAddresses = PlcEventContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(eventAddresses))
            {
                RegisterMonitors(eventAddresses);
                ComplianceContext.LogSystem($"[AutoRegister] PlcEvent: {eventAddresses}", LogLevel.Info, showInUi: false);
            }

            // 🔥 6. 自動註冊來自 RecipeContext 的監控位址
            string recipeAddresses = RecipeContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(recipeAddresses))
            {
                RegisterMonitors(recipeAddresses);
                ComplianceContext.LogSystem($"[AutoRegister] Recipe: {recipeAddresses}", LogLevel.Info, showInUi: false);
            }

            System.Diagnostics.Debug.WriteLine("[PlcStatus] All monitors registered");
        }

        /// <summary>
        /// 🔥 公開方法：重新註冊監控位址（當新的 PlcLabel 載入時呼叫）
        /// </summary>
        public void RefreshMonitors()
        {
            if (_plcManager?.Monitor == null || !_plcManager.IsConnected) return;

            System.Diagnostics.Debug.WriteLine("[PlcStatus] RefreshMonitors called");

            // 重新註冊來自 PlcLabelContext 的監控位址
            string labelAddresses = PlcLabelContext.GenerateMonitorAddresses();
            if (!string.IsNullOrWhiteSpace(labelAddresses))
            {
                RegisterMonitors(labelAddresses);
                System.Diagnostics.Debug.WriteLine($"[PlcStatus] Refreshed PlcLabel monitors: {labelAddresses}");
            }
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

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(3000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (_plcManager != null && !_plcManager.IsConnected)
                    {
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            ComplianceContext.LogSystem("Connection lost detected! Attempting to reconnect...", LogLevel.Error);
                            CancelWatchdog();
                            await ConnectAsync();
                        });
                        break;
                    }
                }
            }, token);
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
                    try { _plcManager.Monitor.Register(current, length); } catch { }
                }
            }
            else
            {
                try { _plcManager.Monitor.Register(config, MonitorLength); } catch { }
            }
        }

        private void PlcStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PlcStatus] Unloaded - this={this.GetHashCode()}, IsGlobal={IsGlobal}");
            
            // 🔥 如果是全局模式且是當前全局實例，不要清理，保持連線
            if (IsGlobal && PlcContext.GlobalStatus == this)
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
            if (!IsGlobal || PlcContext.GlobalStatus != this)
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
                    StatusLight.Fill = new SolidColorBrush(Colors.Orange); 
                    StatusLight.Effect = null; 
                    StatusText.Text = "CONNECTING..."; 
                    StatusText.Foreground = new SolidColorBrush(Colors.Gray); 
                    break;
                case ConnectionState.Connected: 
                    StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen); 
                    StatusLight.Effect = new DropShadowEffect { Color = Colors.LimeGreen, BlurRadius = 15, ShadowDepth = 0 }; 
                    StatusText.Text = "CONNECTED"; 
                    StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen); 
                    break;
                case ConnectionState.Failed: 
                    StatusLight.Fill = new SolidColorBrush(Colors.Red); 
                    StatusLight.Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0 }; 
                    StatusText.Foreground = new SolidColorBrush(Colors.Red); 
                    break;
            }
        }
    }
}