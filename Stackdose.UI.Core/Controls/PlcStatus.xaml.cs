using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Stackdose.Abstractions.Hardware;
using Stackdose.Hardware.Plc;
using Stackdose.Mitsubishi.Plc;

namespace Stackdose.UI.Core.Controls
{
    public partial class PlcStatus : UserControl, IDisposable
    {
        private IPlcManager? _plcManager;
        private bool _isBusy = false; // 防止重複點擊

        // 新增：開放 Manager 給外部 (例如 PlcWatcher) 使用
        public IPlcManager? CurrentManager => _plcManager;

        // 新增：當 PLC 掃描一輪完成時觸發的事件，供外部訂閱刷新
        public event Action<IPlcManager>? ScanUpdated;

        public PlcStatus()
        {
            InitializeComponent();
            this.Loaded += PlcStatus_Loaded;
            this.Unloaded += PlcStatus_Unloaded;

            // 讓 UserControl 可以接收點擊事件
            this.Cursor = Cursors.Hand;
            this.MouseLeftButtonDown += PlcStatus_MouseLeftButtonDown;
        }

        #region Dependency Properties

        public static readonly DependencyProperty IpAddressProperty =
            DependencyProperty.Register("IpAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata("127.0.0.1"));

        public string IpAddress
        {
            get { return (string)GetValue(IpAddressProperty); }
            set { SetValue(IpAddressProperty, value); }
        }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(int), typeof(PlcStatus), new PropertyMetadata(502));

        public int Port
        {
            get { return (int)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        #endregion

        // 自動連線屬性，預設為 true
        public static readonly DependencyProperty AutoConnectProperty =
            DependencyProperty.Register("AutoConnect", typeof(bool), typeof(PlcStatus), new PropertyMetadata(true));

        public bool AutoConnect
        {
            get { return (bool)GetValue(AutoConnectProperty); }
            set { SetValue(AutoConnectProperty, value); }
        }

        // 新增：掃描頻率屬性，預設 150ms
        public static readonly DependencyProperty ScanIntervalProperty =
            DependencyProperty.Register("ScanInterval", typeof(int), typeof(PlcStatus), new PropertyMetadata(150));

        public int ScanInterval
        {
            get { return (int)GetValue(ScanIntervalProperty); }
            set { SetValue(ScanIntervalProperty, value); }
        }

        // 監控設定字串
        // 模式1 (單組): "D0" (搭配 MonitorLength)
        // 模式2 (多組): "D10,8,R2000,10,D31,D32" (自動解析)
        public static readonly DependencyProperty MonitorAddressProperty =
            DependencyProperty.Register("MonitorAddress", typeof(string), typeof(PlcStatus), new PropertyMetadata(null));

        public string MonitorAddress
        {
            get { return (string)GetValue(MonitorAddressProperty); }
            set { SetValue(MonitorAddressProperty, value); }
        }

        // 監控長度，預設 1 (僅在單組模式下生效或作為多組模式的預設值)
        public static readonly DependencyProperty MonitorLengthProperty =
            DependencyProperty.Register("MonitorLength", typeof(int), typeof(PlcStatus), new PropertyMetadata(1));

        public int MonitorLength
        {
            get { return (int)GetValue(MonitorLengthProperty); }
            set { SetValue(MonitorLengthProperty, value); }
        }

        private async void PlcStatus_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            IpDisplay.Text = $"{IpAddress}:{Port}";

            // 只有當 AutoConnect 為 true 時才執行自動連線
            if (AutoConnect)
            {
                await ConnectAsync();
            }
            else
            {
                // 若不自動連線，先顯示待機狀態
                UpdateUiState(ConnectionState.Failed);
                StatusText.Text = "Click To Connecting";
            }
        }

        private async void PlcStatus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 點擊時切換連線狀態
            await ToggleConnectionAsync();
        }

        private async Task ToggleConnectionAsync()
        {
            if (_isBusy) return;

            try
            {
                _isBusy = true;

                if (_plcManager != null && _plcManager.IsConnected)
                {
                    // 如果已連線，則斷線
                    await DisconnectAsync();
                }
                else
                {
                    // 如果未連線，則連線
                    await ConnectAsync();
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task ConnectAsync()
        {
            UpdateUiState(ConnectionState.Connecting);

            try
            {
                // 如果 Manager 尚未建立，則建立新的
                if (_plcManager == null)
                {
                    // 這裡依照您的需求移除 Logger，使用無參數建構子
                    IPlcClient client = new FX3UPlcClient(null);
                    _plcManager = new PlcManager(client, null);

                    _plcManager.ScanElapsedChanged += (ms) =>
                    {
                        try
                        {
                            // 1. 檢查 Dispatcher 是否已經開始關閉 (防呆)
                            if (Dispatcher.HasShutdownStarted) return;

                            Dispatcher.Invoke(() =>
                            {
                                if (!Dispatcher.HasShutdownStarted) StatusText.Text = $"ONLINE ({ms}ms)";
                            });

                            // 2. 觸發外部刷新事件 (通知 PlcWatcher)
                            // 注意：這裡在背景執行緒觸發，PlcWatcher 收到後需要自己 Invoke
                            if (_plcManager != null) ScanUpdated?.Invoke(_plcManager);
                        }
                        catch { }
                    };
                }

                // 執行連線，傳入 ScanInterval (150ms)
                bool success = await _plcManager.InitializeAsync(IpAddress, Port, ScanInterval);

                if (success)
                {
                    StatusText.Text = "CONNECTED";
                    // =========================================================
                    // 註冊監控區塊 (支援複雜格式)
                    // =========================================================
                    if (!string.IsNullOrWhiteSpace(MonitorAddress))
                    {
                        RegisterMonitors(MonitorAddress);
                    }
                }
                else
                {
                    StatusText.Text = "DISCONNECTED";
                }

                UpdateUiState(success ? ConnectionState.Connected : ConnectionState.Failed);
            }
            catch (Exception ex)
            {
                StatusText.Text = "ERR: " + ex.Message;
                UpdateUiState(ConnectionState.Failed);
            }
        }

        private async Task DisconnectAsync()
        {
            if (_plcManager != null)
            {
                await _plcManager.DisconnectAsync();
            }
            UpdateUiState(ConnectionState.Failed);
            StatusText.Text = "Click To Connecting"; // 斷線後恢復提示文字
        }

        /// <summary>
        /// 解析並註冊監控位址
        /// 格式範例: "D10,8,R2000,10,D31,D32"
        /// </summary>
        private void RegisterMonitors(string config)
        {
            if (_plcManager?.Monitor == null) return;

            // 如果包含逗號，使用進階解析模式
            if (config.Contains(","))
            {
                var parts = config.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < parts.Length; i++)
                {
                    string current = parts[i].Trim();
                    int length = MonitorLength; // 預設長度，使用屬性值

                    // 檢查下一個 token 是否為數字 (如果是，則視為長度)
                    if (i + 1 < parts.Length)
                    {
                        string nextToken = parts[i + 1].Trim();
                        // 嘗試解析為數字，且這個數字不能像 "D100" 這種位址格式 (簡單防呆：是否純數字)
                        if (int.TryParse(nextToken, out int parsedLen))
                        {
                            length = parsedLen;
                            i++; // 跳過下一個 token，因為已經被用作長度了
                        }
                    }

                    try
                    {
                        _plcManager.Monitor.Register(current, length);
                    }
                    catch
                    {
                        // 忽略個別錯誤，避免影響其他註冊
                    }
                }
            }
            else
            {
                // 單組模式 (相容舊設定)
                try
                {
                    _plcManager.Monitor.Register(config, MonitorLength);
                }
                catch { }
            }
        }
        private void PlcStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_plcManager != null)
            {
                _plcManager.Dispose();
                _plcManager = null;
            }
        }

        // --- UI 狀態控制 ---
        private enum ConnectionState { Connecting, Connected, Failed }

        private void UpdateUiState(ConnectionState state)
        {
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
                    // StatusText 在呼叫處設定
                    StatusText.Text = "DISCONNECTED";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    break;
            }
        }
    }
}