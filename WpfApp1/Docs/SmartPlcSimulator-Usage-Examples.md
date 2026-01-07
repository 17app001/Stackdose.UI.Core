# ?? SmartPlcSimulator 使用範例

## ?? 快速開始

### 1?? 自動啟用模式（最簡單）

`WpfApp1/App.xaml.cs` 已經設定好了：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    #if DEBUG
    PlcClientFactory.UseSimulator = true; // ? 已啟用
    #endif
    base.OnStartup(e);
}
```

**現在執行 WpfApp1，所有 PLC 功能都會自動使用模擬器！**

---

## ?? 使用模擬器控制面板

### 方法 A：自動開啟（Debug 模式）

在 Debug 模式下，模擬器控制面板會在程式啟動 1 秒後自動開啟。

### 方法 B：快捷鍵

按 **F12** 隨時開啟模擬器控制面板。

### 方法 C：手動開啟

```csharp
var simWindow = new SimulatorWindow();
simWindow.Show();
```

---

## ?? 功能展示

### 1. 溫度控制測試

**步驟：**
1. 啟動 WpfApp1
2. 模擬器控制面板會自動開啟
3. 點擊「Start Heater」
4. 觀察主視窗的 `PlcLabel` (R2002 溫度) 從 25°C 逐漸上升

**物理行為：**
- ?? 加熱中：每 50ms 增加 0.3°C（越接近目標越慢）
- ?? 冷卻中：每 50ms 降低 0.1°C
- ?? 真實雜訊：±0.1°C 隨機波動
- ?? 高溫警報：超過 100°C 自動觸發 M100

**PLC 位址對應：**
```
R2002 (D10) → 當前溫度 (顯示值 / 10)
D12         → 目標溫度 (預設 80°C)
M0          → 加熱器開關 (控制面板控制)
M100        → 高溫警報 (自動觸發)
```

---

### 2. 馬達移動測試

**步驟：**
1. 點擊「Start Motor」
2. 觀察 X Position (D100) 從 0 開始增加
3. 模擬器會加速到最大速度 100
4. 到達 10000 μm 後自動停止

**物理行為：**
- ?? 加速：每週期增加 2 單位速度
- ?? 慣性：停止時有減速過程
- ?? 自動停止：到達最大行程

**PLC 位址對應：**
```
D0 (D100) → X軸位置 (μm)
D2        → 馬達速度
M1        → 馬達運轉 (控制面板控制)
```

---

### 3. Recipe 下載測試

**步驟：**
1. 使用主視窗的 RecipeLoader 控制項
2. 選擇 Recipe1.json、Recipe2.json 或 Recipe3.json
3. 點擊「下載到 PLC」
4. 觀察模擬器統計資訊的「寫入次數」增加

**驗證方式：**
- 使用 `PlcLabel` 讀取對應位址，確認值已寫入
- 查看控制面板的「Statistics」統計資訊
- 檢查 LiveLogViewer 的 Audit Trail 記錄

---

### 4. 故障注入測試

**測試溫度感測器異常：**
1. 點擊「Temp Error」按鈕
2. 1 秒後，R2002 溫度會開始跳動（±5°C）
3. 測試您的異常處理邏輯
4. 點擊「Clear All」恢復正常

**測試伺服異常：**
1. 點擊「Servo Error」按鈕
2. M101 會被設為 1（伺服異常）
3. 警報記錄會顯示「伺服異常」
4. 點擊「Clear All」清除

**使用場景：**
- ? 測試異常處理邏輯
- ? 驗證警報系統
- ? 模擬生產異常
- ? 自動化測試

---

## ?? 即時監控

### 控制面板顯示內容

#### Real-time Status
- ??? **Temperature**: 當前溫度（即時更新）
- ?? **Axis Position**: X軸位置（即時更新）

#### Device Control
- ?? **Start Heater** / ?? **Stop Heater**
- ?? **Start Motor** / ?? **Stop Motor**

#### Fault Injection
- ?? **Temp Error**: 溫度感測器異常
- ?? **Servo Error**: 伺服異常
- ? **Clear All**: 清除所有故障

#### Alarm Log
- ?? 顯示最近 10 筆警報記錄
- ?? 顯示觸發時間

#### Statistics
```
運行 120.5s | 週期:2410 | R/W:1234/56 | 警報:3
```
- **運行時間**: 從連線開始計時
- **週期數**: 模擬迴圈執行次數（20Hz）
- **R/W**: 讀取/寫入次數
- **警報**: 觸發次數

---

## ?? 程式化控制

### 直接存取模擬器

```csharp
// 取得模擬器實例
if (PlcStatus.CurrentManager?.PlcClient is SmartPlcSimulator simulator)
{
    // 1. 調整物理參數
    simulator.Config.HeatingRate = 0.5;      // 加快加熱
    simulator.Config.NoiseLevel = 0.2;       // 增加雜訊
    simulator.Config.MaxMotorSpeed = 200;    // 提高速度
    
    // 2. 訂閱事件
    simulator.OnTemperatureChanged += temp =>
    {
        Console.WriteLine($"溫度: {temp:F1}°C");
    };
    
    simulator.OnAlarmTriggered += alarm =>
    {
        MessageBox.Show($"警報: {alarm}");
    };
    
    // 3. 注入故障（用於自動化測試）
    simulator.InjectFault("TEMP_SENSOR_ERROR", 5000); // 5秒後觸發
    
    // 4. 查看統計
    var stats = simulator.Stats;
    Console.WriteLine($"運行: {stats.UptimeSeconds:F1}s");
    Console.WriteLine($"讀寫: {stats.TotalReads}/{stats.TotalWrites}");
}
```

---

## ?? 自動化測試範例

### 測試案例 1：溫控系統

```csharp
[Test]
public async Task Test_TemperatureControl()
{
    // 1. 設定目標溫度
    await simulator.WriteDeviceValueAsync("D12,800"); // 80°C
    
    // 2. 開啟加熱器
    await simulator.WriteDeviceValueAsync("M0,1");
    
    // 3. 等待 10 秒
    await Task.Delay(10000);
    
    // 4. 檢查溫度是否上升
    var temp = await simulator.ReadDWordAsync("D", 10);
    Assert.IsTrue(temp >= 500, "溫度應該至少達到 50°C");
    
    // 5. 檢查警報未觸發
    var alarm = await simulator.ReadBitAsync("M", 100);
    Assert.IsFalse(alarm, "不應該觸發高溫警報");
}
```

### 測試案例 2：Recipe 下載

```csharp
[Test]
public async Task Test_RecipeDownload()
{
    // 1. 載入 Recipe
    await RecipeContext.LoadRecipeAsync("Recipe1.json");
    
    // 2. 下載到 PLC
    await RecipeContext.DownloadCurrentRecipe();
    
    // 3. 驗證寫入
    var value1 = await simulator.ReadDWordAsync("D", 100);
    var value2 = await simulator.ReadDWordAsync("D", 101);
    
    Assert.AreEqual(500, value1);
    Assert.AreEqual(1000, value2);
    
    // 4. 檢查統計
    Assert.IsTrue(simulator.Stats.TotalWrites > 0);
}
```

### 測試案例 3：異常處理

```csharp
[Test]
public async Task Test_FaultHandling()
{
    // 1. 注入故障
    simulator.InjectFault("SERVO_ERROR", 0);
    
    // 2. 等待警報觸發
    bool alarmTriggered = false;
    simulator.OnAlarmTriggered += alarm => 
    {
        if (alarm.Contains("伺服")) 
            alarmTriggered = true;
    };
    
    await Task.Delay(1000);
    
    // 3. 驗證警報
    Assert.IsTrue(alarmTriggered);
    
    // 4. 檢查 M101
    var servo = await simulator.ReadBitAsync("M", 101);
    Assert.IsTrue(servo);
    
    // 5. 清除故障
    simulator.ClearFault("SERVO_ERROR");
    
    // 6. 確認恢復
    servo = await simulator.ReadBitAsync("M", 101);
    Assert.IsFalse(servo);
}
```

---

## ?? 完整工作流程

### 開發階段
```
1. 啟動 WpfApp1 (Debug 模式)
   ↓
2. 模擬器自動連線
   ↓
3. 控制面板自動開啟 (F12 重新開啟)
   ↓
4. 開始測試 UI 功能：
   - PlcLabel 讀取
   - SecuredButton 寫入
   - RecipeLoader 下載
   - SensorViewer 監控
   ↓
5. 使用控制面板測試異常：
   - 注入故障
   - 觀察反應
   - 驗證處理邏輯
   ↓
6. 查看統計資訊
```

### 測試階段
```
1. 編寫單元測試
   ↓
2. 使用模擬器執行
   ↓
3. 故障注入測試
   ↓
4. 驗證異常處理
   ↓
5. 生成測試報告
```

### 部署階段
```
1. 切換到 Release 模式
   ↓
2. PlcClientFactory.UseSimulator = false
   ↓
3. 連接真實 PLC
   ↓
4. 實際設備測試
```

---

## ?? 常見問題

### Q: 控制面板顯示「Simulator Not Found」？

**A:** 檢查以下項目：
1. `PlcClientFactory.UseSimulator` 是否設為 `true`
2. `PlcStatus` 是否已連線（綠燈）
3. Debug 輸出是否顯示「?? 模擬器模式」

### Q: PlcLabel 不更新？

**A:** 確認：
1. `PlcStatus` 的 `MonitorAddress` 包含對應位址
2. 模擬器已連線（查看控制面板）
3. 使用正確的位址（D10 vs D100）

### Q: 溫度不會上升？

**A:** 檢查：
1. 是否點擊了「Start Heater」
2. M0 是否為 1（使用 PlcLabel 確認）
3. 目標溫度 D12 是否設定

### Q: 如何恢復到真實 PLC？

**A:**
```csharp
// 方法 1：註解掉
// PlcClientFactory.UseSimulator = true;

// 方法 2：條件編譯
#if DEBUG
PlcClientFactory.UseSimulator = true;
#else
PlcClientFactory.UseSimulator = false;
#endif
```

---

## ?? 效能建議

### 最佳實踐

? **DO**
- 開發時使用模擬器（加速迭代）
- 使用控制面板即時測試
- 故障注入驗證異常處理
- 定期查看統計資訊

? **DON'T**
- 在生產環境啟用模擬器
- 過度依賴「完美模擬」
- 忽略真實設備的延遲和失敗

### 效能指標

| 項目 | 模擬器 | 真實 PLC |
|------|--------|----------|
| 連線時間 | < 10ms | 100-500ms |
| 讀寫延遲 | < 1ms | 10-50ms |
| CPU 使用 | < 1% | N/A |
| 記憶體 | < 10MB | N/A |

---

## ?? 進階功能

### 自訂物理參數

```csharp
simulator.Config.SimulationMode = "Testing";
simulator.Config.SimulationCycleMs = 100;  // 降低頻率
simulator.Config.HeatingRate = 1.0;        // 加快加熱
simulator.Config.AmbientTemperature = 20;  // 環境溫度
simulator.Config.HighTempThreshold = 120;  // 警報閾值
```

### 事件驅動測試

```csharp
// 溫度達到目標時自動執行
simulator.OnTemperatureChanged += temp =>
{
    if (temp >= 80.0)
    {
        // 自動停止加熱
        simulator.WriteDeviceValueAsync("M0,0");
    }
};
```

### 複雜場景模擬

```csharp
async Task SimulateProductionCycle()
{
    // 1. 預熱
    await simulator.WriteDeviceValueAsync("M0,1");
    await WaitForTemperature(80);
    
    // 2. 移動軸
    await simulator.WriteDeviceValueAsync("M1,1");
    await WaitForPosition(5000);
    
    // 3. 保持
    await Task.Delay(10000);
    
    // 4. 冷卻
    await simulator.WriteDeviceValueAsync("M0,0");
    await WaitForTemperature(30);
}
```

---

**文件版本**: v1.0  
**更新日期**: 2024-01  
**相關文件**: `Virtual-PLC-Simulator-Guide.md`
