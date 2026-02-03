using Stackdose.Abstractions.Hardware;
using Stackdose.Abstractions.Logging;
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
        /// 🔥 追蹤是否已初始化 Sensor 狀態（避免重複初始化）- 靜態變數，跨頁面保持
        /// </summary>
        private static bool _sensorStatesInitialized = false;
        
        /// <summary>
        /// 🔥 追蹤是否已載入配置檔案
        /// </summary>
        private static bool _configLoaded = false;

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
            System.Diagnostics.Debug.WriteLine($"[SensorViewer] Loaded called. SensorStatesInitialized={_sensorStatesInitialized}, ConfigLoaded={_configLoaded}");
            #endif
            
            // 🔥 載入配置檔案（只載入一次）
            if (!_configLoaded && !string.IsNullOrEmpty(ConfigFile))
            {
                SensorContext.LoadFromJson(ConfigFile);
                _configLoaded = true;
            }

            // 🔥 訂閱 PlcStatus 事件（只需要訂閱一次）
            if (PlcContext.GlobalStatus != null)
            {
                // 移除舊的訂閱（避免重複訂閱）
                PlcContext.GlobalStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
                PlcContext.GlobalStatus.ConnectionEstablished += OnPlcConnectionEstablished;
            }

            // 綁定資料源
            BindSensorList();

            // 🔥 自動啟動監控
            if (AutoStart && PlcContext.GlobalStatus?.CurrentManager?.IsConnected == true)
            {
                // 🔥 只在第一次初始化 Sensor 狀態
                if (!_sensorStatesInitialized)
                {
                    InitializeSensorStates(PlcContext.GlobalStatus.CurrentManager);
                    _sensorStatesInitialized = true;
                }
                
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
                _configLoaded = true;
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

            bool anyStateChanged = false;

            // 逐個檢查感測器
            foreach (var sensor in SensorContext.Sensors)
            {
                try
                {
                    bool isActive = await EvaluateSensor(sensor, manager);
                    string currentValue = sensor.CurrentValue;

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

            var monitor = manager.Monitor;
            bool useMonitor = monitor != null && monitor.IsRunning;

            // === 模式 1: 多 Bit 邏輯運算 (AND / OR) ===
            if (!string.IsNullOrEmpty(bitStr) && bitStr.Contains(','))
            {
                string[] bits = bitStr.Split(',');
                string[] expectedValues = valueStr.Split(',');

                if (bits.Length != expectedValues.Length)
                {
                    return false;
                }

                bool[] results = new bool[bits.Length];

                for (int i = 0; i < bits.Length; i++)
                {
                    int bitIndex = int.Parse(bits[i].Trim());
                    int expectedValue = int.Parse(expectedValues[i].Trim());

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

                    results[i] = (bitValue == expectedValue);
                }

                sensor.CurrentValue = string.Join(",", results.Select(r => r ? "1" : "0"));

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

            return false;
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
                var view = CollectionViewSource.GetDefaultView(SensorContext.Sensors);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                view.Filter = ApplyFilter;
                SensorList.ItemsSource = view;
            }
            else
            {
                var view = CollectionViewSource.GetDefaultView(SensorContext.Sensors);
                view.Filter = ApplyFilter;
                SensorList.ItemsSource = view;
            }

            UpdateStatistics();

            NoDataHint.Visibility = SensorContext.Sensors.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private bool ApplyFilter(object item)
        {
            if (item is not SensorConfig sensor)
                return true;

            if (ChkShowAlarmsOnly == null || ChkShowAlarmsOnly.IsChecked != true)
                return true;

            return sensor.IsActive;
        }

        private void ChkShowAlarmsOnly_Changed(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(SensorList.ItemsSource);
            view?.Refresh();
        }

        private void UpdateStatistics()
        {
            int alarmCount = SensorContext.Sensors.Count(s => s.IsActive);
            int totalCount = SensorContext.Sensors.Count;

            TxtAlarmCount.Text = alarmCount.ToString();
            TxtTotalCount.Text = totalCount.ToString();
        }

        /// <summary>
        /// 當 PLC 連線成功時的回呼
        /// </summary>
        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SensorViewer] OnPlcConnectionEstablished called. SensorStatesInitialized={_sensorStatesInitialized}");
                #endif
                
                // 🔥 如果已經初始化過 Sensor 狀態，不重複執行
                if (_sensorStatesInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("[SensorViewer] Sensor states already initialized, skipping.");
                    return;
                }

                if (manager.Monitor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SensorViewer] Monitor not available.");
                    return;
                }

                // 從 SensorContext 智慧提取監控位址
                string monitorAddresses = SensorContext.GenerateMonitorAddresses();

                if (string.IsNullOrEmpty(monitorAddresses))
                {
                    System.Diagnostics.Debug.WriteLine("[SensorViewer] No monitor addresses generated.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[SensorViewer] Monitor addresses prepared: {monitorAddresses}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SensorViewer] Failed to prepare monitors: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 Sensor 狀態（靜默讀取，不觸發警報）- 只執行一次
        /// </summary>
        private async void InitializeSensorStates(IPlcManager manager)
        {
            if (manager == null || !manager.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[SensorViewer] Cannot initialize sensor states: PLC not connected.");
                return;
            }

            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[SensorViewer] InitializeSensorStates starting...");
            #endif

            var alarmSensors = new List<SensorConfig>();

            foreach (var sensor in SensorContext.Sensors)
            {
                try
                {
                    bool isActive = await EvaluateSensor(sensor, manager);
                    string currentValue = sensor.CurrentValue;

                    // 直接設定初始狀態，不呼叫 UpdateSensorState（避免觸發警報事件）
                    sensor.IsActive = isActive;
                    sensor.CurrentValue = currentValue;
                    
                    // 🔥 如果初始狀態就是觸發（異常），記錄並收集
                    if (isActive)
                    {
                        alarmSensors.Add(sensor);
                        
                        ComplianceContext.LogSystem(
                            $"[Sensor] 初始狀態異常: {sensor.OperationDescription} ({sensor.Device}) = {currentValue}",
                            LogLevel.Warning,
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

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SensorViewer] InitializeSensorStates completed. Total alarms: {alarmSensors.Count}");
            #endif
        }

        #endregion
    }
}
