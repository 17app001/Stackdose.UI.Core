# ?? 虛擬 PLC 模擬器 - 完整使用指南

## ?? 目錄
- [快速開始](#快速開始)
- [功能特色](#功能特色)
- [使用方式](#使用方式)
- [物理模擬說明](#物理模擬說明)
- [故障注入測試](#故障注入測試)
- [進階配置](#進階配置)

---

## ?? 快速開始

### 方法 1：自動模式（推薦）

在 `App.xaml.cs` 加入以下代碼：

```csharp
using Stackdose.Hardware.Plc;

protected override void OnStartup(StartupEventArgs e)
{
    #if DEBUG
    PlcClientFactory.UseSimulator = true; // 開發環境自動使用模擬器
    #endif
    
    base.OnStartup(e);
}
```

**優點：**
- ? Debug 模式自動使用模擬器
- ? Release 模式自動連接真實 PLC
- ? 無需修改其他代碼

---

### 方法 2：環境變數控制

設定系統環境變數：
```bash
PLC_SIMULATOR=1
```

或在程式啟動時：
```csharp
Environment.SetEnvironmentVariable("PLC_SIMULATOR", "1");
```

**優點：**
- ? 不修改代碼即可切換
- ? 適合 CI/CD 環境
- ? 團隊成員可各自設定

---

## ?? 功能特色

### 1. 真實物理模擬

#### 溫度控制（PID 簡化版）
- ?? **加熱行為**：考慮熱慣性，越接近目標溫度加熱越慢
- ?? **冷卻行為**：自然散熱回到環境溫度
- ?? **真實雜訊**：模擬感測器讀數波動

**PLC 位址映射：**
| 位址 | 說明 | 範例值 |
|------|------|--------|
| D10 | 當前溫度（x10） | 250 = 25.0°C |
| D12 | 目標溫度（x10） | 800 = 80.0°C |
| M0 | 加熱器開關 | 0=關, 1=開 |
| M100 | 高溫警報 | 自動觸發 >100°C |

#### 馬達與軸移動
- ?? **慣性模擬**：馬達加速/減速真實模擬
- ?? **自動停止**：到達終點自動停機
- ?? **位置回授**：即時更新軸位置

**PLC 位址映射：**
| 位址 | 說明 | 範例值 |
|------|------|--------|
| D0 | X軸位置（μm） | 0~10000 |
| D2 | 馬達速度 | 0~100 |
| M1 | 馬達運轉 | 0=停, 1=運轉 |
| M101 | 伺服異常 | 故障注入觸發 |

---

### 2. 故障注入系統

用於測試異常處理邏輯：

```csharp
var simulator = new SmartPlcSimulator();

// 注入溫度感測器異常（5秒後觸發）
simulator.InjectFault("TEMP_SENSOR_ERROR", 5000);

// 注入伺服異常（立即觸發）
simulator.InjectFault("SERVO_ERROR", 0);

// 清除所有故障
simulator.ClearAllFaults();
```

**支援的故障類型：**
- `TEMP_SENSOR_ERROR` - 溫度讀數凍結/跳動
- `SERVO_ERROR` - 伺服異常警報
- 更多故障類型可擴展...

---

### 3. 事件回呼機制

```csharp
simulator.OnTemperatureChanged += temp =>
{
    Console.WriteLine($"溫度變化: {temp:F1}°C");
    // 更新 UI 圖表...
};

simulator.OnAxisMoved += position =>
{
    Console.WriteLine($"軸移動至: {position:F0} μm");
};

simulator.OnAlarmTriggered += alarm =>
{
    Console.WriteLine($"?? 警報: {alarm}");
    // 記錄到日誌系統...
};
```

---

## ?? 使用控制面板

### 在 XAML 中加入控制面板

```xml
<Window xmlns:controls="clr-namespace:Stackdose.UI.Core.Controls;assembly=Stackdose.UI.Core">
    
    <Grid>
        <!-- PLC 連線狀態 -->
        <controls:PlcStatus x:Name="PlcStatus" 
                           IpAddress="127.0.0.1" 
                           Port="502" 
                           IsGlobal="True"/>

        <!-- 模擬器控制面板 -->
        <controls:SimulatorControlPanel x:Name="SimPanel"/>
    </Grid>
</Window>
```

### 在 Code-Behind 綁定模擬器

```csharp
public partial class MainWindow : Window
{
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // 取得模擬器實例
        if (PlcStatus.CurrentManager?.PlcClient is SmartPlcSimulator simulator)
        {
            SimPanel.BindSimulator(simulator);
        }
    }
}
```

**控制面板功能：**
- ?? 開啟/關閉加熱器
- ?? 啟動/停止馬達
- ?? 故障注入測試
- ?? 即時監控溫度、軸位置
- ?? 統計資訊（週期數、讀寫次數）

---

## ?? 進階配置

### 自訂物理參數

```csharp
var simulator = new SmartPlcSimulator();

simulator.Config.HeatingRate = 0.5;          // 加熱速率 (°C/週期)
simulator.Config.CoolingRate = 0.1;          // 冷卻速率
simulator.Config.NoiseLevel = 0.05;          // 雜訊水平
simulator.Config.MaxMotorSpeed = 100.0;      // 馬達最大速度
simulator.Config.MotorAcceleration = 2.0;    // 加速度
simulator.Config.MaxAxisPosition = 10000.0;  // 軸最大位置
simulator.Config.HighTempThreshold = 100.0;  // 高溫警報閾值
simulator.Config.SimulationCycleMs = 50;     // 模擬週期 (預設50ms = 20Hz)
```

### 查看運行統計

```csharp
var stats = simulator.Stats;

Console.WriteLine($"運行時間: {stats.UptimeSeconds:F1} 秒");
Console.WriteLine($"總週期數: {stats.TotalCycles}");
Console.WriteLine($"讀取次數: {stats.TotalReads}");
Console.WriteLine($"寫入次數: {stats.TotalWrites}");
Console.WriteLine($"警報次數: {stats.AlarmCount}");
Console.WriteLine($"當前溫度: {stats.LastTemperature:F1}°C");
Console.WriteLine($"當前位置: {stats.LastAxisPosition:F0} μm");
```

---

## ?? 測試案例範例

### 測試 1：溫控系統

```csharp
// 1. 設定目標溫度
await simulator.WriteDeviceValueAsync("D12,800"); // 80°C

// 2. 開啟加熱器
await simulator.WriteDeviceValueAsync("M0,1");

// 3. 等待溫度上升
await Task.Delay(10000);

// 4. 檢查是否達到目標
var temp = await simulator.ReadDWordAsync("D", 10);
Assert.IsTrue(temp >= 750); // 至少 75°C

// 5. 檢查高溫警報（應該未觸發）
var alarm = await simulator.ReadBitAsync("M", 100);
Assert.IsFalse(alarm);
```

### 測試 2：馬達移動

```csharp
// 1. 啟動馬達
await simulator.WriteDeviceValueAsync("M1,1");

// 2. 等待移動
await Task.Delay(5000);

// 3. 檢查位置變化
var pos = await simulator.ReadDWordAsync("D", 0);
Assert.IsTrue(pos > 0);

// 4. 停止馬達
await simulator.WriteDeviceValueAsync("M1,0");

// 5. 檢查速度歸零
await Task.Delay(1000);
var speed = await simulator.ReadDWordAsync("D", 2);
Assert.AreEqual(0, speed);
```

### 測試 3：故障處理

```csharp
// 1. 注入感測器異常
simulator.InjectFault("TEMP_SENSOR_ERROR", 1000);

// 2. 訂閱警報事件
bool alarmReceived = false;
simulator.OnAlarmTriggered += alarm => 
{
    if (alarm.Contains("異常")) 
        alarmReceived = true;
};

// 3. 等待故障觸發
await Task.Delay(2000);

// 4. 驗證異常處理邏輯
// (您的程式應該偵測到感測器讀數異常)

// 5. 清除故障
simulator.ClearFault("TEMP_SENSOR_ERROR");
```

---

## ?? 故障排除

### Q: 模擬器沒有啟動？
**A:** 檢查以下項目：
1. `PlcClientFactory.UseSimulator` 是否設為 `true`
2. 環境變數 `PLC_SIMULATOR` 是否設為 `1`
3. Debug 輸出是否顯示 "?? 模擬器模式"

### Q: PlcLabel 不更新？
**A:** 確認：
1. `PlcStatus` 的 `IsGlobal="True"`
2. `MonitorAddress` 有包含對應的位址
3. 使用 `PlcStatus.CurrentManager.Monitor.Register("D0", 20)` 手動註冊

### Q: 如何強制使用真實 PLC？
**A:** 
```csharp
PlcClientFactory.UseSimulator = false;
```

---

## ?? 效能指標

- **模擬週期**: 50ms (20Hz)
- **溫度精度**: 0.1°C
- **位置精度**: 1μm
- **記憶體佔用**: < 10MB
- **CPU 使用率**: < 1% (單核)

---

## ?? 最佳實踐

### ? DO
- ? 開發時使用模擬器，加速迭代
- ? 使用故障注入測試異常處理
- ? 訂閱事件進行自動化測試
- ? 定期查看統計資訊

### ? DON'T
- ? 在生產環境啟用模擬器
- ? 依賴模擬器的「完美行為」（真實設備會有延遲、失敗）
- ? 過度調整物理參數（應符合真實設備特性）

---

## ?? 下一步擴展

未來可以加入：
- ?? **場景腳本系統** - JSON 定義測試流程
- ?? **多機台模擬** - 模擬多個 PLC 協同工作
- ?? **歷史曲線記錄** - 自動生成溫度/位置曲線
- ??? **Web 控制介面** - 遠端監控模擬器

---

## ?? 技術支援

遇到問題？
1. 查看 Debug 輸出視窗
2. 檢查 `simulator.Stats` 統計資訊
3. 使用控制面板即時監控

---

**版本**: v2.0 (2024-01)  
**作者**: Stackdose Team  
**授權**: MIT License
