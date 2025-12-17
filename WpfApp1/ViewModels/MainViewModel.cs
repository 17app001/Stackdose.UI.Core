using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// MainWindow 的 ViewModel
    /// 實現 MVVM 模式，將所有業務邏輯從 CodeBehind 移至此處
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 建構子

        public MainViewModel()
        {
            // 訂閱感測器警報事件
            SensorContext.AlarmTriggered += OnSensorAlarmTriggered;
            SensorContext.AlarmCleared += OnSensorAlarmCleared;

            // 🔥 訂閱 PlcLabel 值變更事件（統一管理）
            PlcLabelContext.ValueChanged += OnPlcLabelValueChanged;

            // 🔥 訂閱 PlcEvent 事件觸發（統一管理）
            PlcEventContext.EventTriggered += OnPlcEventTriggered;

            // 初始化命令
            TestUIThreadMessageBoxCommand = new RelayCommand(TestUIThreadMessageBox);
            TestBackgroundMessageBoxCommand = new RelayCommand(TestBackgroundMessageBox);
            TestMultipleMessageBoxCommand = new RelayCommand(TestMultipleMessageBox);
        }

        #endregion

        #region 命令 (Commands)

        public ICommand TestUIThreadMessageBoxCommand { get; }
        public ICommand TestBackgroundMessageBoxCommand { get; }
        public ICommand TestMultipleMessageBoxCommand { get; }

        #endregion

        #region 感測器警報處理

        /// <summary>
        /// 感測器警報觸發時的處理
        /// </summary>
        private void OnSensorAlarmTriggered(object? sender, SensorAlarmEventArgs e)
        {
            // 方式 1：針對特定感測器執行動作
            if (e.Sensor.Device == "D90")
            {
                CyberMessageBox.Show(
                    $"緊急警報！{e.Sensor.OperationDescription} 已觸發！\n\n" +
                    $"裝置：{e.Sensor.Device}\n" +
                    $"時間：{e.EventTime:HH:mm:ss}\n" +
                    $"當前值：{e.Sensor.CurrentValue}",
                    "🚨 緊急警報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            // 方式 2：根據感測器分組執行動作
            if (e.Sensor.Group == "安全門檢測")
            {
                PlayAlarmSound();

                CyberMessageBox.Show(
                    $"{e.Sensor.OperationDescription} 偵測到異常！\n請立即檢查安全門狀態。",
                    "⚠️ 安全警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // 方式 3：根據操作描述執行動作
            if (e.Sensor.OperationDescription.Contains("緊急停止"))
            {
                var result = CyberMessageBox.Show(
                    "偵測到緊急停止信號！\n是否要停止所有操作？",
                    "❓ 緊急停止確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    StopAllOperations();
                }
            }
        }

        /// <summary>
        /// 感測器警報消失時的處理
        /// </summary>
        private void OnSensorAlarmCleared(object? sender, SensorAlarmEventArgs e)
        {
            if (e.Sensor.Device == "D90")
            {
                CyberMessageBox.Show(
                    $"{e.Sensor.OperationDescription} 已恢復正常\n\n" +
                    $"警報持續時間：{e.Duration.TotalSeconds:F1} 秒",
                    "✅ 系統恢復",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// 播放警報音效
        /// </summary>
        private void PlayAlarmSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        /// <summary>
        /// 停止所有操作
        /// </summary>
        private void StopAllOperations()
        {
            ComplianceContext.LogSystem("所有操作已停止（緊急停止觸發）", LogLevel.Error, showInUi: true);
        }

        #endregion

        #region PlcLabel 數值變更處理

        /// <summary>
        /// PlcLabel 值變更時的處理（從 PlcLabelContext 事件觸發）
        /// 類似 SensorContext.AlarmTriggered 的使用方式
        /// </summary>
        private void OnPlcLabelValueChanged(object? sender, PlcLabelValueChangedEventArgs e)
        {
            // 方式 1：根據 Address 處理不同的 PlcLabel
            switch (e.Address)
            {
                case "R2002": // 溫度
                    UpdateTemperatureColor(e.PlcLabel, e.Value);
                    break;

                case "D100": // 其他裝置（範例）
                    // 處理其他邏輯
                    break;
            }

            if (e.Label == "X軸位置")
            {
                if (!double.TryParse(e.Value.ToString(), out double currentPosX) || currentPosX < 43800)
                    return;
                CyberMessageBox.Show($"X軸位置異常：{currentPosX}", "⚠️ 警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 方式 2：根據 Label 處理
            if (e.Label == "溫度")
            {
                UpdateTemperatureColor(e.PlcLabel, e.Value);
            }

            // 方式 3：根據條件處理（範例）
            if (e.Label.Contains("壓力") && double.TryParse(e.Value.ToString(), out double pressure))
            {
                if (pressure > 100)
                {
                    e.PlcLabel.Foreground = Brushes.Red;
                    CyberMessageBox.Show($"壓力異常：{pressure} bar", "⚠️ 警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// 更新溫度顏色
        /// </summary>
        private void UpdateTemperatureColor(PlcLabel label, object value)
        {
            if (!double.TryParse(value.ToString(), out double currentTemp))
                return;

            // 根據溫度設定顏色
            label.Foreground = currentTemp switch
            {
                >= 100 => Brushes.Red,
                >= 75 => Brushes.Orange,
                >= 50 => Brushes.Yellow,
                _ => Brushes.Green
            };
        }

        #endregion

        #region PlcEvent 事件觸發處理

        /// <summary>
        /// PlcEvent 觸發時的處理（從 PlcEventContext 事件觸發）
        /// 類似 SensorContext.AlarmTriggered 的使用方式
        /// </summary>
        private void OnPlcEventTriggered(object? sender, PlcEventTriggeredEventArgs e)
        {
            // 🔥 只使用一種方式：根據 EventName 處理（推薦）
            switch (e.EventName)
            {
                case "Recipe1Selected":
                    LoadRecipe1();
                    CyberMessageBox.Show(
                        $"Recipe 1 已載入\n事件：{e.EventName}",
                        "✅ 成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    break;

                case "Recipe2Selected":
                    LoadRecipe2();
                    CyberMessageBox.Show(
                        $"Recipe 2 已載入\n事件：{e.EventName}",
                        "✅ 成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    break;
            }

            // 🔥 自動清空已在 PlcEventTrigger 內部完成，不需要手動寫 0
        }

        /// <summary>
        /// 載入 Recipe 1
        /// </summary>
        private void LoadRecipe1()
        {
            ComplianceContext.LogSystem("Recipe 1 載入中...", LogLevel.Info, showInUi: true);
            // TODO: 實作 Recipe 1 載入邏輯
        }

        /// <summary>
        /// 載入 Recipe 2
        /// </summary>
        private void LoadRecipe2()
        {
            ComplianceContext.LogSystem("Recipe 2 載入中...", LogLevel.Info, showInUi: true);
            // TODO: 實作 Recipe 2 載入邏輯
        }

        #endregion

        #region 測試方法

        /// <summary>
        /// 測試 1：從 UI 執行緒呼叫 MessageBox
        /// </summary>
        private void TestUIThreadMessageBox()
        {
            CyberMessageBox.Show(
                "這是從 UI 執行緒呼叫的訊息",
                "✅ UI 執行緒測試",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// 測試 2：從背景執行緒呼叫 MessageBox（模擬感測器警報）
        /// </summary>
        private void TestBackgroundMessageBox()
        {
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);

                var result = CyberMessageBox.Show(
                    "這是從背景執行緒呼叫的訊息！\n\n" +
                    "如果沒有當機，表示修正成功！",
                    "⚡ 背景執行緒測試",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    CyberMessageBox.Show(
                        "您選擇了「是」",
                        "✅ 測試結果",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            });
        }

        /// <summary>
        /// 測試 3：連續顯示多個 MessageBox
        /// </summary>
        private void TestMultipleMessageBox()
        {
            CyberMessageBox.Show(
                "這是第 1 個訊息",
                "1/3",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            var result = CyberMessageBox.Show(
                "這是第 2 個訊息，是否繼續？",
                "2/3",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                CyberMessageBox.Show(
                    "這是第 3 個訊息\n\n測試完成！✨",
                    "3/3 - 完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.None
                );
            }
        }

        #endregion

        #region 清理資源

        /// <summary>
        /// 清理資源（取消訂閱事件）
        /// </summary>
        public void Cleanup()
        {
            SensorContext.AlarmTriggered -= OnSensorAlarmTriggered;
            SensorContext.AlarmCleared -= OnSensorAlarmCleared;

            // 🔥 取消訂閱 PlcLabel 事件
            PlcLabelContext.ValueChanged -= OnPlcLabelValueChanged;

            // 🔥 取消訂閱 PlcEvent 事件
            PlcEventContext.EventTriggered -= OnPlcEventTriggered;
        }

        #endregion
    }

    #region RelayCommand 輔助類別

    /// <summary>
    /// 簡單的 ICommand 實作
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// 泛型 ICommand 實作（支援參數）
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute == null || _canExecute(typedParameter);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
        }
    }

    #endregion
}
