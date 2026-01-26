using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.Abstractions.Logging;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 賽博風格主框架控制項
    /// </summary>
    /// <remarks>
    /// <para>提供完整的工業控制介面框架，包含：</para>
    /// <list type="bullet">
    /// <item>系統標題列與時鐘顯示</item>
    /// <item>使用者登入/登出狀態顯示</item>
    /// <item>Dark/Light 主題切換（整合 ThemeManager）</item>
    /// <item>狀態指示器（可選）</item>
    /// <item>自動填滿整個 Window</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 基本用法：
    /// <code>
    /// &lt;Custom:CyberFrame Title="MODEL-S" /&gt;
    /// </code>
    /// </example>
    public partial class CyberFrame : UserControl
    {
        #region Private Fields

        /// <summary>時鐘計時器</summary>
        private DispatcherTimer? _clockTimer;

        /// <summary>🔥 全域 PLC 連線管理器</summary>
        private PlcStatus? _globalPlcStatus;

        #endregion

        #region Constructor

        /// <summary>
        /// 建構函數
        /// </summary>
        public CyberFrame()
        {
            InitializeComponent();

            // 🔥 設計階段不執行初始化
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            InitializeClock();
            InitializeSecurityEvents();
            UpdateUserInfo();

            // ✅ 改用 Loaded 事件（確保 ComplianceContext 已初始化）
            this.Loaded += CyberFrame_Loaded;
            this.Unloaded += CyberFrame_Unloaded;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化時鐘計時器
        /// </summary>
        private void InitializeClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        /// <summary>
        /// 訂閱安全上下文事件
        /// </summary>
        private void InitializeSecurityEvents()
        {
            SecurityContext.LoginSuccess += OnLoginSuccess;
            SecurityContext.LogoutOccurred += OnLogoutOccurred;
        }

        /// <summary>
        /// 🔥 初始化批次寫入狀態燈號
        /// </summary>
        private void InitializeBatchWriteIndicator()
        {

            // 訂閱批次刷新事件
            SqliteLogger.BatchFlushStarted += OnBatchFlushStarted;
            SqliteLogger.BatchFlushCompleted += OnBatchFlushCompleted;

        }

        /// <summary>
        /// 🔥 初始化全域 PLC 連線
        /// </summary>
        private void InitializeGlobalPlcConnection()
        {
            try
            {
                // 創建隱藏的 PlcStatus 實例作為全域連線管理器
                _globalPlcStatus = new PlcStatus
                {
                    IpAddress = PlcIpAddress,
                    Port = PlcPort,
                    AutoConnect = false,  // 🔥 禁用自動連線
                    IsGlobal = true,
                    MonitorAddress = MonitorAddress,
                    MonitorLength = 1,
                    ScanInterval = PlcScanInterval,
                    Visibility = Visibility.Collapsed
                };

                // 將其添加到 CyberFrame 的視覺樹中（但不顯示）
                var rootGrid = this.FindName("Root") as Grid;
                if (rootGrid != null && rootGrid.Parent is Border rootBorder && rootBorder.Child is Grid mainGrid)
                {
                    mainGrid.Children.Add(_globalPlcStatus);
                    System.Diagnostics.Debug.WriteLine($"[CyberFrame] Global PlcStatus initialized: {PlcIpAddress}:{PlcPort}");
                    ComplianceContext.LogSystem(
                        $"[CyberFrame] PLC 連線管理器已初始化 (AutoConnect disabled)",
                        LogLevel.Info,
                        showInUi: false
                    );

                    // 🔥 延遲 2 秒後才開始背景連線（確保 MainWindow 已完全顯示）
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Dispatcher.InvokeAsync(async () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("[CyberFrame] 開始背景連線 PLC (delayed)...");

                                // 透過反射呼叫 ConnectAsync
                                var connectMethod = _globalPlcStatus.GetType().GetMethod("ConnectAsync",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                if (connectMethod != null)
                                {
                                    var task = connectMethod.Invoke(_globalPlcStatus, null) as Task;
                                    if (task != null)
                                    {
                                        await task;
                                        System.Diagnostics.Debug.WriteLine("[CyberFrame] PLC 背景連線完成");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CyberFrame] PLC 背景連線失敗: {ex.Message}");
                            }
                        });
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CyberFrame] Warning: Cannot find root grid to add PlcStatus");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] InitializeGlobalPlcConnection Error: {ex.Message}");
                ComplianceContext.LogSystem(
                    $"[CyberFrame] PLC 連線初始化失敗: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
            }
        }

        /// <summary>
        /// 控制項載入時的初始化
        /// </summary>
        private void CyberFrame_Loaded(object sender, RoutedEventArgs e)
        {
            // 🔥 設計階段不執行
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // ✅ 強制輸出（Console + Debug + LiveLogViewer）
            Console.WriteLine("========== CyberFrame_Loaded ==========");
            System.Diagnostics.Debug.WriteLine("========== CyberFrame_Loaded ==========");
            ComplianceContext.LogSystem("========== CyberFrame_Loaded ==========", LogLevel.Info);

            try
            {
                // 🔥 初始化使用者管理服務（會自動建立預設 Admin）
                var _ = new Services.UserManagementService();
                System.Diagnostics.Debug.WriteLine("[CyberFrame] UserManagementService initialized");

                // 🔥 NEW: 初始化全域 PLC 連線
                InitializeGlobalPlcConnection();

                // 確保 ComplianceContext 已初始化（觸發靜態建構函數）
                ComplianceContext.LogSystem("[CyberFrame] Loaded, initializing batch write indicator...",
                    LogLevel.Info, showInUi: true); // ✅ showInUi 改為 true

                Console.WriteLine("[CyberFrame] 訂閱前...");

                // 訂閱批次寫入事件
                InitializeBatchWriteIndicator();

                Console.WriteLine("[CyberFrame] 訂閱後...");

                ComplianceContext.LogSystem("[CyberFrame] 批次寫入事件已訂閱",
                    LogLevel.Success, showInUi: true); // ✅ 顯示在 LiveLogViewer

                System.Diagnostics.Debug.WriteLine("[CyberFrame] 批次寫入事件已訂閱");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CyberFrame] CyberFrame_Loaded ERROR: {ex.Message}");
                ComplianceContext.LogSystem($"[CyberFrame] ERROR: {ex.Message}",
                    LogLevel.Error, showInUi: true);
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] CyberFrame_Loaded Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 控制項卸載時的清理工作
        /// </summary>
        private void CyberFrame_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消訂閱事件avoiding記憶體洩漏
            SecurityContext.LoginSuccess -= OnLoginSuccess;
            SecurityContext.LogoutOccurred -= OnLogoutOccurred;

            // 🔥 取消訂閱批次寫入事件
            SqliteLogger.BatchFlushStarted -= OnBatchFlushStarted;
            SqliteLogger.BatchFlushCompleted -= OnBatchFlushCompleted;

            // 停止並清理計時器
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer.Tick -= ClockTimer_Tick;
                _clockTimer = null;
            }
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 系統標題
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(CyberFrame),
                new PropertyMetadata("SYSTEM"));

        /// <summary>
        /// 取得或設定系統標題
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// 是否顯示狀態指示器（Alarm、Total 等）
        /// </summary>
        public static readonly DependencyProperty ShowStatusIndicatorsProperty =
            DependencyProperty.Register(
                nameof(ShowStatusIndicators),
                typeof(bool),
                typeof(CyberFrame),
                new PropertyMetadata(false, OnShowStatusIndicatorsChanged));

        /// <summary>
        /// 取得或設定是否顯示狀態指示器
        /// </summary>
        public bool ShowStatusIndicators
        {
            get => (bool)GetValue(ShowStatusIndicatorsProperty);
            set => SetValue(ShowStatusIndicatorsProperty, value);
        }

        /// <summary>
        /// ShowStatusIndicators 屬性變更回呼
        /// </summary>
        private static void OnShowStatusIndicatorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame && frame.FindName("StatusIndicatorsPanel") is StackPanel panel)
            {
                panel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// BooleanToVisibilityConverter
        /// </summary>
        private static readonly System.Windows.Controls.BooleanToVisibilityConverter BooleanToVisibilityConverter = new System.Windows.Controls.BooleanToVisibilityConverter();

        /// <summary>
        /// 是否使用淺色主題 (Light Theme)
        /// </summary>
        public static readonly DependencyProperty UseLightThemeProperty =
            DependencyProperty.Register(
                nameof(UseLightTheme),
                typeof(bool),
                typeof(CyberFrame),
                new PropertyMetadata(false, OnUseLightThemeChanged));

        /// <summary>
        /// 取得或設定是否使用淺色主題
        /// </summary>
        /// <remarks>
        /// 變更此屬性會自動觸發主題切換，並通知所有相關控制項更新
        /// </remarks>
        public bool UseLightTheme
        {
            get => (bool)GetValue(UseLightThemeProperty);
            set => SetValue(UseLightThemeProperty, value);
        }

        /// <summary>
        /// UseLightTheme 屬性變更回呼
        /// </summary>
        private static void OnUseLightThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame)
            {
                frame.ApplyTheme((bool)e.NewValue);
            }
        }

        /// <summary>
        /// 是否顯示 PLC 狀態指示器
        /// </summary>
        public static readonly DependencyProperty ShowPlcStatusProperty =
            DependencyProperty.Register(
                nameof(ShowPlcStatus),
                typeof(bool),
                typeof(CyberFrame),
                new PropertyMetadata(true, OnShowPlcStatusChanged));

        public bool ShowPlcStatus
        {
            get => (bool)GetValue(ShowPlcStatusProperty);
            set => SetValue(ShowPlcStatusProperty, value);
        }

        private static void OnShowPlcStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame && frame.FindName("IntegratedPlcStatus") is PlcStatus plcStatus)
            {
                plcStatus.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 主內容區域
        /// </summary>
        public static readonly DependencyProperty MainContentProperty =
            DependencyProperty.Register(
                nameof(MainContent),
                typeof(object),
                typeof(CyberFrame),
                new PropertyMetadata(null));

        /// <summary>
        /// 取得或設定主內容區域
        /// </summary>
        public object MainContent
        {
            get => GetValue(MainContentProperty);
            set => SetValue(MainContentProperty, value);
        }

        /// <summary>
        /// PLC IP 位址
        /// </summary>
        public static readonly DependencyProperty PlcIpAddressProperty =
            DependencyProperty.Register(
                nameof(PlcIpAddress),
                typeof(string),
                typeof(CyberFrame),
                new PropertyMetadata("192.168.22.39"));

        /// <summary>
        /// 取得或設定 PLC IP 位址
        /// </summary>
        public string PlcIpAddress
        {
            get => (string)GetValue(PlcIpAddressProperty);
            set => SetValue(PlcIpAddressProperty, value);
        }


        /// <summary>
        /// PLC IP 位址
        /// </summary>
        public static readonly DependencyProperty MonitorAddressProperty =
            DependencyProperty.Register(
                nameof(MonitorAddress),
                typeof(string),
                typeof(CyberFrame),
                new PropertyMetadata(""));

        public string MonitorAddress
        {
            get => (string)GetValue(MonitorAddressProperty);
            set => SetValue(MonitorAddressProperty, value);
        }

        /// <summary>
        /// PLC 連接埠
        /// </summary>
        public static readonly DependencyProperty PlcPortProperty =
            DependencyProperty.Register(
                nameof(PlcPort),
                typeof(int),
                typeof(CyberFrame),
                new PropertyMetadata(3000));

        /// <summary>
        /// 取得或設定 PLC 連接埠
        /// </summary>
        public int PlcPort
        {
            get => (int)GetValue(PlcPortProperty);
            set => SetValue(PlcPortProperty, value);
        }

        /// <summary>
        /// PLC 掃描間隔 (ms)
        /// </summary>
        public static readonly DependencyProperty PlcScanIntervalProperty =
            DependencyProperty.Register(
                nameof(PlcScanInterval),
                typeof(int),
                typeof(CyberFrame),
                new PropertyMetadata(150));

        public int PlcScanInterval
        {
            get => (int)GetValue(PlcScanIntervalProperty);
            set => SetValue(PlcScanIntervalProperty, value);
        }

        /// <summary>
        /// 是否自動連線 PLC
        /// </summary>
        public static readonly DependencyProperty PlcAutoConnectProperty =
            DependencyProperty.Register(
                nameof(PlcAutoConnect),
                typeof(bool),
                typeof(CyberFrame),
                new PropertyMetadata(true));

        /// <summary>
        /// 取得或設定是否自動連線 PLC
        /// </summary>
        public bool PlcAutoConnect
        {
            get => (bool)GetValue(PlcAutoConnectProperty);
            set => SetValue(PlcAutoConnectProperty, value);
        }

        /// <summary>
        /// 視圖模式 (正常內容 / 使用者管理)
        /// </summary>
        public static readonly DependencyProperty ViewModeProperty =
            DependencyProperty.Register(
                nameof(ViewMode),
                typeof(CyberFrameViewMode),
                typeof(CyberFrame),
                new PropertyMetadata(CyberFrameViewMode.Normal, OnViewModeChanged));

        /// <summary>
        /// 取得或設定視圖模式
        /// </summary>
        public CyberFrameViewMode ViewMode
        {
            get => (CyberFrameViewMode)GetValue(ViewModeProperty);
            set => SetValue(ViewModeProperty, value);
        }

        /// <summary>
        /// 視圖模式變更回呼
        /// </summary>
        private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CyberFrame frame)
            {
                frame.UpdateViewMode((CyberFrameViewMode)e.NewValue);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 時鐘更新
        /// </summary>
        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 登入成功事件
        /// </summary>
        private void OnLoginSuccess(object? sender, Models.UserAccount user)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateUserInfo();

                // 🔥 新增：登入成功後自動切回首頁（避免某些角色卡在使用者管理頁面）
                if (ViewMode == CyberFrameViewMode.UserManagement)
                {
                    ViewMode = CyberFrameViewMode.Normal;
                    ComplianceContext.LogSystem(
                        $"使用者 {user.DisplayName} 登入成功，自動返回首頁",
                        LogLevel.Info,
                        showInUi: true
                    );
                }
            });
        }

        /// <summary>
        /// 登出事件
        /// </summary>
        private void OnLogoutOccurred(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateUserInfo);
        }

        /// <summary>
        /// 登出按鈕點擊
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CyberMessageBox.Show(
                "確定要登出嗎？",
                "登出確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                SecurityContext.Logout();
            }
        }

        /// <summary>
        /// 主題切換按鈕點擊事件
        /// </summary>
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("========== Theme Toggle START ==========");
            System.Diagnostics.Debug.WriteLine($"Current UseLightTheme: {UseLightTheme}");

            // 🔥 使用 ThemeManager 統一切換主題
            ToggleTheme();

            System.Diagnostics.Debug.WriteLine($"New UseLightTheme: {UseLightTheme}");
            System.Diagnostics.Debug.WriteLine("========== Theme Toggle END ==========");
        }

        /// <summary>
        /// 切換使用者管理介面按鈕點擊事件
        /// </summary>
        private void UserManagementToggleButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("========== UserManagementToggleButton_Click START ==========");

            // 檢查權限 (只有 Admin 和 Supervisor 可進入)
            var session = SecurityContext.CurrentSession;
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Current User: {session.CurrentUserName}, Level: {session.CurrentLevel}");

            if (session.CurrentLevel < AccessLevel.Supervisor)
            {
                System.Diagnostics.Debug.WriteLine("[CyberFrame] ❌ Permission denied");
                CyberMessageBox.Show(
                    "您沒有權限存取使用者管理功能\n需要 Supervisor 或 Admin 權限",
                    "權限不足",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Current ViewMode BEFORE: {ViewMode}");

            // 切換視圖模式
            ViewMode = ViewMode == CyberFrameViewMode.Normal
                ? CyberFrameViewMode.UserManagement
                : CyberFrameViewMode.Normal;

            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Current ViewMode AFTER: {ViewMode}");

            // 記錄稽核日誌
            ComplianceContext.LogSystem(
                $"使用者 {session.CurrentUserName} {(ViewMode == CyberFrameViewMode.UserManagement ? "進入" : "離開")}使用者管理介面",
                LogLevel.Info);

            System.Diagnostics.Debug.WriteLine("========== UserManagementToggleButton_Click END ==========");
        }

        /// <summary>
        /// 批次刷新開始事件處理
        /// </summary>
        private void OnBatchFlushStarted(int dataCount, int auditCount)
        {
            // ✅ 最優先輸出（確認事件有被觸發）
            Console.WriteLine($"========== OnBatchFlushStarted ==========");
            Console.WriteLine($"[CyberFrame] dataCount={dataCount}, auditCount={auditCount}");

            System.Diagnostics.Debug.WriteLine($"========== OnBatchFlushStarted ==========");
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] 批次寫入開始: {dataCount}+{auditCount}");

            // ✅ 顯示在 LiveLogViewer
            ComplianceContext.LogSystem($"🟢 批次寫入開始: {dataCount} DataLogs + {auditCount} AuditLogs",
                LogLevel.Success, showInUi: true);

            try
            {
                // ✅ 使用 Invoke 而非 InvokeAsync，確保立即執行
                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine("[CyberFrame] Dispatcher.Invoke 執行中...");

                    // 變綠色 - 寫入中
                    SetBatchWriteIndicatorColor(Colors.LimeGreen);

                    Console.WriteLine("[CyberFrame] 顏色已設定為綠色");

                    // 更新 Tooltip
                    var batchWriteIndicator = this.FindName("BatchWriteIndicator") as Border;
                    if (batchWriteIndicator != null)
                    {
                        batchWriteIndicator.ToolTip = $"批次寫入中: {dataCount} DataLogs + {auditCount} AuditLogs";
                        Console.WriteLine("[CyberFrame] Tooltip 已更新");
                    }
                    else
                    {
                        Console.WriteLine("[CyberFrame] 警告：找不到 BatchWriteIndicator 控制項！");
                        ComplianceContext.LogSystem("[CyberFrame] 警告：找不到 BatchWriteIndicator 控制項！",
                            LogLevel.Warning, showInUi: true);
                    }
                }, System.Windows.Threading.DispatcherPriority.Send); // ✅ 使用 Send 優先級，立即執行
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CyberFrame] OnBatchFlushStarted Error: {ex.Message}");
                ComplianceContext.LogSystem($"[CyberFrame] 批次寫入開始錯誤: {ex.Message}",
                    LogLevel.Error, showInUi: true);
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] OnBatchFlushStarted Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 批次刷新完成事件處理
        /// </summary>
        private void OnBatchFlushCompleted(int dataCount, int auditCount)
        {
            // ✅ 最優先輸出
            Console.WriteLine($"========== OnBatchFlushCompleted ==========");
            Console.WriteLine($"[CyberFrame] dataCount={dataCount}, auditCount={auditCount}");

            System.Diagnostics.Debug.WriteLine($"========== OnBatchFlushCompleted ==========");
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] 批次寫入完成: {dataCount}+{auditCount}");

            // ✅ 顯示在 LiveLogViewer
            ComplianceContext.LogSystem($"🔴 批次寫入完成: {dataCount} DataLogs + {auditCount} AuditLogs",
                LogLevel.Info, showInUi: true);

            try
            {
                // ✅ 延遲 500ms 再變回紅色，讓綠色更明顯
                Task.Delay(500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("[CyberFrame] Dispatcher.Invoke 執行中...");

                        // 變紅色 - 閒置
                        SetBatchWriteIndicatorColor(Colors.Red);

                        Console.WriteLine("[CyberFrame] 顏色已設定為紅色");

                        // 更新 Tooltip
                        var batchWriteIndicator = this.FindName("BatchWriteIndicator") as Border;
                        if (batchWriteIndicator != null)
                        {
                            var stats = ComplianceContext.GetBatchStatistics();
                            batchWriteIndicator.ToolTip = $"批次寫入閒置\n" +
                                $"待寫入: {stats.PendingDataLogs} DataLogs + {stats.PendingAuditLogs} AuditLogs\n" +
                                $"累計: {stats.DataLogs} + {stats.AuditLogs} = {stats.DataLogs + stats.AuditLogs} 筆";
                            Console.WriteLine("[CyberFrame] Tooltip 已更新");
                        }
                        else
                        {
                            Console.WriteLine("[CyberFrame] 警告：找不到 BatchWriteIndicator 控制項！");
                            ComplianceContext.LogSystem("[CyberFrame] 警告：找不到 BatchWriteIndicator 控制項！",
                                LogLevel.Warning, showInUi: true);
                        }
                    }, System.Windows.Threading.DispatcherPriority.Send);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CyberFrame] OnBatchFlushCompleted Error: {ex.Message}");
                ComplianceContext.LogSystem($"[CyberFrame] 批次寫入完成錯誤: {ex.Message}",
                    LogLevel.Error, showInUi: true);
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] OnBatchFlushCompleted Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定批次寫入指示燈顏色
        /// </summary>
        private void SetBatchWriteIndicatorColor(Color color)
        {
            try
            {
                Console.WriteLine($"[CyberFrame] SetBatchWriteIndicatorColor: {color}");

                // 使用 FindName 取得控制項
                var batchWriteIndicator = this.FindName("BatchWriteIndicator") as Border;

                Console.WriteLine($"[CyberFrame] BatchWriteIndicator found: {batchWriteIndicator != null}");

                if (batchWriteIndicator == null)
                {
                    Console.WriteLine("[CyberFrame] 錯誤：找不到 BatchWriteIndicator 控制項！");
                    return;
                }

                // ✅ 直接設定顏色（不使用動畫，確保立即生效）
                batchWriteIndicator.Background = new SolidColorBrush(color);
                Console.WriteLine($"[CyberFrame] 背景顏色已直接設定: {color}");

                // 更新發光效果
                if (batchWriteIndicator.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
                {
                    shadow.Color = color;
                    Console.WriteLine("[CyberFrame] 發光效果顏色已設定");
                }
                else
                {
                    Console.WriteLine("[CyberFrame] 警告：Effect 不是 DropShadowEffect");
                }

                // ✅ 強制刷新 UI
                batchWriteIndicator.InvalidateVisual();
                batchWriteIndicator.UpdateLayout();
                Console.WriteLine("[CyberFrame] UI 已強制刷新");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CyberFrame] SetBatchWriteIndicatorColor Error: {ex.Message}");
                Console.WriteLine($"[CyberFrame] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] SetBatchWriteIndicatorColor Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 更新使用者資訊顯示
        /// </summary>
        private void UpdateUserInfo()
        {
            var session = SecurityContext.CurrentSession;

            // 使用 FindName 查找控制項
            var userNameText = this.FindName("UserNameText") as TextBlock;
            var userLevelText = this.FindName("UserLevelText") as TextBlock;

            if (userNameText != null && userLevelText != null)
            {
                if (session.IsLoggedIn)
                {
                    userNameText.Text = session.CurrentUserName;
                    userLevelText.Text = session.CurrentLevel.ToString();
                }
                else
                {
                    userNameText.Text = "Guest";
                    userLevelText.Text = "Not Logged In";
                }
            }
        }

        /// <summary>
        /// 套用主題
        /// </summary>
        /// <param name="useLightTheme">是否使用淺色主題</param>
        private void ApplyTheme(bool useLightTheme)
        {
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Applying Theme: {(useLightTheme ? "Light" : "Dark")}");

            try
            {
                // 取得應用程式層級的資源字典
                var appResources = Application.Current.Resources;

                // 載入對應的主題檔案
                var themeUri = new Uri(
                    useLightTheme
                        ? "/Stackdose.UI.Core;component/Themes/LightColors.xaml"
                        : "/Stackdose.UI.Core;component/Themes/Colors.xaml",
                    UriKind.Relative);

                var newThemeDict = new ResourceDictionary { Source = themeUri };

                // 找到並移除所有包含 Colors.xaml 或 LightColors.xaml 的字典
                var toRemove = appResources.MergedDictionaries
                    .Where(d => d.Source != null &&
                               (d.Source.ToString().Contains("Colors.xaml") ||
                                d.Source.ToString().Contains("LightColors.xaml")))
                    .ToList();

                foreach (var dict in toRemove)
                {
                    appResources.MergedDictionaries.Remove(dict);
                    System.Diagnostics.Debug.WriteLine($"[CyberFrame] Removed: {dict.Source}");
                }

                // 加入新的主題字典
                appResources.MergedDictionaries.Add(newThemeDict);

                System.Diagnostics.Debug.WriteLine($"[CyberFrame] Theme Applied Successfully: {themeUri}");

                // 🔥 讀取主題顏色
                Color? bgColor = null;
                Color? fgColor = null;

                if (Application.Current?.TryFindResource("Plc.Bg.Main") is SolidColorBrush bgBrush)
                {
                    bgColor = bgBrush.Color;
                }

                if (Application.Current?.TryFindResource("Plc.Fg.Main") is SolidColorBrush fgBrush)
                {
                    fgColor = fgBrush.Color;
                }

                // 🔥 使用 ThemeManager 統一通知所有已註冊的控制項
                ThemeManager.SwitchTheme(
                    useLightTheme,
                    useLightTheme ? "Light" : "Dark",
                    bgColor,
                    fgColor
                );

                System.Diagnostics.Debug.WriteLine("[CyberFrame] ThemeManager.SwitchTheme 已呼叫");

                // 強制刷新 UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 觸發視覺樹重繪
                    foreach (Window window in Application.Current.Windows)
                    {
                        window.InvalidateVisual();
                        window.UpdateLayout();
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);

                // 記錄日誌
                ComplianceContext.LogSystem(
                    $"主題已切換為 {(useLightTheme ? "Light" : "Dark")} 模式",
                    LogLevel.Info,
                    showInUi: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] Theme Apply Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] Stack Trace: {ex.StackTrace}");
                CyberMessageBox.Show(
                    $"Theme switch failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 切換主題（公開方法，可從外部呼叫）
        /// </summary>
        public void ToggleTheme()
        {
            UseLightTheme = !UseLightTheme;
        }

        /// <summary>
        /// 刷新視覺樹中的所有 LiveLogViewer
        /// </summary>
        private void RefreshLiveLogViewers(DependencyObject parent)
        {
            if (parent == null) return;

            if (parent is LiveLogViewer liveLogViewer)
            {
                liveLogViewer.RefreshLogColors();
                return;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                RefreshLiveLogViewers(child);
            }
        }

        /// <summary>
        /// 更新視圖模式
        /// </summary>
        private void UpdateViewMode(CyberFrameViewMode mode)
        {
            System.Diagnostics.Debug.WriteLine($"========== UpdateViewMode START ==========");
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Mode: {mode}");

            // 方法1: 使用 FindName
            var normalContent = this.FindName("NormalContentPresenter") as ContentControl;
            var userManagementPanel = this.FindName("UserManagementPanel") as FrameworkElement;

            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Method1 - NormalContentPresenter: {normalContent != null}");
            System.Diagnostics.Debug.WriteLine($"[CyberFrame] Method1 - UserManagementPanel: {userManagementPanel != null}");

            // 方法2: 如果 FindName 失敗，嘗試從視覺樹搜尋
            if (normalContent == null || userManagementPanel == null)
            {
                System.Diagnostics.Debug.WriteLine("[CyberFrame] FindName failed, searching visual tree...");

                // 搜尋整個視覺樹
                var contentGrid = FindVisualChild<Grid>(this, g => g.Parent is Border);
                if (contentGrid != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[CyberFrame] Found content grid with {contentGrid.Children.Count} children");

                    foreach (var child in contentGrid.Children)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CyberFrame] Child type: {child.GetType().Name}");

                        if (child is ContentControl cc)
                        {
                            normalContent = cc;
                            System.Diagnostics.Debug.WriteLine("[CyberFrame] Found ContentControl");
                        }
                        else if (child is UserManagementPanel ump)
                        {
                            userManagementPanel = ump;
                            System.Diagnostics.Debug.WriteLine("[CyberFrame] Found UserManagementPanel");
                        }
                    }
                }
            }

            if (normalContent != null && userManagementPanel != null)
            {
                System.Diagnostics.Debug.WriteLine("[CyberFrame] Both controls found, switching view...");

                switch (mode)
                {
                    case CyberFrameViewMode.Normal:
                        normalContent.Visibility = Visibility.Visible;
                        userManagementPanel.Visibility = Visibility.Collapsed;
                        System.Diagnostics.Debug.WriteLine("[CyberFrame] ✅ Switched to Normal view");
                        System.Diagnostics.Debug.WriteLine($"[CyberFrame] Normal.Visibility = {normalContent.Visibility}");
                        System.Diagnostics.Debug.WriteLine($"[CyberFrame] UserMgmt.Visibility = {userManagementPanel.Visibility}");
                        break;

                    case CyberFrameViewMode.UserManagement:
                        normalContent.Visibility = Visibility.Collapsed;
                        userManagementPanel.Visibility = Visibility.Visible;
                        System.Diagnostics.Debug.WriteLine("[CyberFrame] ✅ Switched to UserManagement view");
                        System.Diagnostics.Debug.WriteLine($"[CyberFrame] Normal.Visibility = {normalContent.Visibility}");
                        System.Diagnostics.Debug.WriteLine($"[CyberFrame] UserMgmt.Visibility = {userManagementPanel.Visibility}");
                        break;
                }

                // 強制刷新 UI
                normalContent.InvalidateVisual();
                normalContent.UpdateLayout();
                userManagementPanel.InvalidateVisual();
                userManagementPanel.UpdateLayout();

                System.Diagnostics.Debug.WriteLine("[CyberFrame] UI refreshed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ [CyberFrame] ERROR: Cannot find view controls!");
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] NormalContent: {normalContent != null}");
                System.Diagnostics.Debug.WriteLine($"[CyberFrame] UserManagementPanel: {userManagementPanel != null}");
            }

            System.Diagnostics.Debug.WriteLine($"========== UpdateViewMode END ==========");
        }

        /// <summary>
        /// 搜尋視覺樹中的子元素
        /// </summary>
        private T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    if (predicate == null || predicate(typedChild))
                        return typedChild;
                }

                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

    

        #endregion
    }
}