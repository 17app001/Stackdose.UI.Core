using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PlcEventTrigger - 隱藏的 PLC 事件觸發器
    /// 用途：監聽特定 PLC 位址（如 M237, M238），當值為 1 時觸發事件，並自動清空為 0
    /// 特點：完全隱藏、不佔空間、自動管理
    /// </summary>
    public partial class PlcEventTrigger : UserControl
    {
        private PlcStatus? _boundStatus;
        private bool _lastValue = false; // 記錄上一次的值（用於偵測邊緣觸發）
        
        // 🔥 快取 Address，避免在非 UI 執行緒上存取 DependencyProperty
        private string _cachedAddress = "M0";
        private string _cachedEventName = "";
        private TriggerCondition _cachedTriggerCondition = TriggerCondition.OnRising;

        public PlcEventTrigger()
        {
            InitializeComponent();
            Loaded += PlcEventTrigger_Loaded;
            Unloaded += PlcEventTrigger_Unloaded;
        }

        #region Dependency Properties

        /// <summary>
        /// PLC 位址（如 M237, M238）
        /// </summary>
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(PlcEventTrigger), new PropertyMetadata("M0"));
        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        /// <summary>
        /// 事件名稱（用於識別，如 "Recipe1Selected"）
        /// </summary>
        public static readonly DependencyProperty EventNameProperty =
            DependencyProperty.Register("EventName", typeof(string), typeof(PlcEventTrigger), new PropertyMetadata(""));
        public string EventName
        {
            get => (string)GetValue(EventNameProperty);
            set => SetValue(EventNameProperty, value);
        }

        /// <summary>
        /// 是否自動清空（觸發後寫回 0）
        /// </summary>
        public static readonly DependencyProperty AutoClearProperty =
            DependencyProperty.Register("AutoClear", typeof(bool), typeof(PlcEventTrigger), new PropertyMetadata(true));
        public bool AutoClear
        {
            get => (bool)GetValue(AutoClearProperty);
            set => SetValue(AutoClearProperty, value);
        }

        /// <summary>
        /// 清空延遲（毫秒，預設 0）
        /// </summary>
        public static readonly DependencyProperty ClearDelayProperty =
            DependencyProperty.Register("ClearDelay", typeof(int), typeof(PlcEventTrigger), new PropertyMetadata(0));
        public int ClearDelay
        {
            get => (int)GetValue(ClearDelayProperty);
            set => SetValue(ClearDelayProperty, value);
        }

        /// <summary>
        /// 觸發條件（預設 OnRising: 0→1 觸發）
        /// </summary>
        public static readonly DependencyProperty TriggerConditionProperty =
            DependencyProperty.Register("TriggerCondition", typeof(TriggerCondition), typeof(PlcEventTrigger), 
                new PropertyMetadata(TriggerCondition.OnRising));
        public TriggerCondition TriggerCondition
        {
            get => (TriggerCondition)GetValue(TriggerConditionProperty);
            set => SetValue(TriggerConditionProperty, value);
        }

        /// <summary>
        /// 綁定的 PlcStatus
        /// </summary>
        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register("TargetStatus", typeof(PlcStatus), typeof(PlcEventTrigger), 
                new PropertyMetadata(null, OnTargetStatusChanged));
        public PlcStatus TargetStatus
        {
            get => (PlcStatus)GetValue(TargetStatusProperty);
            set => SetValue(TargetStatusProperty, value);
        }

        #endregion

        #region 事件處理

        private void PlcEventTrigger_Loaded(object sender, RoutedEventArgs e)
        {
            // 🔥 快取屬性（在 UI 執行緒上）
            _cachedAddress = Address;
            _cachedEventName = EventName;
            _cachedTriggerCondition = TriggerCondition;
            
            // 🔥 註冊到 PlcEventContext（用於自動監控）
            PlcEventContext.Register(this);
            
            // 自動綁定 PlcStatus
            TryResolveContextStatus();
        }

        private void PlcEventTrigger_Unloaded(object sender, RoutedEventArgs e)
        {
            // 🔥 註銷 PlcEventContext
            PlcEventContext.Unregister(this);
            
            // 解除綁定
            BindToStatus(null);
        }

        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlcEventTrigger trigger && e.NewValue != null)
            {
                // 🔥 確保在 UI 執行緒上執行綁定
                trigger.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        // 直接綁定新的 PlcStatus 日誌在 BindToStatus 中記錄
                        trigger.BindToStatus(e.NewValue as PlcStatus);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PlcEventTrigger] 綁定錯誤: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            }
        }

        /// <summary>
        /// 自動尋找父層的 PlcStatus
        /// </summary>
        private void TryResolveContextStatus()
        {
            // 🔥 如果已經透過 TargetStatus 綁定，不需要再自動尋找
            if (TargetStatus != null)
            {
                return; // TargetStatus 的變更會觸發 OnTargetStatusChanged
            }

            // 🔥 優先使用全域 PlcStatus
            var globalStatus = PlcContext.GlobalStatus;
            if (globalStatus != null)
            { 
                BindToStatus(globalStatus);
                return;
            }

            // Fallback: 尋找父層的 PlcStatus
            DependencyObject? parent = this;
            while (parent != null)
            {
                parent = LogicalTreeHelper.GetParent(parent);
                if (parent is PlcStatus status)
                {
                    BindToStatus(status);
                    return;
                }
            }
            
            // 🔥 如果都找不到，記錄警告
            ComplianceContext.LogSystem(
                $"[PlcEventTrigger] {_cachedEventName} ({_cachedAddress}) 警告：找不到 PlcStatus",
                Models.LogLevel.Warning,
                showInUi: true
            );
        }

        /// <summary>
        /// 綁定到 PlcStatus
        /// </summary>
        private void BindToStatus(PlcStatus? newStatus)
        {
            // 🔥 取消訂閱舊的 Monitor 事件
            if (_boundStatus?.CurrentManager?.Monitor is Hardware.Plc.PlcMonitorService oldMonitor)
            {
                oldMonitor.BitChanged -= OnMonitorBitChanged;
            }

            _boundStatus = newStatus;

            // 🔥 嘗試立即綁定 Monitor
            TryBindMonitor();
        }

        /// <summary>
        /// 嘗試綁定 Monitor（如果 PlcStatus 已連線）
        /// </summary>
        private void TryBindMonitor()
        {
            if (_boundStatus?.CurrentManager?.Monitor is Hardware.Plc.PlcMonitorService monitor)
            {
                // ✅ PlcStatus 已連線，直接註冊並訂閱
                monitor.Register(_cachedAddress, 1);
                monitor.BitChanged += OnMonitorBitChanged;
            }
            else if (_boundStatus != null)
            {
                // ⚠️ PlcStatus 存在但還沒連線，延遲重試
                Dispatcher.BeginInvoke(async () =>
                {
                    await Task.Delay(500); // 延遲 500ms 後重試
                    TryBindMonitor(); // 遞迴重試
                });
            }
        }

        /// <summary>
        /// Monitor BitChanged 事件處理
        /// </summary>
        private void OnMonitorBitChanged(string address, bool value)
        {
            // 🔥 使用快取的 Address（避免存取 DependencyProperty）
            if (address != _cachedAddress)
                return;

            // 🔥 在 Monitor 執行緒上先複製必要的變數
            bool lastValue = _lastValue;
            TriggerCondition condition = _cachedTriggerCondition;
            string eventName = _cachedEventName;

            // 🔥 在 UI 執行緒上處理
            Dispatcher.BeginInvoke(() =>
            {
                // 🔥 除錯日誌：顯示當前值和上次值
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlcEventTrigger] {eventName} ({address}) - 當前值: {value}, 上次值: {lastValue}");
                #endif

                // 判斷是否觸發
                bool shouldTrigger = condition switch
                {
                    TriggerCondition.OnRising => !lastValue && value,  // 0→1
                    TriggerCondition.OnFalling => lastValue && !value, // 1→0
                    TriggerCondition.OnChange => lastValue != value,   // 任何變化
                    _ => false
                };

                // 更新記錄
                _lastValue = value;

                // 觸發事件
                if (shouldTrigger)
                {
                    //// 🔥 日誌：觸發條件滿足
                    //ComplianceContext.LogSystem(
                    //    $"[PlcEventTrigger] {eventName} ({address}) 條件滿足！觸發事件（{condition}）",
                    //    Models.LogLevel.Info,
                    //    showInUi: true
                    //);

                    OnEventTriggered(value);
                }
            });
        }

        /// <summary>
        /// 觸發事件
        /// </summary>
        private async void OnEventTriggered(bool value)
        {
            var manager = _boundStatus?.CurrentManager;
            if (manager == null)
                return;

            // 1. 記錄觸發日誌
            ComplianceContext.LogSystem(
                $"[PlcEvent] {EventName} ({Address}) 觸發",
                Models.LogLevel.Info,
                showInUi: true
            );

            // 2. 🔥 通知 PlcEventContext（同步等待事件處理完成）
            PlcEventContext.NotifyEventTriggered(this, value);

            // 3. 記錄事件處理完成
            ComplianceContext.LogSystem(
                $"[PlcEvent] {EventName} ({Address}) 事件處理完成",
                Models.LogLevel.Info,
                showInUi: false
            );

            // 4. 自動清空（寫回 0）- 確保在事件處理完成後才執行
            if (AutoClear && manager.IsConnected)
            {
                // 延遲（如果有設定）
                if (ClearDelay > 0)
                {
                    await Task.Delay(ClearDelay);
                }

                // 寫回 0
                try
                {
                    await manager.WriteAsync($"{Address},0");

                    ComplianceContext.LogSystem(
                        $"[PlcEvent] {EventName} ({Address}) 自動清零",
                        Models.LogLevel.Info,
                        showInUi: true
                    );
                }
                catch (Exception ex)
                {
                    ComplianceContext.LogSystem(
                        $"[PlcEvent] 清空失敗: {ex.Message}",
                        Models.LogLevel.Error,
                        showInUi: true
                    );
                }
            }
        }

        #endregion
    }

    #region 觸發條件列舉

    /// <summary>
    /// 觸發條件
    /// </summary>
    public enum TriggerCondition
    {
        /// <summary>
        /// 上升緣觸發 (0→1)
        /// </summary>
        OnRising,

        /// <summary>
        /// 下降緣觸發 (1→0)
        /// </summary>
        OnFalling,

        /// <summary>
        /// 任何變化都觸發
        /// </summary>
        OnChange
    }

    #endregion
}
