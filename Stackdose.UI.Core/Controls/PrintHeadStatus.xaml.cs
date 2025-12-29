using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
        private FeiyangPrintHead? _printHead;
        private CancellationTokenSource? _temperatureMonitorCts;
        private bool _isConnected = false;
        private bool _isExpanded = false;

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

            // ⭐ 初始化時顯示 N/A
            ResetStatusDisplay();

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
                    
                    // ⭐ 從 PrintHeadContext 注銷
                    if (_config != null)
                    {
                        PrintHeadContext.UnregisterPrintHead(_config.Name);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 重置狀態顯示為 N/A
        /// </summary>
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

        /// <summary>
        /// 展開/收合按鈕點擊事件
        /// </summary>
        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            AnimateExpand(_isExpanded);
        }

        /// <summary>
        /// 動畫展開/收合狀態面板
        /// </summary>
        private void AnimateExpand(bool expand)
        {
            // 旋轉箭頭圖示
            var rotateAnimation = new DoubleAnimation
            {
                To = expand ? 180 : 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var rotation = ExpandIcon.RenderTransform as RotateTransform;
            if (rotation != null)
            {
                rotation.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            }

            // 顯示/隱藏狀態面板
            StatusDataPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
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
                    UpdateStatus(false);
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
                    UpdateStatus(false);
                    return false;
                }

                // 更新 UI 顯示配置資訊（只顯示 IP:Port）
                Dispatcher.Invoke(() =>
                {
                    BoardAddressText.Text = $"{_config.BoardIP}:{_config.BoardPort}";
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
                UpdateStatus(false);
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

        private async void PowerButton_Checked(object sender, RoutedEventArgs e)
        {
            await ConnectAsync();
        }

        private void PowerButton_Unchecked(object sender, RoutedEventArgs e)
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

                _printHead = new FeiyangPrintHead(ConfigFilePath);
                
                _printHead.Log = (msg) =>
                {
                    ComplianceContext.LogSystem(msg, LogLevel.Info, showInUi: false);
                };
                
                bool connected = await _printHead.Connect();

                if (!connected)
                {
                    string errorMsg = _printHead.LastErrorMessage ?? "Unknown error";
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

                var (firmwareSuccess, firmwareMsg) = await Task.Run(() => _printHead.Setup());
                
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

                var (printModeSuccess, printModeMsg) = await Task.Run(() => _printHead.ConfigurePrintMode());
                
                if (!printModeSuccess)
                {
                    UpdateStatus(false);
                    ComplianceContext.LogSystem(
                        $"[PrintHead] PrintMode config failed: {printModeMsg}",
                        LogLevel.Warning,
                        showInUi: true
                    );
                }

                _isConnected = true;

                UpdateStatus(true);

                ComplianceContext.LogSystem(
                    $"[PrintHead] ✅ Connection established: {_config.Name}",
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
                    $"[PrintHead] ❌ Connection error: {ex.Message}\nStack: {ex.StackTrace}",
                    LogLevel.Error,
                    showInUi: true
                );

                ConnectionLost?.Invoke(ex.Message);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    PowerButton.IsEnabled = true;
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

            StopTemperatureMonitoring();

            try
            {
                _printHead?.Disconnect();

                if (_config != null)
                {
                    PrintHeadContext.UnregisterPrintHead(_config.Name);
                }

                _isConnected = false;

                // ⭐ 斷線後重置顯示為 N/A
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

        #region 溫度監控

        /// <summary>
        /// 啟動溫度監控
        /// </summary>
        private void StartTemperatureMonitoring()
        {
            StopTemperatureMonitoring(); // 確保沒有重複的監控執行緒

            _temperatureMonitorCts = new CancellationTokenSource();
            var token = _temperatureMonitorCts.Token;

            ComplianceContext.LogSystem(
                "[PrintHead] 🌡️ Status monitoring started",
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
                        // ⭐ 读取完整状态数据
                        var status = _printHead?.GetStatus();

                        if (status != null)
                        {
                            successCount++;

                            // ⭐ 只在第一次成功时记录日志
                            if (!firstReadSuccess)
                            {
                                firstReadSuccess = true;
                                ComplianceContext.LogSystem(
                                    $"[PrintHead] ✅ First status read successful",
                                    LogLevel.Success,
                                    showInUi: true
                                );
                            }

                            Dispatcher.Invoke(() =>
                            {
                                UpdateStatusDisplay(status);
                            });
                        }
                        else
                        {
                            errorCount++;
                            if (errorCount <= 3) // 只记录前 3 次错误
                            {
                                ComplianceContext.LogSystem(
                                    $"[PrintHead] ⚠️ Status read returned null (error #{errorCount})",
                                    LogLevel.Warning,
                                    showInUi: true
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        if (errorCount <= 3) // 只记录前 3 次错误
                        {
                            ComplianceContext.LogSystem(
                                $"[PrintHead] ❌ Status read error #{errorCount}: {ex.Message}",
                                LogLevel.Warning,
                                showInUi: true
                            );
                        }
                    }

                    await Task.Delay(500, token); // 每 500ms 更新一次
                }

                ComplianceContext.LogSystem(
                    $"[PrintHead] Status monitoring stopped (success: {successCount}, errors: {errorCount})",
                    LogLevel.Info,
                    showInUi: true
                );
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
        /// 更新状态显示（温度、电压、编码器、PrintIndex）
        /// </summary>
        private void UpdateStatusDisplay(dynamic status)
        {
            try
            {
                // 1. 温度（墨水温度）
                if (status.InkTemperatureA != null)
                {
                    float temp = (float)status.InkTemperatureA;
                    if (temp >= 0 && temp <= 100)
                    {
                        TemperatureText.Text = $"{temp:F1}°C";
                    }
                }

                // 2. 电压（4个通道）- 假设有 VoltageA, VoltageB, VoltageC, VoltageD
                var voltages = new System.Collections.Generic.List<string>();
                if (status.VoltageA != null) voltages.Add($"V1: {status.VoltageA:F1}V");
                if (status.VoltageB != null) voltages.Add($"V2: {status.VoltageB:F1}V");
                if (status.VoltageC != null) voltages.Add($"V3: {status.VoltageC:F1}V");
                if (status.VoltageD != null) voltages.Add($"V4: {status.VoltageD:F1}V");
                
                if (voltages.Count > 0)
                {
                    VoltagesPanel.ItemsSource = voltages;
                }

                // 3. 编码器
                if (status.Encoder != null)
                {
                    EncoderText.Text = status.Encoder.ToString();
                }

                // 4. PrintIndex
                if (status.PrintIndex != null)
                {
                    PrintIndexText.Text = status.PrintIndex.ToString();
                }
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[PrintHead] ⚠️ UpdateStatusDisplay error: {ex.Message}",
                    LogLevel.Warning,
                    showInUi: false
                );
            }
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 更新連線狀態（使用狀態燈號）
        /// </summary>
        private void UpdateStatus(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                _isConnected = connected;

                Color statusColor = connected 
                    ? Color.FromRgb(46, 204, 113)
                    : Color.FromRgb(255, 71, 87);
                
                UpdateStatusLight(statusColor);

                // 更新 PowerButton 狀態
                PowerButton.IsChecked = connected;
            });
        }

        /// <summary>
        /// 更新狀態燈號顏色和光暈
        /// </summary>
        private void UpdateStatusLight(Color color)
        {
            StatusLight.Fill = new SolidColorBrush(color);
            
            if (StatusGlow != null)
            {
                StatusGlow.Color = color;
            }
        }

        #endregion
    }
}
