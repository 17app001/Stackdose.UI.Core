using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.PrintHead.Feiyang;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

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
                new PropertyMetadata("feiyang_head1.json"));

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
        private dynamic? _printHead; // FeiyangPrintHead 實例
        private CancellationTokenSource? _temperatureMonitorCts;
        private bool _isConnected = false;

        #endregion

        #region Events

        /// <summary>
        /// 連線成功事件
        /// </summary>
        public event Action? ConnectionEstablished;

        /// <summary>
        /// 連線失敗或斷線事件
        /// </summary>
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
            // 載入配置檔
            if (!LoadConfiguration())
            {
                return;
            }

            // 自動連線（如果啟用）
            if (AutoConnect)
            {
                await Task.Delay(500); // 延遲確保 UI 完全載入
                await ConnectAsync();
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            // 停止監控並斷線
            StopTemperatureMonitoring();
            
            if (_isConnected && _printHead != null)
            {
                try
                {
                    _printHead.Disconnect();
                }
                catch { }
            }
        }

        #endregion

        #region 配置載入

        /// <summary>
        /// 載入 JSON 配置檔
        /// </summary>
        private bool LoadConfiguration()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    UpdateStatus(false, $"Config file not found: {ConfigFilePath}");
                    ComplianceContext.LogSystem(
                        $"[PrintHead] Config file not found: {ConfigFilePath}",
                        LogLevel.Error,
                        showInUi: false
                    );
                    return false;
                }

                string jsonContent = File.ReadAllText(ConfigFilePath);
                _config = JsonSerializer.Deserialize<PrintHeadConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (_config == null)
                {
                    UpdateStatus(false, "Failed to parse config file");
                    return false;
                }

                // 更新 UI 顯示配置資訊
                Dispatcher.Invoke(() =>
                {
                    ConfigFileText.Text = Path.GetFileName(ConfigFilePath);
                    BoardAddressText.Text = $"{_config.BoardIP}:{_config.BoardPort}";
                    ModelText.Text = _config.Model;
                });

                ComplianceContext.LogSystem(
                    $"[PrintHead] Config loaded: {_config.Name} ({_config.Model})",
                    LogLevel.Info,
                    showInUi: false
                );

                return true;
            }
            catch (Exception ex)
            {
                UpdateStatus(false, $"Config load error: {ex.Message}");
                ComplianceContext.LogSystem(
                    $"[PrintHead] Config load error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return false;
            }
        }

        #endregion

        #region 連線管理

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await ConnectAsync();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            DisconnectAsync();
        }

        /// <summary>
        /// 連線到 PrintHead
        /// </summary>
        private async Task ConnectAsync()
        {
            if (_config == null)
            {
                UpdateStatus(false, "No configuration loaded");
                return;
            }

            if (_isConnected)
            {
                UpdateStatus(true, "Already connected");
                return;
            }

            // 禁用連線按鈕
            Dispatcher.Invoke(() =>
            {
                ConnectButton.IsEnabled = false;
                StatusText.Text = "CONNECTING...";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            });

            try
            {
                // ⭐ 這裡需要實際的 FeiyangPrintHead 實例化邏輯
                // 目前先使用佔位符
                ComplianceContext.LogSystem(
                    $"[PrintHead] Connecting to {_config.Name} ({_config.BoardIP}:{_config.BoardPort})...",
                    LogLevel.Info,
                    showInUi: true
                );

                // TODO: 實際連線邏輯
                _printHead = new FeiyangPrintHead(ConfigFilePath);
                bool connected = await _printHead.Connect();

                // 模擬連線（實際應使用上面的邏輯）
                //await Task.Delay(1000);
                //bool connected = true; // 模擬成功

                if (connected)
                {
                    _isConnected = true;

                    // TODO: 配置列印模式
                    // _printHead.ConfigurePrintMode();

                    UpdateStatus(true, "CONNECTED");

                    ComplianceContext.LogSystem(
                        $"[PrintHead] Connection established: {_config.Name}",
                        LogLevel.Success,
                        showInUi: true
                    );

                    // 觸發事件
                    ConnectionEstablished?.Invoke();

                    // 啟動溫度監控
                    StartTemperatureMonitoring();
                }
                else
                {
                    UpdateStatus(false, "Connection failed");
                    string errorMsg = _printHead.LastErrorMessage ?? "Unknown error";                     
                    ComplianceContext.LogSystem(
                        $"[PrintHead] Connection failed: {errorMsg}",
                        LogLevel.Error,
                        showInUi: true
                    );

                    ConnectionLost?.Invoke(errorMsg);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus(false, "Connection error");

                ComplianceContext.LogSystem(
                    $"[PrintHead] Connection error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );

                ConnectionLost?.Invoke(ex.Message);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    ConnectButton.IsEnabled = !_isConnected;
                });
            }
        }

        /// <summary>
        /// 斷線
        /// </summary>
        private void DisconnectAsync()
        {
            if (!_isConnected)
            {
                return;
            }

            // 停止溫度監控
            StopTemperatureMonitoring();

            try
            {
                // TODO: 實際斷線邏輯
                // _printHead?.Disconnect();

                _isConnected = false;

                UpdateStatus(false, "DISCONNECTED");

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

        #region 溫度監控

        /// <summary>
        /// 啟動溫度監控
        /// </summary>
        private void StartTemperatureMonitoring()
        {
            StopTemperatureMonitoring(); // 確保沒有重複的監控執行緒

            _temperatureMonitorCts = new CancellationTokenSource();
            var token = _temperatureMonitorCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _isConnected)
                {
                    try
                    {
                        // TODO: 實際讀取溫度邏輯
                        // var temps = _printHead?.GetTemperatures();

                        // 模擬溫度數據（實際應使用上面的邏輯）
                        var temps = new[] { 38.5, 39.2, 38.8, 39.0 };

                        if (temps != null && temps.Length > 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                UpdateTemperatureDisplay(temps);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ComplianceContext.LogSystem(
                            $"[PrintHead] Temperature read error: {ex.Message}",
                            LogLevel.Warning,
                            showInUi: false
                        );
                    }

                    await Task.Delay(200, token); // 每 200ms 更新一次
                }
            }, token);
        }

        /// <summary>
        /// 停止溫度監控
        /// </summary>
        private void StopTemperatureMonitoring()
        {
            _temperatureMonitorCts?.Cancel();
            _temperatureMonitorCts?.Dispose();
            _temperatureMonitorCts = null;
        }

        /// <summary>
        /// 更新溫度顯示
        /// </summary>
        private void UpdateTemperatureDisplay(double[] temperatures)
        {
            var displayList = new System.Collections.Generic.List<string>();

            for (int i = 0; i < temperatures.Length; i++)
            {
                displayList.Add($"CH{i + 1}: {temperatures[i]:F1}°C");
            }

            TemperaturesPanel.ItemsSource = displayList;
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 更新連線狀態
        /// </summary>
        private void UpdateStatus(bool connected, string statusText)
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = connected;

                StatusText.Text = statusText;
                StatusText.Foreground = connected
                    ? new SolidColorBrush(Color.FromRgb(46, 204, 113))  // Green
                    : new SolidColorBrush(Color.FromRgb(255, 71, 87));  // Red

                ConnectButton.IsEnabled = !connected;
                DisconnectButton.IsEnabled = connected;
            });
        }

        #endregion
    }
}
