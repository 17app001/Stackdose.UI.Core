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
        private DateTime _processStartTime;
        private bool _isProcessRunning = false;

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

            // Subscribe to PlcLabelContext for global status updates
            PlcLabelContext.ValueChanged += OnPlcValueChanged;

            ComplianceContext.LogSystem("MainPage loaded, monitoring global status...", LogLevel.Info);
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Stop timer
            _clockTimer?.Stop();
            _clockTimer = null;

            // Unsubscribe events
            PlcLabelContext.ValueChanged -= OnPlcValueChanged;
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            // Update current time
            CurrentTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Update running time if process is running
            if (_isProcessRunning)
            {
                TimeSpan runningTime = DateTime.Now - _processStartTime;
                RunningTimeText.Text = runningTime.ToString(@"hh\:mm\:ss");
            }
        }

        private void OnPlcValueChanged(object? sender, PlcLabelValueChangedEventArgs e)
        {
            try
            {
                // Monitor batch number from D103
                if (e.Address == "D103" || e.Label == "Batch No")
                {
                    Dispatcher.Invoke(() =>
                    {
                        BatchNumberText.Text = e.Value?.ToString() ?? "-";
                    });
                }

                // Monitor completed count (假設從 D200 讀取)
                if (e.Address == "D200" || e.Label == "Completed Count")
                {
                    Dispatcher.Invoke(() =>
                    {
                        CompletedCountText.Text = e.Value?.ToString() ?? "0";
                        UpdateYieldRate();
                    });
                }

                // Monitor defect count (假設從 D201 讀取)
                if (e.Address == "D201" || e.Label == "Defect Count")
                {
                    Dispatcher.Invoke(() =>
                    {
                        DefectCountText.Text = e.Value?.ToString() ?? "0";
                        UpdateYieldRate();
                    });
                }

                // Monitor process state (假設從 M0 讀取)
                if ((e.Address == "M0" || e.Label == "Process Running") && e.Value is bool isRunning)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (isRunning && !_isProcessRunning)
                        {
                            // Process started
                            _isProcessRunning = true;
                            _processStartTime = DateTime.Now;
                            GlobalProcessStatus.ProcessState = Stackdose.UI.Core.Controls.ProcessState.Running;
                            ComplianceContext.LogSystem("Process started on MainPage", LogLevel.Success);
                        }
                        else if (!isRunning && _isProcessRunning)
                        {
                            // Process stopped
                            _isProcessRunning = false;
                            GlobalProcessStatus.ProcessState = Stackdose.UI.Core.Controls.ProcessState.Idle;
                            ComplianceContext.LogSystem("Process stopped on MainPage", LogLevel.Warning);
                        }
                    });
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
                if (int.TryParse(CompletedCountText.Text, out int completed) &&
                    int.TryParse(DefectCountText.Text, out int defect))
                {
                    int total = completed + defect;
                    if (total > 0)
                    {
                        double yieldRate = (double)completed / total * 100.0;
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
                    else
                    {
                        YieldRateText.Text = "100.00 %";
                        YieldRateText.Foreground = (System.Windows.Media.Brush)TryFindResource("Status.Success") 
                            ?? System.Windows.Media.Brushes.Green;
                    }
                }
            }
            catch
            {
                // Ignore calculation errors
            }
        }
    }
}
