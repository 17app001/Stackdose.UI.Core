using Stackdose.Abstractions.Hardware;
using Stackdose.Hardware.Plc;
using Stackdose.Mitsubishi.Plc;
using Stackdose.UI.Core.Helpers; // 引用 Context
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

        public IPlcManager? CurrentManager => _plcManager;
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

        // 🔥 新增：是否為全域預設 PLC (IsGlobal)
        public static readonly DependencyProperty IsGlobalProperty =
            DependencyProperty.Register(
                "IsGlobal",                 // 屬性名稱
                typeof(bool),               // 類型
                typeof(PlcStatus),          // 擁有者
                new PropertyMetadata(
                    true,                  // 預設值建議為 true，方便大多數情況使用 
                    OnIsGlobalChanged));    // ⚡ 關鍵：設定變更時的回呼函式

        public bool IsGlobal
        {
            get { return (bool)GetValue(IsGlobalProperty); }
            set { SetValue(IsGlobalProperty, value); }
        }

        // 當 IsGlobal 被設定為 True 時觸發
        private static void OnIsGlobalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcStatus plcStatus && (bool)e.NewValue)
            {
                // 當設定為 True 時，將自己註冊為全域預設值
                // 這樣 PlcLabel 找不到綁定時，就會來抓這個變數
                PlcContext.GlobalStatus = plcStatus;
            }
        }

        #endregion

        private async void PlcStatus_Loaded(object sender, RoutedEventArgs e)
        {
            // 雙重保險：載入時如果 IsGlobal 為 true，確保 Context 有被設定
            if (IsGlobal)
            {
                PlcContext.GlobalStatus = this;
                ComplianceContext.LogSystem("System initialized. Main PLC set.", Stackdose.UI.Core.Models.LogLevel.Info);
            }

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            IpDisplay.Text = $"{IpAddress}:{Port}";
            if (AutoConnect) await ConnectAsync();
            else { UpdateUiState(ConnectionState.Failed); StatusText.Text = "Click To Connecting"; }
        }

        private async void PlcStatus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => await ToggleConnectionAsync();

        private async Task ToggleConnectionAsync()
        {
            if (_isBusy) return;
            try
            {
                _isBusy = true;
                if (_plcManager != null && _plcManager.IsConnected) await DisconnectAsync();
                else await ConnectAsync();
            }
            finally { _isBusy = false; }
        }

        private async Task ConnectAsync()
        {
            UpdateUiState(ConnectionState.Connecting);
            try
            {
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
                            if (_plcManager != null) ScanUpdated?.Invoke(_plcManager);
                        }
                        catch { }
                    };
                }
                // 📝 LOG: 開始連線
                ComplianceContext.LogSystem($"Connecting to PLC ({IpAddress}:{Port})...", Stackdose.UI.Core.Models.LogLevel.Info);


                bool success = await _plcManager.InitializeAsync(IpAddress, Port, ScanInterval);
                if (success)
                {
                    StatusText.Text = "CONNECTED";
                    // ✅ LOG: 連線成功 (使用綠色 Success 等級)
                    ComplianceContext.LogSystem($"PLC Connection Established ({IpAddress})", Stackdose.UI.Core.Models.LogLevel.Success);
                    if (!string.IsNullOrWhiteSpace(MonitorAddress)) RegisterMonitors(MonitorAddress);
                }
                else
                {
                    StatusText.Text = "DISCONNECTED";
                    // ❌ LOG: 連線失敗 (使用紅色 Error 等級)
                    ComplianceContext.LogSystem($"PLC Connection Failed ({IpAddress})", Stackdose.UI.Core.Models.LogLevel.Error);
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
            if (_plcManager != null) await _plcManager.DisconnectAsync();
            UpdateUiState(ConnectionState.Failed);
            StatusText.Text = "Click To Connecting";
            // ⚠️ LOG: 手動斷線 (使用黃色 Warning 等級)
            ComplianceContext.LogSystem($"PLC Disconnected by User", Stackdose.UI.Core.Models.LogLevel.Warning);
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

        private void PlcStatus_Unloaded(object sender, RoutedEventArgs e) => Dispose();
        public void Dispose()
        {
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