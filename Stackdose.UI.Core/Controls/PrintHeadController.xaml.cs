using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.PrintHead.Feiyang;
using System.IO;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 噴頭控制器 - 統一控制所有已連接的噴頭
    /// </summary>
    public partial class PrintHeadController : UserControl
    {
        private string _currentImagePath = string.Empty;

        public PrintHeadController()
        {
            InitializeComponent();
            
            this.Loaded += PrintHeadController_Loaded;
            this.Unloaded += PrintHeadController_Unloaded;
        }

        #region 初始化

        private void PrintHeadController_Loaded(object sender, RoutedEventArgs e)
        {
            // 訂閱 PrintHead 連線/斷線事件
            PrintHeadContext.PrintHeadConnected += OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected += OnPrintHeadDisconnected;

            // 更新連接數量顯示
            UpdateConnectedCount();
        }

        private void PrintHeadController_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消訂閱
            PrintHeadContext.PrintHeadConnected -= OnPrintHeadConnected;
            PrintHeadContext.PrintHeadDisconnected -= OnPrintHeadDisconnected;
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

        #endregion

        #region 閃噴控制 (Spit)

        private async void SpitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePrintHeads()) return;

            // 解析參數
            if (!double.TryParse(FrequencyBox.Text, out double frequency))
            {
                ShowError("Frequency 必須是有效的數字");
                return;
            }

            var spitParams = new SpitParams
            {
                Frequency = frequency,
                WorkDuration = 1.0,
                IdleDuration = 1.0,
                Drops = 1
            };

            // 禁用按鈕
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            try
            {
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Starting spit on all heads (Freq:{frequency}kHz)",
                    LogLevel.Info,
                    showInUi: true
                );

                int successCount = 0;
                int failCount = 0;

                // 對所有已連接的噴頭執行閃噴
                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        dynamic printHead = kvp.Value;
                        string name = kvp.Key;

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
                if (button != null) button.IsEnabled = true;
            }
        }

        #endregion

        #region 圖形控制 (Image)

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
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

                int successCount = 0;
                int failCount = 0;

                foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
                {
                    try
                    {
                        dynamic printHead = kvp.Value;
                        string name = kvp.Key;

                        bool result = printHead.LoadImage(_currentImagePath);

                        if (result)
                        {
                            successCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Image loaded successfully",
                                LogLevel.Success,
                                showInUi: true
                            );
                        }
                        else
                        {
                            failCount++;
                            ComplianceContext.LogSystem(
                                $"[PrintHeadController] {name}: Image load failed",
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
