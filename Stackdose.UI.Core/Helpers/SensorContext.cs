using Stackdose.UI.Core.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Data;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 感測器上下文管理 (Sensor Context Manager)
    /// 用途：統一管理感測器配置、狀態監控和警報事件
    /// </summary>
    public static class SensorContext
    {
        #region 靜態屬性

        /// <summary>
        /// 全域感測器清單 (可綁定到 UI)
        /// </summary>
        public static ObservableCollection<SensorConfig> Sensors { get; } = new ObservableCollection<SensorConfig>();

        private static object _lock = new object();

        #endregion

        #region 事件定義

        /// <summary>
        /// 警報觸發事件 (當感測器從正常變為異常時觸發)
        /// </summary>
        public static event EventHandler<SensorAlarmEventArgs>? AlarmTriggered;

        /// <summary>
        /// 警報消失事件 (當感測器從異常變為正常時觸發)
        /// </summary>
        public static event EventHandler<SensorAlarmEventArgs>? AlarmCleared;

        #endregion

        #region 靜態建構子

        static SensorContext()
        {
            // 啟用跨執行緒集合同步 (允許從 PLC 監控執行緒更新)
            BindingOperations.EnableCollectionSynchronization(Sensors, _lock);
        }

        #endregion

        #region 公開方法

        /// <summary>
        /// 從 JSON 檔案載入感測器配置
        /// </summary>
        /// <param name="jsonFilePath">JSON 檔案名稱 (例如：Sensors.json，會自動從 Resources 目錄載入)</param>
        public static void LoadFromJson(string jsonFilePath)
        {
            try
            {
                // 🔥 使用 ResourcePathHelper 統一管理路徑
                string fullPath;
                
                if (Path.IsPathRooted(jsonFilePath) && File.Exists(jsonFilePath))
                {
                    // 支援絕對路徑（向下相容）
                    fullPath = jsonFilePath;
                }
                else
                {
                    // 優先使用 ResourcePathHelper
                    fullPath = ResourcePathHelper.GetResourceFilePath(jsonFilePath);
                }

                if (!File.Exists(fullPath))
                {
                    LogError($"Sensor config file not found: {fullPath}");
                    return;
                }

                // 🔥 修正：強制使用 UTF-8 編碼讀取 JSON 檔案
                string json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                var configs = JsonSerializer.Deserialize<SensorConfig[]>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 🔥 允許中文字元
                });

                if (configs == null || configs.Length == 0)
                {
                    LogError($"No sensors found in config file: {fullPath}");
                    return;
                }

                lock (_lock)
                {
                    Sensors.Clear();
                    foreach (var config in configs)
                    {
                        Sensors.Add(config);
                    }
                }

                LogInfo($"Loaded {configs.Length} sensor(s) from {jsonFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load sensor config: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：從當前感測器清單中智慧提取監控位址
        /// 自動合併連續位址（例如 D90, D91, D92 → D90,3）
        /// </summary>
        /// <returns>監控位址字串（例如 "D90,3,M100,1,X10,1"）</returns>
        public static string GenerateMonitorAddresses()
        {
            if (Sensors.Count == 0)
                return string.Empty;

            var addressGroups = new Dictionary<string, List<int>>();

            // 1. 提取所有位址
            foreach (var sensor in Sensors)
            {
                string device = sensor.Device.Trim().ToUpper();
                
                // 解析裝置類型和編號 (例如 D90 → 類型=D, 編號=90)
                if (System.Text.RegularExpressions.Regex.Match(device, @"^([A-Z]+)(\d+)$") is var match && match.Success)
                {
                    string deviceType = match.Groups[1].Value; // D, M, X, Y, R
                    int deviceNumber = int.Parse(match.Groups[2].Value); // 90, 100, 101

                    if (!addressGroups.ContainsKey(deviceType))
                    {
                        addressGroups[deviceType] = new List<int>();
                    }

                    // 避免重複
                    if (!addressGroups[deviceType].Contains(deviceNumber))
                    {
                        addressGroups[deviceType].Add(deviceNumber);
                    }
                }
            }

            // 2. 智慧合併連續位址
            var monitorParts = new List<string>();

            foreach (var group in addressGroups.OrderBy(g => g.Key))
            {
                string deviceType = group.Key;
                var numbers = group.Value.OrderBy(n => n).ToList();

                int i = 0;
                while (i < numbers.Count)
                {
                    int start = numbers[i];
                    int end = start;

                    // 找出連續範圍
                    while (i + 1 < numbers.Count && numbers[i + 1] == end + 1)
                    {
                        i++;
                        end = numbers[i];
                    }

                    int length = end - start + 1;

                    // 🔥 修正：連續 2 個以上就批次合併
                    if (length >= 2)
                    {
                        // 批次模式：D90,2 表示 D90, D91
                        monitorParts.Add($"{deviceType}{start},{length}");
                    }
                    else
                    {
                        // 單獨模式：D200,1
                        monitorParts.Add($"{deviceType}{start},1");
                    }

                    i++;
                }
            }

            string result = string.Join(",", monitorParts);
            LogInfo($"Generated monitor addresses: {result}");
            return result;
        }
        /// <summary>
        /// 更新感測器狀態 (由監控服務呼叫)
        /// </summary>
        /// <param name="sensor">感測器配置</param>
        /// <param name="isActive">新的狀態 (true=異常, false=正常)</param>
        /// <param name="currentValue">當前讀取的數值</param>
        public static void UpdateSensorState(SensorConfig sensor, bool isActive, string currentValue)
        {
            bool wasActive = sensor.IsActive;
            sensor.CurrentValue = currentValue;
            sensor.IsActive = isActive;

            // 狀態變化時觸發事件和日誌
            if (wasActive != isActive)
            {
                if (isActive)
                {
                    // 警報觸發 (正常 → 異常)
                    OnAlarmTriggered(sensor);
                }
                else
                {
                    // 警報消失 (異常 → 正常)
                    OnAlarmCleared(sensor);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 觸發警報觸發事件
        /// </summary>
        private static void OnAlarmTriggered(SensorConfig sensor)
        {
            // 1. 記錄到 Compliance 系統 (SQLite + LiveLogViewer)
            string message = $"警報觸發: {sensor.OperationDescription} ({sensor.Device})";
            ComplianceContext.LogSystem(message, LogLevel.Warning, showInUi: true);

            // 2. 記錄到 Audit Trail (符合法規要求)
            ComplianceContext.LogAuditTrail(
                deviceName: sensor.OperationDescription,
                address: sensor.Device,
                oldValue: "正常",
                newValue: "異常",
                reason: $"感測器觸發條件: {sensor.Mode} {sensor.Value}",
                showInUi: false  // 避免重複顯示
            );

            // 3. 🔥 修正：在 UI 執行緒上觸發外部事件（避免 MessageBox 當機）
            if (AlarmTriggered != null)
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    AlarmTriggered?.Invoke(null, new SensorAlarmEventArgs(sensor, DateTime.Now));
                });
            }
        }

        /// <summary>
        /// 觸發警報消失事件
        /// </summary>
        private static void OnAlarmCleared(SensorConfig sensor)
        {
            // 計算警報持續時間
            TimeSpan duration = TimeSpan.Zero;
            if (sensor.AlarmTriggeredTime.HasValue && sensor.AlarmClearedTime.HasValue)
            {
                duration = sensor.AlarmClearedTime.Value - sensor.AlarmTriggeredTime.Value;
            }

            // 1. 記錄到 Compliance 系統
            string message = $"警報消失: {sensor.OperationDescription} ({sensor.Device}) [持續 {duration.TotalSeconds:F1}s]";
            ComplianceContext.LogSystem(message, LogLevel.Info, showInUi: true);

            // 2. 記錄到 Audit Trail
            ComplianceContext.LogAuditTrail(
                deviceName: sensor.OperationDescription,
                address: sensor.Device,
                oldValue: "異常",
                newValue: "正常",
                reason: $"感測器恢復正常 (持續時間: {duration.TotalSeconds:F1}s)",
                showInUi: false
            );

            // 3. 🔥 修正：在 UI 執行緒上觸發外部事件
            if (AlarmCleared != null)
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    AlarmCleared?.Invoke(null, new SensorAlarmEventArgs(sensor, DateTime.Now, duration));
                });
            }
        }

        /// <summary>
        /// 記錄資訊日誌
        /// </summary>
        private static void LogInfo(string message)
        {
            ComplianceContext.LogSystem($"[SensorContext] {message}", LogLevel.Info, showInUi: false);
        }

        /// <summary>
        /// 記錄錯誤日誌
        /// </summary>
        private static void LogError(string message)
        {
            ComplianceContext.LogSystem($"[SensorContext] ERROR: {message}", LogLevel.Error, showInUi: true);
        }

        #endregion
    }

    #region 事件參數

    /// <summary>
    /// 感測器警報事件參數
    /// </summary>
    public class SensorAlarmEventArgs : EventArgs
    {
        public SensorConfig Sensor { get; }
        public DateTime EventTime { get; }
        public TimeSpan Duration { get; }

        public SensorAlarmEventArgs(SensorConfig sensor, DateTime eventTime, TimeSpan duration = default)
        {
            Sensor = sensor;
            EventTime = eventTime;
            Duration = duration;
        }
    }

    #endregion
}
