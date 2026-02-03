using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Stackdose.UI.Core.Helpers;
using Stackdose.Abstractions.Logging;

namespace ModelB.Demo.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : UserControl
    {
        private DispatcherTimer? _clockTimer;

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Start clock timer for current time display
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Subscribe to ProcessContext for global status
            ProcessContext.StateChanged += OnProcessStateChanged;
            ProcessContext.BatchNumberChanged += OnBatchNumberChanged;
            ProcessContext.BatchIdChanged += OnBatchIdChanged;  // ?? 新增：訂閱 BatchId 變更
            ProcessContext.CountChanged += OnCountChanged;

            // Subscribe to PlcLabelContext for PLC value updates
            PlcLabelContext.ValueChanged += OnPlcValueChanged;

            // Sync current state from ProcessContext
            SyncFromProcessContext();

            // ?? 移除重複的日誌記錄（只在 Debug 輸出）
            System.Diagnostics.Debug.WriteLine("[MainPage] Loaded, monitoring global status...");
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Stop timer
            _clockTimer?.Stop();
            _clockTimer = null;

            // Unsubscribe events
            ProcessContext.StateChanged -= OnProcessStateChanged;
            ProcessContext.BatchNumberChanged -= OnBatchNumberChanged;
            ProcessContext.BatchIdChanged -= OnBatchIdChanged;  // ?? 新增：取消訂閱
            ProcessContext.CountChanged -= OnCountChanged;
            PlcLabelContext.ValueChanged -= OnPlcValueChanged;
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            // Update current time
            CurrentTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Update running time from ProcessContext
            if (ProcessContext.CurrentState == Stackdose.UI.Core.Controls.ProcessState.Running)
            {
                RunningTimeText.Text = ProcessContext.RunningTime.ToString(@"hh\:mm\:ss");
            }
        }

        /// <summary>
        /// Sync UI from ProcessContext on page load
        /// </summary>
        private void SyncFromProcessContext()
        {
            try
            {
                // Sync state
                GlobalProcessStatus.ProcessState = ProcessContext.CurrentState;
                
                // ?? 修正：優先使用 BatchId（完整字串），否則使用 BatchNumber
                if (!string.IsNullOrEmpty(ProcessContext.BatchId))
                {
                    BatchNumberText.Text = ProcessContext.BatchId;
                }
                else if (ProcessContext.BatchNumber > 0)
                {
                    BatchNumberText.Text = ProcessContext.BatchNumber.ToString();
                }
                else
                {
                    BatchNumberText.Text = "-";
                }
                
                // Sync counts
                CompletedCountText.Text = ProcessContext.CompletedCount.ToString();
                DefectCountText.Text = ProcessContext.DefectCount.ToString();
                
                // Sync device status
                if (PlcContext.GlobalStatus?.CurrentManager?.IsConnected == true)
                {
                    DeviceStatusText.Text = "Online";
                    DeviceStatusText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Success") 
                        ?? System.Windows.Media.Brushes.Green;
                }
                else
                {
                    DeviceStatusText.Text = "Not Initialized";
                    DeviceStatusText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Warning") 
                        ?? System.Windows.Media.Brushes.Orange;
                }
                
                UpdateYieldRate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] SyncFromProcessContext error: {ex.Message}");
            }
        }

        private void OnProcessStateChanged(Stackdose.UI.Core.Controls.ProcessState state)
        {
            Dispatcher.Invoke(() =>
            {
                GlobalProcessStatus.ProcessState = state;
                
                if (state == Stackdose.UI.Core.Controls.ProcessState.Running)
                {
                    DeviceStatusText.Text = "Running";
                    DeviceStatusText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Success") 
                        ?? System.Windows.Media.Brushes.Green;
                }
                else if (state == Stackdose.UI.Core.Controls.ProcessState.Idle)
                {
                    DeviceStatusText.Text = "Idle";
                    DeviceStatusText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Info") 
                        ?? System.Windows.Media.Brushes.Cyan;
                }
            });
        }

        private void OnBatchNumberChanged(int batchNumber)
        {
            // ?? 當 BatchId 有值時，優先使用 BatchId
            if (!string.IsNullOrEmpty(ProcessContext.BatchId)) return;
            
            Dispatcher.Invoke(() =>
            {
                BatchNumberText.Text = batchNumber > 0 ? batchNumber.ToString() : "-";
            });
        }

        /// <summary>
        /// ?? 新增：BatchId 變更事件處理
        /// </summary>
        private void OnBatchIdChanged(string batchId)
        {
            Dispatcher.Invoke(() =>
            {
                BatchNumberText.Text = !string.IsNullOrEmpty(batchId) ? batchId : "-";
            });
        }

        private void OnCountChanged(int completed, int defect)
        {
            Dispatcher.Invoke(() =>
            {
                CompletedCountText.Text = completed.ToString();
                DefectCountText.Text = defect.ToString();
                UpdateYieldRate();
            });
        }

        private void OnPlcValueChanged(object? sender, PlcLabelValueChangedEventArgs e)
        {
            try
            {
                // Monitor batch number from D103 (also update ProcessContext)
                if (e.Address == "D103" || e.Label == "Batch No")
                {
                    if (e.Value is int batchNo && batchNo > 0)
                    {
                        ProcessContext.BatchNumber = batchNo;
                    }
                }

                // Monitor completed count from D200 (also update ProcessContext)
                if (e.Address == "D200" || e.Label == "Completed Count")
                {
                    if (e.Value is int completed)
                    {
                        ProcessContext.CompletedCount = completed;
                    }
                }

                // Monitor defect count from D201 (also update ProcessContext)
                if (e.Address == "D201" || e.Label == "Defect Count")
                {
                    if (e.Value is int defect)
                    {
                        ProcessContext.DefectCount = defect;
                    }
                }

                // Monitor process state from M0 (also update ProcessContext)
                if ((e.Address == "M0" || e.Label == "Process Running") && e.Value is bool isRunning)
                {
                    ProcessContext.CurrentState = isRunning 
                        ? Stackdose.UI.Core.Controls.ProcessState.Running 
                        : Stackdose.UI.Core.Controls.ProcessState.Idle;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] OnPlcValueChanged error: {ex.Message}");
            }
        }

        private void UpdateYieldRate()
        {
            try
            {
                double yieldRate = ProcessContext.YieldRate;
                YieldRateText.Text = $"{yieldRate:F2} %";
                
                // Change color based on yield rate
                if (yieldRate >= 95.0)
                {
                    YieldRateText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Success") 
                        ?? System.Windows.Media.Brushes.Green;
                }
                else if (yieldRate >= 85.0)
                {
                    YieldRateText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Warning") 
                        ?? System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    YieldRateText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Error") 
                        ?? System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] UpdateYieldRate error: {ex.Message}");
            }
        }
    }
}
