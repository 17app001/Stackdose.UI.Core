using Stackdose.Abstractions.Logging;
using Stackdose.Abstractions.Models;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// PrintHead 面板項目配置（簡化版，用於 UI 綁定）
    /// </summary>
    public class PrintHeadItemConfig
    {
        public string HeadName { get; set; } = "PrintHead";
        public string ConfigFilePath { get; set; } = "";
        public bool AutoConnect { get; set; } = false;
    }

    /// <summary>
    /// PrintHead 管理面板控件
    /// 功能：管理多個 PrintHead 並提供統一的 Flash 功能
    /// </summary>
    public partial class PrintHeadPanel : UserControl
    {
        #region Fields
        
        /// <summary>
        /// 🔥 追蹤是否已初始化 PrintHead 列表
        /// </summary>
        private bool _isInitialized = false;
        
        #endregion

        #region Dependency Properties

        /// <summary>
        /// PrintHead 配置清單
        /// </summary>
        public static readonly DependencyProperty PrintHeadConfigsProperty =
            DependencyProperty.Register(
                nameof(PrintHeadConfigs),
                typeof(ObservableCollection<PrintHeadItemConfig>),
                typeof(PrintHeadPanel),
                new PropertyMetadata(null, OnPrintHeadConfigsChanged));

        public ObservableCollection<PrintHeadItemConfig> PrintHeadConfigs
        {
            get => (ObservableCollection<PrintHeadItemConfig>)GetValue(PrintHeadConfigsProperty);
            set => SetValue(PrintHeadConfigsProperty, value);
        }

        private static void OnPrintHeadConfigsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PrintHeadPanel panel)
            {
                panel.UpdatePrintHeadList();
            }
        }

        /// <summary>
        /// 預設 Flash 參數（格式：0.1,1,1,1）
        /// </summary>
        public static readonly DependencyProperty DefaultFlashParametersProperty =
            DependencyProperty.Register(
                nameof(DefaultFlashParameters),
                typeof(string),
                typeof(PrintHeadPanel),
                new PropertyMetadata("0.1,1,1,1"));

        public string DefaultFlashParameters
        {
            get => (string)GetValue(DefaultFlashParametersProperty);
            set => SetValue(DefaultFlashParametersProperty, value);
        }

        #endregion

        #region Constructor

        public PrintHeadPanel()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;           
      
        }

        #endregion

        #region Initialization

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 設定預設 Flash 參數
            FlashTimesTextBox.Text = DefaultFlashParameters;

            // 🔥 只在第一次載入時掃描並建立 PrintHead 控件
            if (!_isInitialized)
            {
                // 自動掃描並載入 PrintHead 配置
                if (PrintHeadConfigs == null || PrintHeadConfigs.Count == 0)
                {
                    AutoLoadPrintHeadConfigs();
                }
                else
                {
                    // 使用外部提供的配置
                    UpdatePrintHeadList();
                }
                
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 🔥 自動掃描 Resources 目錄中的 feiyang_head*.json 檔案並載入
        /// </summary>
        private void AutoLoadPrintHeadConfigs()
        {
            try
            {
                // 使用 ResourcePathHelper 取得所有 feiyang_head*.json 檔案
                var jsonFiles = ResourcePathHelper.GetJsonFiles(searchPattern: "feiyang_head*.json");

                if (jsonFiles.Length == 0)
                {
                    ComplianceContext.LogSystem(
                        "[PrintHeadPanel] No feiyang_head*.json files found in Resources directory",
                        LogLevel.Warning,
                        showInUi: true
                    );
                    
                    // 建立空集合
                    PrintHeadConfigs = new ObservableCollection<PrintHeadItemConfig>();
                    return;
                }

                // 建立配置集合
                var configs = new ObservableCollection<PrintHeadItemConfig>();

                // 解析每個 JSON 檔案
                foreach (var filePath in jsonFiles.OrderBy(f => f))
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileName(filePath);
                        
                        // 🔥 讀取 JSON 檔案以取得 PrintHead 名稱
                        string? jsonContent = ResourcePathHelper.ReadJsonFile(fileName);
                        
                        if (string.IsNullOrWhiteSpace(jsonContent))
                        {
                            continue;
                        }

                        // 解析 JSON 以取得 Name 欄位
                        string headName = fileName; // 預設使用檔案名稱
                        
                        try
                        {
                            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                            if (jsonDoc.RootElement.TryGetProperty("Name", out var nameProperty))
                            {
                                headName = nameProperty.GetString() ?? fileName;
                            }
                        }
                        catch
                        {
                            // 如果解析失敗，使用檔案名稱
                            headName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                        }

                        // 建立配置
                        configs.Add(new PrintHeadItemConfig
                        {
                            HeadName = headName,
                            ConfigFilePath = fileName,
                            AutoConnect = true // 預設自動連線
                        });
                    }
                    catch (Exception ex)
                    {
                        ComplianceContext.LogSystem(
                            $"[PrintHeadPanel] Failed to load {System.IO.Path.GetFileName(filePath)}: {ex.Message}",
                            LogLevel.Error,
                            showInUi: false
                        );
                    }
                }

                // 設定配置
                PrintHeadConfigs = configs;

                // 更新顯示
                UpdatePrintHeadList();

                ComplianceContext.LogSystem(
                    $"[PrintHeadPanel] Auto-loaded {configs.Count} PrintHead(s) from Resources",
                    LogLevel.Success,
                    showInUi: true
                );
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadPanel] Auto-load failed: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );

                // 建立空集合
                PrintHeadConfigs = new ObservableCollection<PrintHeadItemConfig>();
            }
        }

        /// <summary>
        /// 更新 PrintHead 列表顯示
        /// </summary>
        private void UpdatePrintHeadList()
        {
            if (PrintHeadContainer != null && PrintHeadConfigs != null)
            {
                // 🔥 只在尚未初始化時清空並重建（避免 Tab 切換時重建）
                if (!_isInitialized)
                {
                    // 清除現有控件
                    PrintHeadContainer.Children.Clear();
                    
                    // 動態建立 PrintHead 控件
                    foreach (var config in PrintHeadConfigs)
                    {
                        var printHeadStatus = new PrintHeadStatus
                        {
                            HeadName = config.HeadName,
                            ConfigFilePath = config.ConfigFilePath,
                            AutoConnect = config.AutoConnect,
                            Margin = new Thickness(0, 0, 0, 10),
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        PrintHeadContainer.Children.Add(printHeadStatus);
                    }
                }
            }
        }

        #endregion

        #region Flash PrintHead

        /// <summary>
        /// Flash 按鈕點擊事件
        /// </summary>
        private async void FlashButton_Click(object sender, RoutedEventArgs e)
        {
            // 禁用按鈕防止重複點擊
            FlashButton.IsEnabled = false;

            try
            {
                // 解析 Flash 參數
                string? flashParams = FlashTimesTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(flashParams))
                {
                    CyberMessageBox.Show(
                        "請輸入 Flash 參數！\n格式：0.1,1,1,1 (Khz)",
                        "參數錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 驗證格式
                var parts = flashParams.Split(',');
                if (parts.Length != 4)
                {
                    CyberMessageBox.Show(
                        "Flash 參數格式錯誤！\n正確格式：0.1,1,1,1 (4個數值，用逗號分隔)",
                        "參數錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 驗證每個參數是否為數字
                if (!parts.All(p => double.TryParse(p.Trim(), out _)))
                {
                    CyberMessageBox.Show(
                        "Flash 參數必須為數字！\n格式：0.1,1,1,1",
                        "參數錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // 記錄操作到合規日誌
                ComplianceContext.LogSystem(
                    $"[PrintHead] Starting Flash operation with parameters: {flashParams}",
                    LogLevel.Info,
                    showInUi: true);

                // 取得所有已連線的 PrintHead
                var connectedPrintHeads = PrintHeadContext.GetAllConnectedPrintHeads();

                if (connectedPrintHeads.Count == 0)
                {
                    CyberMessageBox.Show(
                        "沒有已連線的 PrintHead！\n請先連接 PrintHead 後再執行 Flash 操作。",
                        "無連線裝置",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    ComplianceContext.LogSystem(
                        "[PrintHead] Flash operation cancelled - No connected PrintHeads",
                        LogLevel.Warning,
                        showInUi: true);
                    return;
                }

                ComplianceContext.LogSystem(
                    $"[PrintHead] Found {connectedPrintHeads.Count} connected PrintHead(s)",
                    LogLevel.Info,
                    showInUi: true);

                // 詢問使用者確認
                var result = CyberMessageBox.Show(
                    $"確定要對 {connectedPrintHeads.Count} 個已連線的 PrintHead 執行 Flash 操作嗎？\n\n" +
                    $"參數：{flashParams} Khz\n\n" +
                    $"已連線的 PrintHead：\n{string.Join("\n", connectedPrintHeads.Select(kvp => $"  • {kvp.Key}"))}",
                    "確認 Flash 操作",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    ComplianceContext.LogSystem(
                        "[PrintHead] Flash operation cancelled by user",
                        LogLevel.Info,
                        showInUi: true);
                    return;
                }

                // 執行 Flash 操作
                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in connectedPrintHeads)
                {
                    string printHeadName = kvp.Key;
                    var printHead = kvp.Value;

                    try
                    {
                        ComplianceContext.LogSystem(
                            $"[PrintHead] Flashing {printHeadName}...",
                            LogLevel.Info,
                            showInUi: true);

                        // 執行 Flash 操作
                        bool flashSuccess = await Task.Run(async () =>
                        {
                            try
                            {
                                // 🔥 解析 Flash 參數 (格式：0.1,1,1,1)
                                // 0.1 = Frequency (kHz)
                                // 1   = WorkDuration (秒)
                                // 1   = IdleDuration (秒)
                                // 1   = Drops (液滴數)
                                
                                var paramParts = flashParams.Split(',');
                                double frequency = double.Parse(paramParts[0].Trim());
                                double workDuration = double.Parse(paramParts[1].Trim());
                                double idleDuration = double.Parse(paramParts[2].Trim());
                                byte drops = byte.Parse(paramParts[3].Trim());

                                // 🔥 建立 SpitParams 物件
                                var spitParams = new SpitParams
                                {
                                    Frequency = frequency,
                                    WorkDuration = workDuration,
                                    IdleDuration = idleDuration,
                                    Drops = drops
                                };

                                // 🔥 呼叫 PrintHead 的 Spit 方法
                                bool result = await printHead.Spit(spitParams);
                                
                                return result;
                            }
                            catch (Exception ex)
                            {
                                ComplianceContext.LogSystem(
                                    $"[PrintHead] {printHeadName} Flash error: {ex.Message}",
                                    LogLevel.Error,
                                    showInUi: false);
                                return false;
                            }
                        });

                        if (flashSuccess)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHead] {printHeadName} Flash completed",
                                LogLevel.Success,
                                showInUi: true);
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHead] {printHeadName} Flash failed",
                                LogLevel.Error,
                                showInUi: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHead] {printHeadName} Flash exception: {ex.Message}",
                            LogLevel.Error,
                            showInUi: true);
                    }
                }

                // 顯示結果
                if (failCount == 0)
                {
                    CyberMessageBox.Show(
                        $"Flash 操作完成！\n\n" +
                        $"成功：{successCount} 個 PrintHead\n" +
                        $"參數：{flashParams} Khz",
                        "操作成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    ComplianceContext.LogSystem(
                        $"[PrintHead] Flash operation completed successfully ({successCount}/{connectedPrintHeads.Count})",
                        LogLevel.Success,
                        showInUi: true);
                }
                else
                {
                    CyberMessageBox.Show(
                        $"Flash 操作完成（部分失敗）\n\n" +
                        $"成功：{successCount} 個\n" +
                        $"失敗：{failCount} 個\n" +
                        $"總計：{connectedPrintHeads.Count} 個 PrintHead",
                        "操作完成",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    ComplianceContext.LogSystem(
                        $"[PrintHead] Flash operation completed with errors ({successCount} success, {failCount} failed)",
                        LogLevel.Warning,
                        showInUi: true);
                }

                // 記錄到 Audit Trail
                ComplianceContext.LogAuditTrail(
                    deviceName: "PrintHead Flash",
                    address: "Multiple",
                    oldValue: "N/A",
                    newValue: flashParams,
                    reason: $"Flash operation by {ComplianceContext.CurrentUser} - {successCount} success, {failCount} failed",
                    showInUi: false);
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show(
                    $"Flash 操作發生錯誤：\n\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                ComplianceContext.LogSystem(
                    $"[PrintHead] Flash operation error: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true);
            }
            finally
            {
                // 重新啟用按鈕
                FlashButton.IsEnabled = true;
            }
        }

        #endregion
    }
}
