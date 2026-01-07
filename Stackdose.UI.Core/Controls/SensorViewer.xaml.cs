using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Stackdose.UI.Core.Controls
{
    public partial class SensorViewer : UserControl
    {
        private DispatcherTimer? _monitorTimer;
        private CancellationTokenSource? _cancellationTokenSource;
        
        /// <summary>
        /// 🔥 追蹤是否已初始化 Sensor 狀態（避免重複初始化）
        /// </summary>
        private bool _isInitialized = false;

        public SensorViewer()
        {
            InitializeComponent();
            Loaded += SensorViewer_Loaded;
            Unloaded += SensorViewer_Unloaded;
        }

        #region Dependency Properties

        /// <summary>
        /// 標題文字 (顯示在控制項頂部)
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SensorViewer), 
                new PropertyMetadata("Sensor Status"));
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// JSON 配置檔案路徑 (例如：Sensors.json，會自動從 Resources 目錄載入)
        /// </summary>
        public static readonly DependencyProperty ConfigFileProperty =
            DependencyProperty.Register("ConfigFile", typeof(string), typeof(SensorViewer), 
                new PropertyMetadata("Sensors.json", OnConfigFileChanged));
        public string ConfigFile
        {
            get { return (string)GetValue(ConfigFileProperty); }
            set { SetValue(ConfigFileProperty, value); }
        }

        /// <summary>
        /// 是否啟用分組顯示
        /// </summary>
        public static readonly DependencyProperty EnableGroupingProperty =
            DependencyProperty.Register("EnableGrouping", typeof(bool), typeof(SensorViewer), 
                new PropertyMetadata(true, OnEnableGroupingChanged));
        public bool EnableGrouping
        {
            get { return (bool)GetValue(EnableGroupingProperty); }
            set { SetValue(EnableGroupingProperty, value); }
        }

        /// <summary>
        /// 自動刷新間隔 (毫秒，預設 1000ms)
        /// </summary>
        public static readonly DependencyProperty RefreshIntervalProperty =
            DependencyProperty.Register("RefreshInterval", typeof(int), typeof(SensorViewer), 
                new PropertyMetadata(1000));
        public int RefreshInterval
        {
            get { return (int)GetValue(RefreshIntervalProperty); }
            set { SetValue(RefreshIntervalProperty, value); }
        }

        /// <summary>
        /// 是否自動啟動監控 (預設 true)
        /// </summary>
        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.Register("AutoStart", typeof(bool), typeof(SensorViewer), 
                new PropertyMetadata(true));
        public bool AutoStart
        {
            get { return (bool)GetValue(AutoStartProperty); }
            set { SetValue(AutoStartProperty, value); }
        }

        #endregion

        #region 事件處理

        private void SensorViewer_Loaded(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SensorViewer] Loaded called. IsInitialized={_isInitialized}, MonitorRunning={_monitorTimer != null}");
            #endif
            
            // 🔥 如果已經初始化過，只需要重新啟動監控（不重新初始化）
            if (_isInitialized)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[SensorViewer] Already initialized, restarting monitoring only.");
                #endif
                
                // 🔥 重新啟動監控（如果已經連線且監控未運行）
                if (AutoStart && 
                    PlcContext.GlobalStatus?.CurrentManager?.IsConnected == true && 
                    _monitorTimer == null)
                {
                    StartMonitoring();
                }
                return;
            }
            
            // 設定為已初始化
            _isInitialized = true;
            
            // 載入配置檔案
            if (!string.IsNullOrEmpty(ConfigFile))
            {
                SensorContext.LoadFromJson(ConfigFile);
            }

            // 🔥 只在第一次載入時訂閱 PlcStatus 事件
            if (PlcContext.GlobalStatus != null)
            {
                // 移除舊的訂閱（如果存在）
                PlcContext.GlobalStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
                
                // 訂閱連線成功事件
                PlcContext.GlobalStatus.ConnectionEstablished += OnPlcConnectionEstablished;

                // 🔥 如果 PlcStatus 已經連線完成，立即執行註冊
                if (PlcContext.GlobalStatus.CurrentManager != null && 
                    PlcContext.GlobalStatus.CurrentManager.IsConnected &&
                    !SensorContext.IsMonitorRegistered)
                {
                    OnPlcConnectionEstablished(PlcContext.GlobalStatus.CurrentManager);
                }
            }

            // 綁定資料源
            BindSensorList();

            // 🔥 自動啟動監控（只在第一次且已連線時）
            if (AutoStart && PlcContext.GlobalStatus?.CurrentManager?.IsConnected == true)
            {
                InitializeSensorStates(PlcContext.GlobalStatus.CurrentManager);
                StartMonitoring();
            }
        }

        private void SensorViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            // 🔥 Tab 切換時停止監控（節省資源）
            StopMonitoring();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[SensorViewer] Unloaded, monitoring stopped (will restart on next Loaded)");
            #endif
        }

        private static void OnConfigFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (SensorViewer)d;
            if (viewer.IsLoaded && !string.IsNullOrEmpty(e.NewValue as string))
            {
                SensorContext.LoadFromJson((string)e.NewValue!);
                viewer.BindSensorList();
            }
        }

        private static void OnEnableGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (SensorViewer)d;
            viewer.BindSensorList();
        }

        #endregion

        #region 監控邏輯

        /// <summary>
        /// 開始監控感測器狀態
        /// </summary>
        public void StartMonitoring()
        {
            if (_monitorTimer != null) return; // 已啟動

            _cancellationTokenSource = new CancellationTokenSource();
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RefreshInterval)
            };
            _monitorTimer.Tick += async (s, e) => await MonitorSensors();
            _monitorTimer.Start();
        }

        /// <summary>
        /// 停止監控
        /// </summary>
        public void StopMonitoring()
        {
            _monitorTimer?.Stop();
            _monitorTimer = null;
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 監控所有感測器 (每次 Tick 執行)
        /// </summary>
        private async Task MonitorSensors()
        {
            // 取得 PLC Manager
            var status = PlcContext.GlobalStatus;
            var manager = status?.CurrentManager;

            if (manager == null || !manager.IsConnected)
            {
                return; // PLC 未連線，跳過本次監控
            }

            bool anyStateChanged = false; // 🔥 新增：追蹤是否有狀態改變

            // 逐個檢查感測器
            foreach (var sensor in SensorContext.Sensors)
            {
                try
                {
                    bool isActive = await EvaluateSensor(sensor, manager);
                    string currentValue = sensor.CurrentValue;

                    // 🔥 優化：只有狀態真正改變時才標記
                    bool oldState = sensor.IsActive;
                    
                    // 更新狀態 (會自動觸發警報事件)
                    SensorContext.UpdateSensorState(sensor, isActive, currentValue);

                    if (oldState != isActive)
                    {
                        anyStateChanged = true;
                    }
                }
                catch (Exception ex)
                {
                    // 讀取失敗，記錄錯誤但不中斷監控
                    System.Diagnostics.Debug.WriteLine($"[SensorViewer] Failed to monitor {sensor.Device}: {ex.Message}");
                }
            }

            // 更新統計資訊
            UpdateStatistics();

            // 🔥 優化：只在狀態改變或勾選 Checkbox 時才重新篩選
            if (anyStateChanged || (ChkShowAlarmsOnly != null && ChkShowAlarmsOnly.IsChecked == true))
            {
                Dispatcher.Invoke(() =>
                {
                    var view = CollectionViewSource.GetDefaultView(SensorList.ItemsSource);
                    view?.Refresh();
                });
            }
        }

        /// <summary>
        /// 評估單一感測器是否觸發
        /// </summary>
        private async Task<bool> EvaluateSensor(SensorConfig sensor, IPlcManager manager)
        {
            string device = sensor.Device.Trim().ToUpper();
            string bitStr = sensor.Bit.Trim();
            string valueStr = sensor.Value.Trim();
            string mode = sensor.Mode.Trim().ToUpper();

            // 🔥 優先使用 Monitor 快取資料（避免直接打 PLC）
            var monitor = manager.Monitor;
            bool useMonitor = monitor != null && monitor.IsRunning;

            // === 模式 1: 多 Bit 邏輯運算 (AND / OR) ===
            if (!string.IsNullOrEmpty(bitStr) && bitStr.Contains(','))
            {
                string[] bits = bitStr.Split(',');
                string[] expectedValues = valueStr.Split(',');

                if (bits.Length != expectedValues.Length)
                {
                    return false; // 配置錯誤
                }

                bool[] results = new bool[bits.Length];

                for (int i = 0; i < bits.Length; i++)
                {
                    int bitIndex = int.Parse(bits[i].Trim());
                    int expectedValue = int.Parse(expectedValues[i].Trim());

                    // 🔥 優先從 Monitor 讀取
                    int bitValue;
                    if (useMonitor)
                    {
                        bool? cachedBit = monitor!.GetBit(device, bitIndex);
                        bitValue = cachedBit.HasValue ? (cachedBit.Value ? 1 : 0) : 0;
                    }
                    else
                    {
                        // Fallback: 直接讀取 PLC
                        int wordValue = await manager.ReadAsync(device);
                        bitValue = (wordValue >> bitIndex) & 1;
                    }

                    results[i] = (bitValue == expectedValue);
                }

                // 儲存當前值 (用於顯示)
                sensor.CurrentValue = string.Join(",", results.Select(r => r ? "1" : "0"));

                // 根據 Mode 計算結果
                if (mode == "AND")
                    return results.All(r => r);
                else if (mode == "OR")
                    return results.Any(r => r);
                else
                    return false;
            }
            // === 模式 2: 單一 Bit ===
            else if (!string.IsNullOrEmpty(bitStr))
            {
                int bitIndex = int.Parse(bitStr);
                int expectedValue = int.Parse(valueStr);

                // 🔥 優先從 Monitor 讀取
                int bitValue;
                if (useMonitor)
                {
                    bool? cachedBit = monitor!.GetBit(device, bitIndex);
                    bitValue = cachedBit.HasValue ? (cachedBit.Value ? 1 : 0) : 0;
                }
                else
                {
                    int wordValue = await manager.ReadAsync(device);
                    bitValue = (wordValue >> bitIndex) & 1;
                }

                sensor.CurrentValue = bitValue.ToString();
                return (bitValue == expectedValue);
            }
            // === 模式 3: 數值比較 (COMPARE 模式) ===
            else if (mode == "COMPARE")
            {
                // 🔥 優先從 Monitor 讀取
                int currentValue;
                if (useMonitor)
                {
                    short? cachedWord = monitor!.GetWord(device);
                    currentValue = cachedWord ?? 0;
                }
                else
                {
                    currentValue = await manager.ReadAsync(device);
                }

                sensor.CurrentValue = currentValue.ToString();

                // 解析比較運算子 (例如 >75, <50, ==100)
                var match = Regex.Match(valueStr, @"^([><=!]+)(\d+)$");
                if (match.Success)
                {
                    string op = match.Groups[1].Value;
                    int threshold = int.Parse(match.Groups[2].Value);

                    return op switch
                    {
                        ">" => currentValue > threshold,
                        "<" => currentValue < threshold,
                        ">=" => currentValue >= threshold,
                        "<=" => currentValue <= threshold,
                        "==" => currentValue == threshold,
                        "!=" => currentValue != threshold,
                        _ => false
                    };
                }
            }

            return false; // 無法判斷，預設為 false
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 綁定感測器清單到 UI
        /// </summary>
        private void BindSensorList()
        {
            if (EnableGrouping)
            {
                // 分組顯示
                var view = CollectionViewSource.GetDefaultView(SensorContext.Sensors);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                
                // 🔥 新增：套用篩選器
                view.Filter = ApplyFilter;
                
                SensorList.ItemsSource = view;
            }
            else
            {
                // 平面顯示
                var view = CollectionViewSource.GetDefaultView(SensorContext.Sensors);
                
                // 🔥 新增：套用篩選器
                view.Filter = ApplyFilter;
                
                SensorList.ItemsSource = view;
            }

            // 更新統計
            UpdateStatistics();

            // 控制無資料提示
            NoDataHint.Visibility = SensorContext.Sensors.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        /// <summary>
        /// 🔥 新增：篩選器邏輯（根據 Checkbox 狀態決定是否顯示項目）
        /// </summary>
        private bool ApplyFilter(object item)
        {
            if (item is not SensorConfig sensor)
                return true;

            // 如果 Checkbox 未勾選，顯示所有項目
            if (ChkShowAlarmsOnly == null || ChkShowAlarmsOnly.IsChecked != true)
                return true;

            // 如果 Checkbox 勾選，只顯示異常項目 (IsActive = true)
            return sensor.IsActive;
        }

        /// <summary>
        /// 🔥 新增：Checkbox 狀態改變時重新篩選
        /// </summary>
        private void ChkShowAlarmsOnly_Changed(object sender, RoutedEventArgs e)
        {
            // 重新套用篩選器
            var view = CollectionViewSource.GetDefaultView(SensorList.ItemsSource);
            view?.Refresh();
        }

        /// <summary>
        /// 更新統計資訊 (異常數量 / 總數)
        /// </summary>
        private void UpdateStatistics()
        {
            int alarmCount = SensorContext.Sensors.Count(s => s.IsActive);
            int totalCount = SensorContext.Sensors.Count;

            TxtAlarmCount.Text = alarmCount.ToString();
            TxtTotalCount.Text = totalCount.ToString();
        }

        /// <summary>
        /// 🔥 新增：訂閱 PlcStatus 的連線成功事件
        /// </summary>
        private void SubscribeToPlcStatusEvents()
        {
            var status = PlcContext.GlobalStatus;
            if (status == null)
            {
                ComplianceContext.LogSystem("[SensorViewer] PlcStatus not found in PlcContext.", Models.LogLevel.Warning, showInUi: false);
                return;
            }

            // 訂閱連線成功事件
            status.ConnectionEstablished += OnPlcConnectionEstablished;

            // 🔥 如果 PlcStatus 已經連線完成（在 SensorViewer 載入之前），立即註冊
            if (status.CurrentManager != null && status.CurrentManager.IsConnected)
            {
                OnPlcConnectionEstablished(status.CurrentManager);
            }
        }

        /// <summary>
        /// 🔥 新增：當 PLC 連線成功時的回呼
        /// </summary>
        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SensorViewer] OnPlcConnectionEstablished called. IsInitialized={_isInitialized}, IsMonitorRegistered={SensorContext.IsMonitorRegistered}");
                #endif
                
                // 🔥 如果已經初始化過，不重複執行
                if (_isInitialized)
                {
                    ComplianceContext.LogSystem("[SensorViewer] Already initialized, skipping OnPlcConnectionEstablished logic.", Models.LogLevel.Info, showInUi: false);
                    return;
                }
                
                // 🔥 如果已經註冊過，不重複註冊
                if (SensorContext.IsMonitorRegistered)
                {
                    ComplianceContext.LogSystem("[SensorViewer] Monitor addresses already registered, skipping.", Models.LogLevel.Info, showInUi: false);
                    return;
                }

                if (manager.Monitor == null)
                {
                    ComplianceContext.LogSystem("[SensorViewer] Monitor not available.", Models.LogLevel.Warning, showInUi: false);
                    return;
                }

                // 從 SensorContext 智慧提取監控位址
                string monitorAddresses = SensorContext.GenerateMonitorAddresses();

                if (string.IsNullOrEmpty(monitorAddresses))
                {
                    ComplianceContext.LogSystem("[SensorViewer] No monitor addresses generated.", Models.LogLevel.Warning, showInUi: false);
                    return;
                }

                ComplianceContext.LogSystem($"[SensorViewer] Monitor addresses prepared: {monitorAddresses}", Models.LogLevel.Info, showInUi: false);
                
                // 🔥 註冊監控位址（SensorContext.GenerateMonitorAddresses 會設定 IsMonitorRegistered）
                // 由 PlcStatus 的 ConnectAsync 自動呼叫 RegisterMonitors
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem($"[SensorViewer] Failed to prepare monitors: {ex.Message}", Models.LogLevel.Error, showInUi: true);
            }
        }

        /// <summary>
        /// 🔥 新增：初始化 Sensor 狀態（靜默讀取，不觸發警報）
        /// </summary>
        private async void InitializeSensorStates(IPlcManager manager)
        {
            if (manager == null || !manager.IsConnected)
            {
                ComplianceContext.LogSystem("[SensorViewer] Cannot initialize sensor states: PLC not connected.", Models.LogLevel.Warning, showInUi: false);
                return;
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[SensorViewer] InitializeSensorStates starting...");
            #endif

            ComplianceContext.LogSystem("[SensorViewer] Initializing sensor states (silent)...", Models.LogLevel.Info, showInUi: false);

            var alarmSensors = new List<SensorConfig>(); // 🔥 收集異常 Sensor

            foreach (var sensor in SensorContext.Sensors)
            {
                try
                {
                    bool isActive = await EvaluateSensor(sensor, manager);
                    string currentValue = sensor.CurrentValue;

                    // 🔥 直接設定初始狀態，不呼叫 UpdateSensorState（避免觸發警報事件）
                    sensor.IsActive = isActive;
                    sensor.CurrentValue = currentValue;
                    
                    // 🔥 如果初始狀態就是觸發（異常），記錄並收集
                    if (isActive)
                    {
                        alarmSensors.Add(sensor);
                        
                        ComplianceContext.LogSystem(
                            $"[Sensor] 初始狀態異常: {sensor.OperationDescription} ({sensor.Device}) = {currentValue}",
                            Models.LogLevel.Warning,
                            showInUi: true
                        );
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SensorViewer] Failed to initialize {sensor.Device}: {ex.Message}");
                }
            }

            // 更新統計
            UpdateStatistics();

            ComplianceContext.LogSystem($"[SensorViewer] Sensor states initialized. Total alarms: {alarmSensors.Count}", Models.LogLevel.Success, showInUi: false);

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SensorViewer] InitializeSensorStates completed.");
            #endif

            // 🔥 MessageBox 警告已移除，僅保留 SensorView 顯示及 Log 記錄
            // 使用者可透過 SensorView 介面查看所有感測器狀態
        }

        #endregion
    }
}
