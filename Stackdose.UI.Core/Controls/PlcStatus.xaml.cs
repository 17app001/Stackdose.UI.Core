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

        // 🔥 新增：用來控制看門狗是否應該繼續運行
        private CancellationTokenSource? _watchdogCts;

        public IPlcManager? CurrentManager => _plcManager;
        
        // 🔥 新增：當 PLC 連線成功時觸發的事件
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

        // ... (原有的屬性保持不變: IpAddress, Port, AutoConnect, etc.) ...
        public static readonly DependencyProperty IpAddressProperty =
             DependencyProperty.Register("IpAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata("127.0.0.1"));
        public string IpAddress { get { return (string)GetValue(IpAddressProperty); } set { SetValue(IpAddressProperty, value); } }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(int), typeof(PlcStatus), new PropertyMetadata(502));
        public int Port { get { return (int)GetValue(PortProperty); } set { SetValue(PortProperty, value); } }

        public static readonly DependencyProperty AutoConnectProperty =
            DependencyProperty.Register("AutoConnect", typeof(bool), typeof(PlcStatus), new PropertyMetadata(true));
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

        // 🔥 新增：重試次數設定 (預設 3 次)
        public static readonly DependencyProperty MaxRetryCountProperty =
            DependencyProperty.Register("MaxRetryCount", typeof(int), typeof(PlcStatus), new PropertyMetadata(3));
        public int MaxRetryCount { get { return (int)GetValue(MaxRetryCountProperty); } set { SetValue(MaxRetryCountProperty, value); } }

        // 新增：控制是否顯示邊框 (預設 true)
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
            if (IsGlobal)
            {
                PlcContext.GlobalStatus = this;
                ComplianceContext.LogSystem("System initialized. Main PLC set.", LogLevel.Info);
            }

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            //IpDisplay.Text = $"{IpAddress}:{Port}";

            // 🔥 改為非同步背景連線，不阻塞 UI
            if (AutoConnect)
            {
                // 不要使用 await，讓連線在背景執行
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PlcStatus] Background connection failed: {ex.Message}");
                    }
                });
            }
            else
            {
                UpdateUiState(ConnectionState.Failed);
                StatusText.Text = "Click To Connecting";
            }
        }

        private async void PlcStatus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => await ToggleConnectionAsync();

        private async Task ToggleConnectionAsync()
        {
            if (_isBusy) return;
            try
            {
                _isBusy = true;
                // 手動點擊時，如果已經連線，則斷線 (並且停止自動重連的看門狗)
                if (_plcManager != null && _plcManager.IsConnected)
                {
                    CancelWatchdog(); // 停止監控
                    await DisconnectAsync();
                }
                else
                {
                    await ConnectAsync();
                }
            }
            finally { _isBusy = false; }
        }

        /// <summary>
        /// 🔥 修改後的連線邏輯：支援 3 次重試 + 啟動斷線偵測
        /// </summary>
        private async Task ConnectAsync()
        {
            UpdateUiState(ConnectionState.Connecting);

            // 初始化 PLC Manager (若尚未建立)
            if (_plcManager == null)
            {
                IPlcClient client = new FX3UPlcClient(null);
                _plcManager = new PlcManager(client, null);
                _plcManager.ScanElapsedChanged += (ms) =>
                {
                    try
                    {
                        if (Dispatcher.HasShutdownStarted) return;
                        Dispatcher.Invoke(() => { if (!Dispatcher.HasShutdownStarted) StatusText.Text = $"ONLINE ({ms}ms)"; });
                        ScanUpdated?.Invoke(_plcManager);
                    }
                    catch { }
                };
            }

            // 🔥 重試迴圈邏輯
            bool success = false;
            int attempt = 0;

            while (!success && attempt <= MaxRetryCount)
            {
                attempt++;
                try
                {
                    string retryMsg = attempt > 1 ? $" (Attempt {attempt}/{MaxRetryCount})" : "";
                    ComplianceContext.LogSystem($"Connecting to PLC ({IpAddress}:{Port}){retryMsg}...", LogLevel.Info);

                    if (attempt > 1) StatusText.Text = $"RETRYING ({attempt}/{MaxRetryCount})...";

                    // 嘗試連線
                    success = await _plcManager.InitializeAsync(IpAddress, Port, ScanInterval);

                    if (success)
                    {
                        StatusText.Text = "CONNECTED";
                        ComplianceContext.LogSystem($"PLC Connection Established ({IpAddress})", LogLevel.Success);

                        // 🔥 1. 先註冊手動設定的 MonitorAddress（如果有）
                        if (!string.IsNullOrWhiteSpace(MonitorAddress)) 
                            RegisterMonitors(MonitorAddress);

                        // 🔥 2. 自動註冊來自 SensorContext 的監控位址
                        string sensorAddresses = SensorContext.GenerateMonitorAddresses();
                        if (!string.IsNullOrWhiteSpace(sensorAddresses))
                        {
                            RegisterMonitors(sensorAddresses);
                            ComplianceContext.LogSystem(
                                $"[AutoRegister] Sensor: {sensorAddresses}", 
                                LogLevel.Info,
                                showInUi: false
                            );
                        }

                        // 🔥 3. 自動註冊來自 PlcLabelContext 的監控位址
                        string labelAddresses = PlcLabelContext.GenerateMonitorAddresses();
                        if (!string.IsNullOrWhiteSpace(labelAddresses))
                        {
                            RegisterMonitors(labelAddresses);
                            ComplianceContext.LogSystem(
                                $"[AutoRegister] PlcLabel: {labelAddresses}", 
                                LogLevel.Info,
                                showInUi: false
                            );
                        }

                        // 🔥 4. 自動註冊來自 PlcEventContext 的監控位址
                        string eventAddresses = PlcEventContext.GenerateMonitorAddresses();
                        if (!string.IsNullOrWhiteSpace(eventAddresses))
                        {
                            RegisterMonitors(eventAddresses);
                            ComplianceContext.LogSystem(
                                $"[AutoRegister] PlcEvent: {eventAddresses}", 
                                LogLevel.Info,
                                showInUi: false
                            );
                        }

                        // 🔥 5. 自動註冊來自 RecipeContext 的監控位址
                        string recipeAddresses = RecipeContext.GenerateMonitorAddresses();
                        if (!string.IsNullOrWhiteSpace(recipeAddresses))
                        {
                            RegisterMonitors(recipeAddresses);
                            ComplianceContext.LogSystem(
                                $"[AutoRegister] Recipe: {recipeAddresses}", 
                                LogLevel.Info,
                                showInUi: false
                            );
                        }

                        // 🔥 觸發連線成功事件（讓訂閱者可以執行自訂邏輯，例如下載 Recipe）
                        ComplianceContext.LogSystem(
                            "[PlcStatus] Triggering ConnectionEstablished event...",
                            LogLevel.Info,
                            showInUi: false
                        );
                        
                        ConnectionEstablished?.Invoke(_plcManager);
                        
                        ComplianceContext.LogSystem(
                            $"[PlcStatus] ConnectionEstablished event triggered. Subscriber count: {ConnectionEstablished?.GetInvocationList().Length ?? 0}",
                            LogLevel.Info,
                            showInUi: false
                        );

                        // 🔥 連線成功後，啟動「看門狗」來偵測未來是否斷線
                        StartConnectionWatchdog();
                    }
                    else
                    {
                        // 連線失敗
                        if (attempt <= MaxRetryCount)
                        {
                            ComplianceContext.LogSystem($"Connection failed. Retrying in 2s... ({attempt}/{MaxRetryCount})", LogLevel.Warning);
                            // 等待 2 秒後重試
                            await Task.Delay(2000);
                        }
                        else
                        {
                            // 超過次數，放棄
                            StatusText.Text = "DISCONNECTED";
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

            UpdateUiState(success ? ConnectionState.Connected : ConnectionState.Failed);
        }

        private async Task DisconnectAsync()
        {
            // 斷線時先取消看門狗，避免它以為斷線了又嘗試重連
            CancelWatchdog();

            if (_plcManager != null) await _plcManager.DisconnectAsync();
            UpdateUiState(ConnectionState.Failed);
            StatusText.Text = "Click To Connecting";
            ComplianceContext.LogSystem($"PLC Disconnected by User", LogLevel.Warning);
        }

        // 🔥 新增：斷線偵測看門狗 (Watchdog)
        private void StartConnectionWatchdog()
        {
            // 先清除舊的，確保只有一個在跑
            CancelWatchdog();

            _watchdogCts = new CancellationTokenSource();
            var token = _watchdogCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    // 每 3 秒檢查一次狀態
                    await Task.Delay(3000, token);

                    if (_plcManager != null && !_plcManager.IsConnected)
                    {
                        // 😱 發現斷線了！(且不是使用者手動斷的)

                        // 切回 UI 執行緒處理重連
                        Dispatcher.Invoke(async () =>
                        {
                            ComplianceContext.LogSystem("⚠️ Connection lost detected! Attempting to reconnect...", LogLevel.Error);

                            // 停止這個看門狗迴圈 (ConnectAsync 成功後會再起一個新的)
                            CancelWatchdog();

                            // 觸發重連邏輯 (這裡會再次執行 3 次重試)
                            await ConnectAsync();
                        });

                        break; // 跳出迴圈
                    }
                }
            }, token);
        }

        private void CancelWatchdog()
        {
            _watchdogCts?.Cancel();
            _watchdogCts = null;
        }

        // ... (RegisterMonitors, Dispose, UpdateUiState 保持不變) ...

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

        private void PlcStatus_Unloaded(object sender, RoutedEventArgs e) => Dispose();
        public void Dispose()
        {
            CancelWatchdog(); // 記得釋放
            if (_plcManager != null) { _plcManager.Dispose(); _plcManager = null; }
        }

        private enum ConnectionState { Connecting, Connected, Failed }
        private void UpdateUiState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connecting: StatusLight.Fill = new SolidColorBrush(Colors.Orange); StatusLight.Effect = null; StatusText.Text = "CONNECTING..."; StatusText.Foreground = new SolidColorBrush(Colors.Gray); break;
                case ConnectionState.Connected: StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen); StatusLight.Effect = new DropShadowEffect { Color = Colors.LimeGreen, BlurRadius = 15, ShadowDepth = 0 }; StatusText.Text = "CONNECTED"; StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen); break;
                case ConnectionState.Failed: StatusLight.Fill = new SolidColorBrush(Colors.Red); StatusLight.Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0 }; StatusText.Foreground = new SolidColorBrush(Colors.Red); break;
            }
        }
    }
}