using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Stackdose.UI.Core.Helpers;

namespace ModelB.Demo
{
    /// <summary>
    /// FDA Test Data Generator Window
    /// </summary>
    public partial class FdaTestDataWindow : Window
    {
        public FdaTestDataWindow()
        {
            InitializeComponent();
            LogMessage("=== FDA 21 CFR Part 11 Test Data Generator ===");
            LogMessage("Set parameters and click a button to generate test data.");
            LogMessage("");
        }

        /// <summary>
        /// Generate Event Logs
        /// </summary>
        private async void GenerateEventLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int eventCount, out int _, out int daysBack, out string batchId))
                return;

            SetButtonsEnabled(false);
            LogMessage($"[{DateTime.Now:HH:mm:ss}] Starting Event Log generation...");

            try
            {
                await Task.Run(() =>
                {
                    FdaLogDataGenerator.GenerateEventLogs(
                        count: eventCount,
                        batchId: batchId,
                        startDate: DateTime.Now,
                        daysBack: daysBack
                    );
                });

                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] Event Logs generated successfully!");
                    LogMessage($"  - Count: {eventCount}");
                    LogMessage($"  - Date Range: {DateTime.Now.AddDays(-daysBack):yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
                    LogMessage("");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                    LogMessage("");
                });
                MessageBox.Show($"Generation failed: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => SetButtonsEnabled(true));
            }
        }

        /// <summary>
        /// Generate Periodic Data Logs
        /// </summary>
        private async void GeneratePeriodicData_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int _, out int periodicCount, out int daysBack, out string batchId))
                return;

            SetButtonsEnabled(false);
            LogMessage($"[{DateTime.Now:HH:mm:ss}] Starting Periodic Data Log generation...");

            try
            {
                await Task.Run(() =>
                {
                    FdaLogDataGenerator.GeneratePeriodicDataLogs(
                        count: periodicCount,
                        batchId: batchId,
                        startDate: DateTime.Now,
                        daysBack: daysBack
                    );
                });

                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] Periodic Data Logs generated successfully!");
                    LogMessage($"  - Count: {periodicCount}");
                    LogMessage($"  - Date Range: {DateTime.Now.AddDays(-daysBack):yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
                    LogMessage("");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                    LogMessage("");
                });
                MessageBox.Show($"Generation failed: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => SetButtonsEnabled(true));
            }
        }

        /// <summary>
        /// Generate All Test Data
        /// </summary>
        private async void GenerateAll_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int eventCount, out int periodicCount, out int daysBack, out string _))
                return;

            SetButtonsEnabled(false);
            LogMessage($"[{DateTime.Now:HH:mm:ss}] Starting complete FDA test data generation...");

            try
            {
                await Task.Run(() =>
                {
                    FdaLogDataGenerator.GenerateCompleteFdaTestData(
                        eventLogCount: eventCount,
                        periodicDataCount: periodicCount,
                        daysBack: daysBack
                    );
                });

                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] All test data generated successfully!");
                    LogMessage($"  - Event Logs: {eventCount}");
                    LogMessage($"  - Periodic Data Logs: {periodicCount}");
                    LogMessage($"  - Date Range: {DateTime.Now.AddDays(-daysBack):yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
                    LogMessage("");
                });

                MessageBox.Show(
                    $"Test data generated successfully!\n\nEvent Logs: {eventCount}\nPeriodic Data Logs: {periodicCount}\n\nGo to Log Viewer to see the results.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                    LogMessage("");
                });
                MessageBox.Show($"Generation failed: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => SetButtonsEnabled(true));
            }
        }

        /// <summary>
        /// Validate input parameters
        /// </summary>
        private bool ValidateInputs(out int eventCount, out int periodicCount, out int daysBack, out string batchId)
        {
            eventCount = 0;
            periodicCount = 0;
            daysBack = 0;
            batchId = BatchIdTextBox.Text.Trim();

            if (!int.TryParse(EventLogCountTextBox.Text, out eventCount) || eventCount <= 0)
            {
                MessageBox.Show("Please enter a valid Event Log count (> 0)", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(PeriodicDataCountTextBox.Text, out periodicCount) || periodicCount <= 0)
            {
                MessageBox.Show("Please enter a valid Periodic Data count (> 0)", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(DaysBackTextBox.Text, out daysBack) || daysBack <= 0)
            {
                MessageBox.Show("Please enter a valid Days Back value (> 0)", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Log message to output
        /// </summary>
        private void LogMessage(string message)
        {
            LogTextBlock.Text += message + Environment.NewLine;

            // Auto-scroll to bottom
            var parent = LogTextBlock.Parent;
            if (parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// Set all buttons enabled/disabled
        /// </summary>
        private void SetButtonsEnabled(bool enabled)
        {
            var grid = this.Content as Grid;
            if (grid == null) return;

            foreach (var child in grid.Children)
            {
                if (child is UniformGrid uniformGrid)
                {
                    foreach (var item in uniformGrid.Children)
                    {
                        if (item is Button button)
                        {
                            button.IsEnabled = enabled;
                        }
                    }
                }
            }
        }
    }
}
