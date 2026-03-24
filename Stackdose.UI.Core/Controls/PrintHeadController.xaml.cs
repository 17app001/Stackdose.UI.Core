using Microsoft.Win32;
using Stackdose.Abstractions.Logging;
using Stackdose.Abstractions.Models;
using Stackdose.Abstractions.Print;
using Stackdose.PrintHead.Feiyang;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 各機台獨立的 UI 狀態快照
    /// </summary>
    internal sealed class MachineUiState
    {
        public string ImagePath { get; set; } = string.Empty;
        public BitmapImage? PreviewSource { get; set; }
        public string Frequency { get; set; } = "0.1,1,1,1";
        public string StartX { get; set; } = "50";
        public string CaliMM { get; set; } = "30.5";
        public int DirectionIndex { get; set; } = 0;
        public string FilePathDisplay { get; set; } = "尚未選擇檔案";
        public string ImageWidth { get; set; } = "-";
        public string ImageHeight { get; set; } = "-";
        public string XDpi { get; set; } = "-";
        public string YDpi { get; set; } = "-";
        public bool HasImage { get; set; } = false;
    }

    /// <summary>
    /// 噴頭控制器 - 統一控制所有已連接的噴頭
    /// </summary>
    public partial class PrintHeadController : UserControl
    {
        private string _currentImagePath = string.Empty;

        /// <summary>
        /// 各機台的 UI 狀態快照 (machineId → state)
        /// </summary>
        private readonly Dictionary<string, MachineUiState> _machineStates = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 目前正在顯示的機台 ID（用於存回狀態）
        /// </summary>
        private string? _activeMachineId;

        #region MachineId Dependency Property

        public static readonly DependencyProperty MachineIdProperty =
            DependencyProperty.Register(
                nameof(MachineId),
                typeof(string),
                typeof(PrintHeadController),
                new PropertyMetadata(null, OnMachineIdChanged));

        /// <summary>
        /// 目前操作的機台 ID，切換時自動儲存/還原 UI 狀態
        /// </summary>
        public string MachineId
        {
            get => (string)GetValue(MachineIdProperty);
            set => SetValue(MachineIdProperty, value);
        }

        private static void OnMachineIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PrintHeadController ctrl)
            {
                ctrl.HandleMachineSwitch(e.OldValue as string, e.NewValue as string);
            }
        }

        #endregion

        public PrintHeadController()
        {
            InitializeComponent();

            this.Loaded += PrintHeadController_Loaded;
            this.Unloaded += PrintHeadController_Unloaded;
        }

        #region Machine Switch (Save / Restore)

        private void HandleMachineSwitch(string? oldMachineId, string? newMachineId)
        {
            // 1. Save current UI state for old machine
            if (!string.IsNullOrWhiteSpace(oldMachineId))
            {
                SaveCurrentState(oldMachineId);
            }

            _activeMachineId = newMachineId;

            // 2. Restore UI state for new machine (or reset to default)
            if (!string.IsNullOrWhiteSpace(newMachineId) && _machineStates.TryGetValue(newMachineId, out var state))
            {
                RestoreState(state);
            }
            else
            {
                ResetUiToDefault();
            }
        }

        private void SaveCurrentState(string machineId)
        {
            if (!_machineStates.TryGetValue(machineId, out var state))
            {
                state = new MachineUiState();
                _machineStates[machineId] = state;
            }

            state.ImagePath = _currentImagePath;
            state.PreviewSource = PreviewImage.Source as BitmapImage;
            state.Frequency = FrequencyBox.Text;
            state.StartX = StartXBox.Text;
            state.CaliMM = CaliMMBox.Text;
            state.DirectionIndex = DirectionCombo.SelectedIndex;
            state.FilePathDisplay = FilePathText.Text;
            state.ImageWidth = ImageWidthText.Text;
            state.ImageHeight = ImageHeightText.Text;
            state.XDpi = XDpiText.Text;
            state.YDpi = YDpiText.Text;
            state.HasImage = PreviewImage.Source != null;
        }

        private void RestoreState(MachineUiState state)
        {
            _currentImagePath = state.ImagePath;
            PreviewImage.Source = state.PreviewSource;
            FrequencyBox.Text = state.Frequency;
            StartXBox.Text = state.StartX;
            CaliMMBox.Text = state.CaliMM;
            DirectionCombo.SelectedIndex = state.DirectionIndex;
            FilePathText.Text = state.FilePathDisplay;
            ImageWidthText.Text = state.ImageWidth;
            ImageHeightText.Text = state.ImageHeight;
            XDpiText.Text = state.XDpi;
            YDpiText.Text = state.YDpi;
            ImageInfoText.Visibility = state.HasImage ? Visibility.Collapsed : Visibility.Visible;
            if (!state.HasImage)
            {
                ImageInfoText.Text = "尚未載入圖片";
            }
        }

        private void ResetUiToDefault()
        {
            _currentImagePath = string.Empty;
            PreviewImage.Source = null;
            FrequencyBox.Text = "0.1,1,1,1";
            StartXBox.Text = "50";
            CaliMMBox.Text = "30.5";
            DirectionCombo.SelectedIndex = 0;
            FilePathText.Text = "尚未選擇檔案";
            ImageWidthText.Text = "-";
            ImageHeightText.Text = "-";
            XDpiText.Text = "-";
            YDpiText.Text = "-";
            ImageInfoText.Text = "尚未載入圖片";
            ImageInfoText.Visibility = Visibility.Visible;
        }

        #endregion

        #region 初始化

        private void PrintHeadController_Loaded(object sender, RoutedEventArgs e)
        {
            // 訂閱 PrintHead 連線/斷線事件
            PrintHeadContext.PrintHeadConnected += OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected += OnPrintHeadDisconnected;

            // 訂閱權限變更事件
            SecurityContext.AccessLevelChanged += OnAccessLevelChanged;

            // 更新連接數量顯示
            UpdateConnectedCount();

            // 更新按鈕權限狀態
            UpdateButtonPermissions();
        }

        private void PrintHeadController_Unloaded(object sender, RoutedEventArgs e)
        {
            // 切換離開前先存檔
            if (!string.IsNullOrWhiteSpace(_activeMachineId))
            {
                SaveCurrentState(_activeMachineId);
            }

            // 取消訂閱
            PrintHeadContext.PrintHeadConnected -= OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected -= OnPrintHeadDisconnected;
            SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
        }

        private void OnPrintHeadConnected(string name)
        {
            Dispatcher.Invoke(() => UpdateConnectedCount());
        }

        private void OnPrintHeadDisconnected(string name)
        {
            Dispatcher.Invoke(() => UpdateConnectedCount());
        }

        private void UpdateConnectedCount()
        {
            int count = PrintHeadContext.ConnectedPrintHeads.Count;
            ConnectedCountText.Text = $"{count} Connected";
        }

        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateButtonPermissions);
        }

        /// <summary>
        /// 更新按鈕權限狀態
        /// </summary>
        private void UpdateButtonPermissions()
        {
            bool hasAdminAccess = SecurityContext.HasAccess(AccessLevel.Admin);

            // 圖片操作按鈕只有 Engineer 可以使用
            BrowseImageButton.IsEnabled = hasAdminAccess;
            LoadImageButton.IsEnabled = hasAdminAccess;
            CancelTaskButton.IsEnabled = hasAdminAccess;

            // 設置提示文字
            if (!hasAdminAccess)
            {
                string tooltip = $"需要 Engineer 權限\n目前權限: {SecurityContext.CurrentSession.CurrentLevel}";
                BrowseImageButton.ToolTip = tooltip;
                LoadImageButton.ToolTip = tooltip;
                CancelTaskButton.ToolTip = tooltip;
            }
            else
            {
                BrowseImageButton.ToolTip = "讀取圖片";
                LoadImageButton.ToolTip = "載入任務";
                CancelTaskButton.ToolTip = "取消任務";
            }
        }

        #endregion

        private void EncoderReset_Click(object sender, EventArgs e)
        {
            // 更新活動時間
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Starting Encoder Reset on all heads (default:1000)",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                // 對所有已連接的噴头執行閃噴
                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        // ⭐ 強制轉型為實際類型，方便 Debug 和 IntelliSense
                        if (kvp.Value is not FeiyangPrintHead printHead)
                        {
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Invalid PrintHead type",
                                LogLevel.Error,
                                showInUi: true
                            );
                            failCount++;
                            continue;
                        }

                        string name = kvp.Key;

                        // 現在可以正常 Debug，有完整的 IntelliSense 支援
                        bool result = printHead.GratingReset(1000);

                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}:Encoder Reset successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: :Encoder Rese failed",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: :Encoder Rese error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] :Encoder Rese completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            finally
            {
                // 重新啟用按鈕
                if (button != null) button.IsEnabled = true;
            }
        }


        public async Task Spit(string? overrideParams = null)
        {
            if (string.IsNullOrWhiteSpace(overrideParams))
            {
                overrideParams = FrequencyBox.Text;
            }

            await ExecuteSpitAsync(overrideParams, null);
        }

        private async void SpitButton_Click(object sender, RoutedEventArgs e)
        {
            // 取得按鈕實例
            var button = sender as Button;

            // 取得 UI 上的設定值
            string uiParams = FrequencyBox.Text;

            // 呼叫核心邏輯
            await ExecuteSpitAsync(uiParams, button);
        }


        #region 閃噴控制 (Spit)

        private async Task ExecuteSpitAsync(string frequencyString, Button? sourceButton = null)
        {
            // 更新活動時間
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;


            var parts = frequencyString?.Trim().Split(',');
            if (parts == null || parts.Length != 4)
            {
                ShowError("Frequency 必須是 4 個數字 (ex. 0.1,1,1,1)");
                return;
            }

            //double[] dataParams = frequencyText.Select(v => double.Parse(v.Trim())).ToArray();

            if (!double.TryParse(parts[0].Trim(), out double frequency) ||
                !double.TryParse(parts[1].Trim(), out double workDuration) ||
                !double.TryParse(parts[2].Trim(), out double idleDuration) ||
                !byte.TryParse(parts[3].Trim(), out byte drops))
            {
                ShowError("Frequency 必須是有效的數字 (ex. 0.1,1,1,1)");
                return;
            }

            var spitParams = new SpitParams
            {
                Frequency = frequency,
                WorkDuration = workDuration,
                IdleDuration = idleDuration,
                Drops = drops
            };


            // 禁用按鈕

            if (sourceButton != null) sourceButton.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Starting spit on all heads (Freq:{frequency}kHz)",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                // 對所有已連接的噴头執行閃噴
                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        // ⭐ 強制轉型為實際類型，方便 Debug 和 IntelliSense
                        if (kvp.Value is not FeiyangPrintHead printHead)
                        {
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Invalid PrintHead type",
                                LogLevel.Error,
                                showInUi: true
                            );
                            failCount++;
                            continue;
                        }

                        string name = kvp.Key;

                        // 現在可以正常 Debug，有完整的 IntelliSense 支援
                        bool result = await printHead.Spit(spitParams);

                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Spit started successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Spit failed",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: Spit error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Spit completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            finally
            {
                // 重新啟用按鈕
                if (sourceButton != null) sourceButton.IsEnabled = true;
            }
        }

        #endregion

        #region 圖形控制 (Image)

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            // 更新活動時間
            SecurityContext.UpdateActivity();

            // 檢查權限
            if (!SecurityContext.CheckAccess(AccessLevel.Admin, "讀取圖片"))
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "圖片檔案 (*.bmp;*.png;*.jpg)|*.bmp;*.png;*.jpg|所有檔案 (*.*)|*.*",
                Title = "選擇圖片檔案"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentImagePath = dialog.FileName;
                LoadImageInfo(dialog.FileName);
            }
        }

        private void LoadImageInfo(string imagePath)
        {
            try
            {
                using (var image = System.Drawing.Image.FromFile(imagePath))
                {
                    // Update dimensions
                    ImageWidthText.Text = image.Width.ToString();
                    ImageHeightText.Text = image.Height.ToString();

                    // Update DPI
                    XDpiText.Text = ((int)image.HorizontalResolution).ToString();
                    YDpiText.Text = ((int)image.VerticalResolution).ToString();

                    // Update file info (底部狀態列)
                    FilePathText.Text = $"{Path.GetFileName(imagePath)}";

                    // Load preview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;

                    // ? 圖片載入成功後隱藏提示文字
                    ImageInfoText.Visibility = Visibility.Collapsed;

                    ComplianceContext.LogSystem(
                        $"[PrintHeadController] Image loaded: {Path.GetFileName(imagePath)} ({image.Width}×{image.Height}, {(int)image.HorizontalResolution} DPI)",
                        LogLevel.Info,
                        showInUi: false
                    );
                }
            }
            catch (Exception ex)
            {
                ImageWidthText.Text = "-";
                ImageHeightText.Text = "-";
                XDpiText.Text = "-";
                YDpiText.Text = "-";
                FilePathText.Text = "尚未選擇檔案";
                PreviewImage.Source = null;

                // ? 載入失敗時顯示錯誤提示
                ImageInfoText.Text = "圖片載入失敗";
                ImageInfoText.Visibility = Visibility.Visible;

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Failed to read image info: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
            }
        }

        private void CancelTaskButton_Click(object sender, RoutedEventArgs e)
        {
            // 更新活動時間
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Aborting image transfer on all heads",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        if (kvp.Value is not FeiyangPrintHead printHead)
                        {
                            failCount++;
                            continue;
                        }

                        string name = kvp.Key;
                        bool ok = printHead.StopPrint();

                        if (ok)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Task aborted successfully",
                                LogLevel.Success,
                                showInUi: true
                            );

                            printHead.SetState(PrintHeadConnectionState.Ready);
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Task abort failed",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: Abort error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Abort completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            catch (Exception ex)
            {
                ShowError($"取消任務時發生錯誤: {ex.Message}");
            }
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            // 更新活動時間
            SecurityContext.UpdateActivity();

            // 檢查權限
            if (!SecurityContext.CheckAccess(AccessLevel.Admin, "載入任務"))
            {
                return;
            }

            if (!ValidatePrintHeads()) return;

            if (string.IsNullOrWhiteSpace(_currentImagePath))
            {
                ShowError("請先選擇圖片檔案");
                return;
            }

            if (!File.Exists(_currentImagePath))
            {
                ShowError($"圖片檔案不存在: {_currentImagePath}");
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Loading image '{_currentImagePath}' to all heads",
                    LogLevel.Info,
                    showInUi: true
                );

                // ⭐ 修正 1：解析 UI 上的列印參數
                if (!float.TryParse(StartXBox.Text, out float startX)) startX = 0;
                if (!float.TryParse(CaliMMBox.Text, out float caliMM)) caliMM = 0;

                // ⭐ 修正 2：讀取圖片物件 (注意：System.Drawing.Bitmap 需要正確釋放)
                using var bitmap = new System.Drawing.Bitmap(_currentImagePath);

                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        // ⭐ 強制轉型為實際類型，方便 Debug 和 IntelliSense
                        if (kvp.Value is not FeiyangPrintHead printHead)
                        {
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Invalid PrintHead type",
                                LogLevel.Error,
                                showInUi: true
                            );
                            failCount++;
                            continue;
                        }

                        string name = kvp.Key;

                        // ⭐ 修正 3：呼叫實際具有傳輸圖片功能的 TransferBitmap 方法
                        var (result, msg) = printHead.TransferBitmap(bitmap, startX, caliMM);

                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Image loaded successfully",
                                LogLevel.Success,
                                showInUi: true
                            );

                            printHead.StartPrint();
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Image load failed - {msg}",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: Image load error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Image load completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        #endregion

        #region 輔助方法

        private bool ValidatePrintHeads()
        {
            if (!PrintHeadContext.HasConnectedPrintHead)
            {
                ShowError("沒有已連接的噴頭");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            ComplianceContext.LogSystem(
                $"[PrintHeadController] Error: {message}",
                LogLevel.Error,
                showInUi: true
            );

            MessageBox.Show(message, "PrintHead Controller", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion
    }
}
