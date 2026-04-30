using Microsoft.Win32;
using Stackdose.Abstractions.Logging;
using Stackdose.Abstractions.Models;
using Stackdose.Abstractions.Print;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Stackdose.Abstractions.Hardware;


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

        #region Dependency Properties

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
                ctrl.HandleMachineSwitch(e.OldValue as string, e.NewValue as string);
        }

        /// <summary>
        /// 傳圖完成後通知 PLC 的裝置位址，e.g. "D513"。空字串表示不通知。
        /// </summary>
        public static readonly DependencyProperty PlcReadyDeviceProperty =
            DependencyProperty.Register(nameof(PlcReadyDevice), typeof(string), typeof(PrintHeadController), new PropertyMetadata(string.Empty));

        public string PlcReadyDevice
        {
            get => (string)GetValue(PlcReadyDeviceProperty);
            set => SetValue(PlcReadyDeviceProperty, value);
        }

        /// <summary>
        /// 列印方向寫入的 PLC D-register，e.g. "D32"。
        /// 值：0=雙向、1=左到右、2=右到左。空字串表示不寫。
        /// </summary>
        public static readonly DependencyProperty DirectionPlcDeviceProperty =
            DependencyProperty.Register(nameof(DirectionPlcDevice), typeof(string), typeof(PrintHeadController), new PropertyMetadata(string.Empty));

        public string DirectionPlcDevice
        {
            get => (string)GetValue(DirectionPlcDeviceProperty);
            set => SetValue(DirectionPlcDeviceProperty, value);
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
            if (!string.IsNullOrWhiteSpace(oldMachineId))
                SaveCurrentState(oldMachineId);

            _activeMachineId = newMachineId;

            if (!string.IsNullOrWhiteSpace(newMachineId) && _machineStates.TryGetValue(newMachineId, out var state))
                RestoreState(state);
            else
                ResetUiToDefault();
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
                ImageInfoText.Text = "尚未載入圖片";
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
            PrintHeadContext.PrintHeadConnected += OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected += OnPrintHeadDisconnected;
            SecurityContext.AccessLevelChanged += OnAccessLevelChanged;

            UpdateConnectedCount();
            UpdateButtonPermissions();
        }

        private void PrintHeadController_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_activeMachineId))
                SaveCurrentState(_activeMachineId);

            PrintHeadContext.PrintHeadConnected -= OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected -= OnPrintHeadDisconnected;
            SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
        }

        private void OnPrintHeadConnected(string name) => Dispatcher.Invoke(UpdateConnectedCount);
        private void OnPrintHeadDisconnected(string name) => Dispatcher.Invoke(UpdateConnectedCount);

        private void UpdateConnectedCount()
        {
            int count = PrintHeadContext.ConnectedPrintHeads.Count;
            ConnectedCountText.Text = $"{count} Connected";
        }

        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateButtonPermissions);
        }

        private void UpdateButtonPermissions()
        {
            bool hasAccess = SecurityContext.HasAccess(AccessLevel.Admin);

            BrowseImageButton.IsEnabled = hasAccess;
            LoadImageButton.IsEnabled = hasAccess;
            CancelTaskButton.IsEnabled = hasAccess;

            string tooltip = hasAccess
                ? string.Empty
                : $"需要 Admin 權限\n目前權限: {SecurityContext.CurrentSession.CurrentLevel}";

            BrowseImageButton.ToolTip = hasAccess ? "讀取圖片" : tooltip;
            LoadImageButton.ToolTip   = hasAccess ? "載入任務" : tooltip;
            CancelTaskButton.ToolTip  = hasAccess ? "取消任務" : tooltip;
        }

        #endregion

        private void EncoderReset_Click(object sender, EventArgs e)
        {
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    "[PrintHeadController] Starting Encoder Reset on all heads (default:1000)",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        bool result = kvp.Value.GratingReset(1000);
                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Encoder Reset successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Encoder Reset failed",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: Encoder Reset error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Encoder Reset completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        public async Task Spit(string? overrideParams = null)
        {
            await ExecuteSpitAsync(string.IsNullOrWhiteSpace(overrideParams) ? FrequencyBox.Text : overrideParams, null);
        }

        private async void SpitButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSpitAsync(FrequencyBox.Text, sender as Button);
        }

        #region 閃噴控制 (Spit)

        private async Task ExecuteSpitAsync(string frequencyString, Button? sourceButton = null)
        {
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;

            if (!PrintHeadContext.TryParseSpitParams(frequencyString, out var spitParams) || spitParams == null)
            {
                ShowError("Frequency 必須是 4 個數字 (ex. 0.1,1,1,1)");
                return;
            }

            if (sourceButton != null) sourceButton.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Starting spit on all heads (Freq:{spitParams.Frequency}kHz)",
                    LogLevel.Info,
                    showInUi: true
                );

                var (successCount, failCount) = await PrintHeadContext.SpitAllAsync(spitParams);

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Spit completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );
            }
            finally
            {
                if (sourceButton != null) sourceButton.IsEnabled = true;
            }
        }

        #endregion

        #region 圖形控制 (Image)

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.UpdateActivity();

            if (!SecurityContext.CheckAccess(AccessLevel.Admin, "讀取圖片"))
                return;

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
                    ImageWidthText.Text = image.Width.ToString();
                    ImageHeightText.Text = image.Height.ToString();
                    XDpiText.Text = ((int)image.HorizontalResolution).ToString();
                    YDpiText.Text = ((int)image.VerticalResolution).ToString();
                    FilePathText.Text = Path.GetFileName(imagePath);

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
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
                ImageInfoText.Text = "圖片載入失敗";
                ImageInfoText.Visibility = Visibility.Visible;

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Failed to read image info: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
            }
        }

        private async void CancelTaskButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.UpdateActivity();

            if (!ValidatePrintHeads()) return;

            try
            {
                ComplianceContext.LogSystem(
                    "[PrintHeadController] Aborting image transfer on all heads",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        bool ok = await kvp.Value.StopPrintAsync();
                        if (ok)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Task aborted successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                            kvp.Value.SetState(PrintHeadConnectionState.Ready);
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Task abort failed",
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

        private async void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.UpdateActivity();

            if (!SecurityContext.CheckAccess(AccessLevel.Admin, "載入任務"))
                return;

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
                if (!float.TryParse(StartXBox.Text, out float startX)) startX = 0;
                if (!float.TryParse(CaliMMBox.Text, out float caliMM)) caliMM = 0;
                int imageDpi = int.TryParse(XDpiText.Text, out int d) && d > 0 ? d : 0;
                int directionValue = DirectionCombo.SelectedIndex; // 0=雙向 1=左到右 2=右到左

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Loading image '{_currentImagePath}' (DPI:{imageDpi}, Dir:{directionValue})",
                    LogLevel.Info,
                    showInUi: true
                );

                // 1. 方向寫入 PLC（傳圖前先設定）
                if (!string.IsNullOrWhiteSpace(DirectionPlcDevice))
                    await WritePlcAsync($"{DirectionPlcDevice}={directionValue}", $"Direction={directionValue}");

                using var bitmap = new System.Drawing.Bitmap(_currentImagePath);

                var connectedHeads = PrintHeadContext.GetAllConnectedPrintHeads().ToList();
                int totalHeads = connectedHeads.Count;
                int successCount = 0;
                int failCount = 0;

                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    TransferProgressBar.Value = 0;
                });

                for (int i = 0; i < totalHeads; i++)
                {
                    var kvp = connectedHeads[i];
                    int headIndex = i;

                    Action<int> progressHandler = (progress) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            double overallProgress = ((double)headIndex + (progress / 100.0)) / totalHeads * 100.0;
                            TransferProgressBar.Value = overallProgress;
                        });
                    };

                    kvp.Value.ProgressChanged += progressHandler;

                    try
                    {
                        // 2. 每次傳圖前設定 DPI
                        if (imageDpi > 0)
                        {
                            var (cfgOk, cfgMsg) = await kvp.Value.ConfigurePrintModeAsync(imageDpi);
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: ConfigureDPI({imageDpi}) → {(cfgOk ? "OK" : cfgMsg)}",
                                cfgOk ? LogLevel.Info : LogLevel.Warning,
                                showInUi: !cfgOk
                            );
                        }

                        // 3. 傳圖 + 啟動
                        var (result, msg) = await kvp.Value.TransferBitmapAsync(bitmap, startX, caliMM);
                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Image transferred successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                            await kvp.Value.StartPrintAsync();
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {kvp.Key}: Transfer failed - {msg}",
                                LogLevel.Error,
                                showInUi: true
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ComplianceContext.LogSystem(
                            $"[PrintHeadController] {kvp.Key}: Transfer error - {ex.Message}",
                            LogLevel.Error,
                            showInUi: true
                        );
                    }
                    finally
                    {
                        kvp.Value.ProgressChanged -= progressHandler;
                        Dispatcher.Invoke(() =>
                        {
                            TransferProgressBar.Value = (double)(headIndex + 1) / totalHeads * 100.0;
                        });
                    }
                }

                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Transfer completed: {successCount} success, {failCount} failed",
                    successCount > 0 ? LogLevel.Success : LogLevel.Error,
                    showInUi: true
                );

                // 4. 通知 PLC 傳圖完畢（全部成功才寫）
                if (successCount > 0 && !string.IsNullOrWhiteSpace(PlcReadyDevice))
                    await WritePlcAsync($"{PlcReadyDevice}=1", "ImageReady");

                await Task.Delay(1000);
                Dispatcher.Invoke(() => ProgressPanel.Visibility = Visibility.Collapsed);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        #endregion

        #region 輔助方法

        private async Task WritePlcAsync(string deviceInput, string label)
        {
            var manager = PlcContext.GlobalStatus?.CurrentManager;
            if (manager == null || !manager.IsConnected)
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] PLC 未連線，跳過寫入 {label}",
                    LogLevel.Warning, showInUi: false);
                return;
            }
            bool ok = await manager.WriteAsync(deviceInput);
            ComplianceContext.LogSystem(
                $"[PrintHeadController] PLC {label}: {deviceInput} → {(ok ? "OK" : "FAIL")}",
                ok ? LogLevel.Info : LogLevel.Warning, showInUi: false);
        }

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
