using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Windows;
using System.Windows.Media;

namespace WpfApp1.ViewModels
{
    /// <summary>
    /// MainWindow 的 ViewModel
    /// 使用 CommunityToolkit.Mvvm 實現現代化 MVVM 模式
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        #region 建構子

        public MainViewModel()
        {
            // 訂閱感測器警報事件
            SensorContext.AlarmTriggered += OnSensorAlarmTriggered;
            SensorContext.AlarmCleared += OnSensorAlarmCleared;

            // 訂閱 PlcLabel 值變更事件
            PlcLabelContext.ValueChanged += OnPlcLabelValueChanged;

            // 訂閱 PlcEvent 事件觸發
            PlcEventContext.EventTriggered += OnPlcEventTriggered;
        }

        #endregion

        #region 命令 (使用 RelayCommand 自動生成)

        /// <summary>
        /// 測試 UI 執行緒 MessageBox
        /// </summary>
        [RelayCommand]
        private void TestUIThreadMessageBox()
        {
            CyberMessageBox.Show(
                "這是從 UI 執行緒呼叫的訊息",
                "? UI 執行緒測試",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// 測試背景執行緒 MessageBox
        /// </summary>
        [RelayCommand]
        private void TestBackgroundMessageBox()
        {
            Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);

                var result = CyberMessageBox.Show(
                    "這是從背景執行緒呼叫的訊息！\n\n" +
                    "如果沒有當機，表示修正成功！",
                    "? 背景執行緒測試",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    CyberMessageBox.Show(
                        "您選擇了「是」",
                        "? 測試結果",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            });
        }

        /// <summary>
        /// 測試連續多個 MessageBox
        /// </summary>
        [RelayCommand]
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
                    "這是第 3 個訊息\n\n測試完成！?",
                    "3/3 - 完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.None
                );
            }
        }

        /// <summary>
        /// 製程開始命令
        /// </summary>
        [RelayCommand]
        private void StartProcess()
        {
            // 1. 檢查 PLC 連線狀態
            var plcManager = PlcContext.GlobalStatus?.CurrentManager;
            if (plcManager == null || !plcManager.IsConnected)
            {
                CyberMessageBox.Show(
                    "?? PLC 未連線\n無法啟動製程",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // 2. 顯示確認對話框
            var result = CyberMessageBox.Show(
                "確定要啟動製程嗎？\n\n請確認：\n" +
                "? 設備已就緒\n" +
                "? 安全防護已確認\n" +
                "? 原料已備妥",
                "?? 製程啟動確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
            {
                ComplianceContext.LogSystem(
                    "製程啟動已取消",
                    LogLevel.Info,
                    showInUi: true
                );
                return;
            }

            // 3. 寫入 PLC 啟動信號
            try
            {
                // 寫入啟動信號到 M100
                _ = plcManager.WriteAsync("M100,1");

                // 4. 記錄到 Audit Trail
                ComplianceContext.LogAuditTrail(
                    deviceName: "製程控制",
                    address: "M100",
                    oldValue: "0",
                    newValue: "1",
                    reason: $"製程啟動 by {SecurityContext.CurrentSession.CurrentUserName}",
                    showInUi: true
                );

                // 5. 記錄到系統日誌
                ComplianceContext.LogSystem(
                    $"?? 製程已啟動 by {SecurityContext.CurrentSession.CurrentUserName}",
                    LogLevel.Success,
                    showInUi: true
                );

                // 6. 顯示成功訊息
                CyberMessageBox.Show(
                    "? 製程已成功啟動！\n\n請監控設備運行狀態",
                    "製程啟動",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                // 7. 錯誤處理
                ComplianceContext.LogSystem(
                    $"? 製程啟動失敗: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );

                CyberMessageBox.Show(
                    $"? 製程啟動失敗\n\n錯誤訊息：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        #endregion

        #region 感測器警報處理

        /// <summary>
        /// 感測器警報觸發時的處理
        /// </summary>
        private void OnSensorAlarmTriggered(object? sender, SensorAlarmEventArgs e)
        {
            // 針對特定感測器執行動作
            if (e.Sensor.Device == "D90")
            {
                CyberMessageBox.Show(
                    $"緊急警報！{e.Sensor.OperationDescription} 已觸發！\n\n" +
                    $"裝置：{e.Sensor.Device}\n" +
                    $"時間：{e.EventTime:HH:mm:ss}\n" +
                    $"當前值：{e.Sensor.CurrentValue}",
                    "?? 緊急警報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            // 根據感測器分組執行動作
            if (e.Sensor.Group == "安全門檢測")
            {
                PlayAlarmSound();

                CyberMessageBox.Show(
                    $"{e.Sensor.OperationDescription} 偵測到異常！\n請立即檢查安全門狀態。",
                    "?? 安全警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // 根據操作描述執行動作
            if (e.Sensor.OperationDescription.Contains("緊急停止"))
            {
                var result = CyberMessageBox.Show(
                    "偵測到緊急停止信號！\n是否要停止所有操作？",
                    "? 緊急停止確認",
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
                    "? 系統恢復",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// 播放警報音效
        /// </summary>
        private static void PlayAlarmSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        /// <summary>
        /// 停止所有操作
        /// </summary>
        private static void StopAllOperations()
        {
            ComplianceContext.LogSystem("所有操作已停止（緊急停止觸發）", LogLevel.Error, showInUi: true);
        }

        #endregion

        #region PlcLabel 數值變更處理

        /// <summary>
        /// PlcLabel 值變更時的處理
        /// </summary>
        private void OnPlcLabelValueChanged(object? sender, PlcLabelValueChangedEventArgs e)
        {
            // 根據 Address 處理不同的 PlcLabel
            switch (e.Address)
            {
                case "R2002": // 溫度
                    UpdateTemperatureColor(e.PlcLabel, e.Value);
                    break;

                case "D100":
                    // 處理其他邏輯
                    break;
            }

            // 根據 Label 處理
            if (e.Label == "X軸位置")
            {
                if (!double.TryParse(e.Value.ToString(), out double currentPosX) || currentPosX < 43800)
                    return;
                    
                CyberMessageBox.Show(
                    $"X軸位置異常：{currentPosX}",
                    "?? 警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            if (e.Label == "溫度")
            {
                UpdateTemperatureColor(e.PlcLabel, e.Value);
            }

            // 根據條件處理
            if (e.Label.Contains("壓力") && double.TryParse(e.Value.ToString(), out double pressure))
            {
                if (pressure > 100)
                {
                    e.PlcLabel.Foreground = Brushes.Red;
                    CyberMessageBox.Show(
                        $"壓力異常：{pressure} bar",
                        "?? 警告",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
        }

        /// <summary>
        /// 更新溫度顏色
        /// </summary>
        private static void UpdateTemperatureColor(PlcLabel label, object value)
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
        /// PlcEvent 觸發時的處理
        /// </summary>
        private void OnPlcEventTriggered(object? sender, PlcEventTriggeredEventArgs e)
        {
            // 根據 EventName 處理
            switch (e.EventName)
            {
                case "Recipe1Selected":
                    LoadRecipe1();
                    CyberMessageBox.Show(
                        $"Recipe 1 已載入\n事件：{e.EventName}",
                        "? 成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    break;

                case "Recipe2Selected":
                    LoadRecipe2();
                    CyberMessageBox.Show(
                        $"Recipe 2 已載入\n事件：{e.EventName}",
                        "? 成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    break;
            }
        }

        /// <summary>
        /// 載入 Recipe 1
        /// </summary>
        private static void LoadRecipe1()
        {
            ComplianceContext.LogSystem("Recipe 1 載入中...", LogLevel.Info, showInUi: true);
            // TODO: 實作 Recipe 1 載入邏輯
        }

        /// <summary>
        /// 載入 Recipe 2
        /// </summary>
        private static void LoadRecipe2()
        {
            ComplianceContext.LogSystem("Recipe 2 載入中...", LogLevel.Info, showInUi: true);
            // TODO: 實作 Recipe 2 載入邏輯
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
            PlcLabelContext.ValueChanged -= OnPlcLabelValueChanged;
            PlcEventContext.EventTriggered -= OnPlcEventTriggered;
        }

        #endregion
    }
}
