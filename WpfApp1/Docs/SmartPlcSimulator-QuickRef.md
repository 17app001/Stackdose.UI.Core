# ?? SmartPlcSimulator 快速參考卡片

## ?? 一行代碼啟用

```csharp
// WpfApp1/App.xaml.cs
PlcClientFactory.UseSimulator = true;
```

---

## ?? 快捷鍵

| 快捷鍵 | 功能 |
|--------|------|
| **F12** | 開啟模擬器控制面板 |

---

## ?? 控制面板按鈕

| 按鈕 | 功能 | PLC 位址 |
|------|------|----------|
| ?? Start Heater | 開啟加熱 | M0 = 1 |
| ?? Stop Heater | 關閉加熱 | M0 = 0 |
| ?? Start Motor | 啟動馬達 | M1 = 1 |
| ?? Stop Motor | 停止馬達 | M1 = 0 |
| ?? Temp Error | 溫度異常 | 測試用 |
| ?? Servo Error | 伺服異常 | M101 = 1 |
| ? Clear All | 清除故障 | 恢復正常 |

---

## ?? PLC 位址映射表

### 溫度控制
| 位址 | 說明 | 範例 |
|------|------|------|
| **D10** | 當前溫度 (x10) | 250 = 25.0°C |
| **D12** | 目標溫度 (x10) | 800 = 80.0°C |
| **M0** | 加熱器開關 | 0=關, 1=開 |
| **M100** | 高溫警報 | 自動觸發 >100°C |

### 馬達控制
| 位址 | 說明 | 範例 |
|------|------|------|
| **D0** | X軸位置 (μm) | 0~10000 |
| **D2** | 馬達速度 | 0~100 |
| **M1** | 馬達運轉 | 0=停, 1=運轉 |
| **M101** | 伺服異常 | 故障注入 |

### 系統狀態
| 位址 | 說明 | Bit 定義 |
|------|------|----------|
| **D100** | 系統狀態碼 | Bit0:連線中<br>Bit1:加熱中<br>Bit2:馬達運轉<br>Bit7:故障 |

---

## ?? 即時監控數值

```
Real-time Status:
  Temperature: 25.0°C   (D10 / 10)
  Axis Position: 0 μm   (D0)

Statistics:
  運行 120.5s | 週期:2410 | R/W:1234/56 | 警報:3
  ↑          ↑          ↑          ↑
  運行時間    週期數      讀寫次數    警報次數
```

---

## ?? 程式化控制範例

### 取得模擬器實例
```csharp
if (MainPlc.CurrentManager?.PlcClient is SmartPlcSimulator sim)
{
    // 可以直接控制
}
```

### 調整物理參數
```csharp
sim.Config.HeatingRate = 0.5;      // 加熱速率
sim.Config.NoiseLevel = 0.1;       // 雜訊水平
sim.Config.MaxMotorSpeed = 100;    // 馬達速度
```

### 訂閱事件
```csharp
sim.OnTemperatureChanged += temp => 
{
    Console.WriteLine($"溫度: {temp:F1}°C");
};

sim.OnAlarmTriggered += alarm => 
{
    MessageBox.Show($"警報: {alarm}");
};
```

### 注入故障
```csharp
sim.InjectFault("TEMP_SENSOR_ERROR", 5000); // 5秒後
sim.InjectFault("SERVO_ERROR", 0);          // 立即
sim.ClearFault("TEMP_SENSOR_ERROR");        // 清除
sim.ClearAllFaults();                       // 全部清除
```

### 查看統計
```csharp
var stats = sim.Stats;
Console.WriteLine($"運行: {stats.UptimeSeconds:F1}s");
Console.WriteLine($"讀寫: {stats.TotalReads}/{stats.TotalWrites}");
Console.WriteLine($"警報: {stats.AlarmCount}");
Console.WriteLine($"溫度: {stats.LastTemperature:F1}°C");
Console.WriteLine($"位置: {stats.LastAxisPosition:F0} μm");
```

---

## ?? 測試流程

### 1?? 溫度測試
```
1. 按 F12 開啟控制面板
2. 點擊「Start Heater」
3. 觀察主視窗 PlcLabel (R2002)
4. 溫度從 25°C 逐漸上升
5. 接近目標溫度 (80°C) 時變慢
```

### 2?? 馬達測試
```
1. 點擊「Start Motor」
2. 觀察 D100 位置增加
3. 速度逐漸加速到 100
4. 到達 10000 自動停止
```

### 3?? Recipe 測試
```
1. 使用 RecipeLoader 選擇 Recipe
2. 點擊「下載到 PLC」
3. 使用 PlcLabel 驗證值
4. 查看控制面板統計資訊
```

### 4?? 故障測試
```
1. 點擊「Temp Error」
2. 觀察溫度讀數跳動
3. 測試異常處理邏輯
4. 點擊「Clear All」恢復
```

---

## ?? 配置選項

### 物理參數 (Config)
```csharp
SimulationCycleMs      = 50    // 週期 (ms)
HeatingRate           = 0.3    // 加熱速率 (°C/週期)
CoolingRate           = 0.1    // 冷卻速率 (°C/週期)
AmbientTemperature    = 25.0   // 環境溫度 (°C)
HighTempThreshold     = 100.0  // 警報閾值 (°C)
NoiseLevel            = 0.1    // 雜訊水平 (°C)
MaxMotorSpeed         = 100.0  // 最大速度
MotorAcceleration     = 2.0    // 加速度
MaxAxisPosition       = 10000  // 最大行程 (μm)
```

---

## ?? 故障排除

### ? 控制面板顯示「Simulator Not Found」
```
檢查：
1. PlcClientFactory.UseSimulator = true
2. PlcStatus 已連線（綠燈）
3. Debug 輸出顯示「?? 模擬器模式」
```

### ? PlcLabel 不更新
```
檢查：
1. MonitorAddress 包含對應位址
2. 模擬器已連線
3. 使用正確的位址映射
```

### ? 溫度不上升
```
檢查：
1. 點擊了「Start Heater」
2. M0 = 1 (使用 PlcLabel 確認)
3. D12 目標溫度已設定
```

---

## ?? 最佳實踐

### ? DO
- ? 開發時使用模擬器（加速迭代）
- ? 使用 F12 快速開啟控制面板
- ? 故障注入測試異常處理
- ? 查看統計資訊監控效能

### ? DON'T
- ? 在生產環境啟用模擬器
- ? 依賴模擬器的「完美行為」
- ? 忽略真實設備的延遲和失敗

---

## ?? 效能指標

| 項目 | 數值 |
|------|------|
| 模擬週期 | 50ms (20Hz) |
| 溫度精度 | 0.1°C |
| 位置精度 | 1μm |
| 記憶體 | < 10MB |
| CPU | < 1% |

---

## ?? 相關文件

- ?? **完整指南**: `Virtual-PLC-Simulator-Guide.md`
- ?? **使用範例**: `SmartPlcSimulator-Usage-Examples.md`
- ?? **API 文件**: 查看程式碼 XML 註解

---

**版本**: v1.0  
**更新**: 2024-01  
**支援**: GitHub Issues
