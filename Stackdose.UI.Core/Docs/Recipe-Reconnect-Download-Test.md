# Recipe 重連線自動下載測試指南

## 問題描述

**PLC 第一次連線時**，Recipe 會自動下載到 PLC ?

**PLC 斷線後重新連線時**，Recipe **不會**自動下載到 PLC ?

## 測試步驟

### 測試案例 1：第一次連線（應該成功）

1. 啟動 WpfApp1
2. 點擊 PlcStatus 的 **Connect** 按鈕
3. 查看日誌輸出

**預期日誌**：
```
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:2,D103:1,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
[PlcStatus] Triggering ConnectionEstablished event...
[PlcStatus] ConnectionEstablished event triggered. Subscriber count: 1
[MainWindow] OnPlcConnectionEstablished called!
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
[Recipe Download] Cooling Water Pressure (D103) = 35 bar
...
[Recipe] Download completed successfully: 10/10 parameters written
[Recipe] Auto-downloaded to PLC: 10 parameters written
System initialized. Main PLC set.
```

**關鍵日誌**：
- ? `[MainWindow] OnPlcConnectionEstablished called!` → 事件被觸發
- ? `[Recipe] Auto-downloaded to PLC: 10 parameters written` → Recipe 下載成功

### 測試案例 2：重連線（問題場景）

1. 點擊 PlcStatus 的 **Disconnect** 按鈕
2. 等待 2 秒
3. 點擊 PlcStatus 的 **Connect** 按鈕
4. 查看日誌輸出

**預期日誌**（修復後）：
```
[PLC] Disconnecting...
[PLC] Disconnected
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:2,D103:1,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
[PlcStatus] Triggering ConnectionEstablished event...
[PlcStatus] ConnectionEstablished event triggered. Subscriber count: 1
[MainWindow] OnPlcConnectionEstablished called!  ← ? 應該出現！
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
...
[Recipe] Download completed successfully: 10/10 parameters written
[Recipe] Auto-downloaded to PLC: 10 parameters written  ← ? 應該出現！
```

**如果沒有出現這些日誌**：
- ? `[MainWindow] OnPlcConnectionEstablished called!` 沒出現 → 事件沒被觸發
- ? `[Recipe] Auto-downloaded to PLC: 10 parameters written` 沒出現 → Recipe 沒下載

**可能原因**：
1. 事件訂閱被取消
2. 事件沒有被觸發
3. 事件處理中發生異常

## 診斷步驟

### 1. 檢查事件訂閱數量

查看日誌中的：
```
[PlcStatus] ConnectionEstablished event triggered. Subscriber count: 1
```

**說明**：
- `Subscriber count: 0` → ? 沒有訂閱者
- `Subscriber count: 1` → ? 有 1 個訂閱者（MainWindow）
- `Subscriber count: 2+` → ?? 有多個訂閱者（可能重複訂閱）

### 2. 檢查 MainWindow 事件處理

如果看到：
```
[MainWindow] OnPlcConnectionEstablished called!
```

表示事件已觸發，繼續檢查下一步。

如果**沒有看到**，表示：
- 事件沒有被訂閱，或
- 訂閱被取消了

**檢查方法**：
```csharp
// MainWindow.xaml.cs
public MainWindow()
{
    InitializeComponent();
    
    // ? 確認這行代碼存在
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
}
```

### 3. 檢查 Recipe 下載

如果看到：
```
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
...
[Recipe] Auto-downloaded to PLC: 10 parameters written
```

表示 Recipe 下載成功 ?

如果**沒有看到**，檢查：
- Recipe 是否已載入？ → `RecipeContext.HasActiveRecipe`
- PLC 是否已連線？ → `plcManager.IsConnected`
- 是否有異常？

## 可能的問題和解決方案

### 問題 1：事件訂閱被取消

**現象**：
```
[PlcStatus] Subscriber count: 0
```

**原因**：
- MainWindow 被重建或釋放
- 事件訂閱被手動取消

**解決方案**：
確保在 MainWindow 的建構函式中訂閱事件：
```csharp
MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
```

### 問題 2：事件沒有被觸發

**現象**：
- 看不到 `[PlcStatus] Triggering ConnectionEstablished event...`

**原因**：
- PlcStatus 的 ConnectAsync 沒有執行到觸發事件的地方
- 連線失敗

**解決方案**：
檢查連線是否成功：
```
PLC Connection Established (192.168.22.39)
```

### 問題 3：事件處理中發生異常

**現象**：
```
[MainWindow] OnPlcConnectionEstablished called!
```
但沒有看到後續的 Recipe 下載日誌。

**原因**：
- Recipe 載入失敗
- PLC 連線已斷開
- 異常被吞掉

**解決方案**：
檢查日誌中是否有錯誤訊息：
```
[ERROR] ...
```

### 問題 4：Recipe 沒有載入

**現象**：
```
[Recipe] Auto-loading Recipe after PLC connection...
```
但沒有看到 Recipe 下載日誌。

**原因**：
- Recipe.json 檔案不存在
- Recipe 載入失敗

**解決方案**：
手動載入 Recipe：
```csharp
bool success = await RecipeContext.LoadRecipeAsync("Recipe.json");
```

## 額外的診斷日誌

### MainWindow 中的事件訂閱

在 MainWindow 的建構函式中添加日誌：
```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ...existing code...
    
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
    
    ComplianceContext.LogSystem(
        "[MainWindow] Subscribed to ConnectionEstablished event",
        LogLevel.Info,
        showInUi: false
    );
}
```

### PlcStatus 中的事件觸發

已經添加了詳細的日誌：
```csharp
ComplianceContext.LogSystem(
    "[PlcStatus] Triggering ConnectionEstablished event...",
    LogLevel.Info,
    showInUi: false
);

ConnectionEstablished?.Invoke(_plcManager);

ComplianceContext.LogSystem(
    $"[PlcStatus] ConnectionEstablished event triggered. Subscriber count: {ConnectionEstablished?.GetInvocationList().Length ?? 0}",
    LogLevel.Info,
    showInUi: false
);
```

## 預期的完整日誌流程

### 第一次連線

```
[Application Start]
[MainWindow] Subscribed to ConnectionEstablished event
[Recipe] Initializing Recipe system...
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)

[User clicks Connect]
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:2,D103:1,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
[PlcStatus] Triggering ConnectionEstablished event...
[PlcStatus] ConnectionEstablished event triggered. Subscriber count: 1
[MainWindow] OnPlcConnectionEstablished called!
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
[Recipe Download] Cooling Water Pressure (D103) = 35 bar
[Recipe Download] Conveyor Speed (D104) = 120 mm/s
[Recipe Download] Mixer RPM (D106) = 450 RPM
[Recipe Download] Pressure Time (D110) = 30 sec
[Recipe Download] Holding Time (D112) = 120 sec
[Recipe Download] Cooling Time (D114) = 90 sec
[Recipe Download] Material A Dosage (D120) = 250 g
[Recipe Download] Material B Dosage (D122) = 150 g
[Recipe Download] Alarm Critical Temp (D200) = 200 °C
[Recipe] Download completed successfully: 10/10 parameters written
[Recipe] Auto-downloaded to PLC: 10 parameters written
System initialized. Main PLC set.
```

### 重連線（應該與第一次連線相同）

```
[User clicks Disconnect]
[PLC] Disconnecting...
[PLC] Disconnected

[User clicks Connect]
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:2,D103:1,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
[PlcStatus] Triggering ConnectionEstablished event...
[PlcStatus] ConnectionEstablished event triggered. Subscriber count: 1
[MainWindow] OnPlcConnectionEstablished called!
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
...
[Recipe] Download completed successfully: 10/10 parameters written
[Recipe] Auto-downloaded to PLC: 10 parameters written
```

**關鍵**：兩次日誌應該幾乎相同！

## 總結

### 檢查清單

- [ ] 查看 `[MainWindow] OnPlcConnectionEstablished called!` 是否出現
- [ ] 查看 `[PlcStatus] Subscriber count: X` 的數量
- [ ] 查看 `[Recipe] Auto-downloaded to PLC: X parameters written` 是否出現
- [ ] 查看是否有錯誤日誌

### 如果問題仍然存在

請提供完整的日誌輸出，包括：
1. 第一次連線的日誌
2. 斷線的日誌
3. 重連線的日誌

這樣可以幫助診斷問題的根本原因。

### 預期結果

**修復後**：
- ? 第一次連線 → Recipe 自動下載
- ? 重連線 → Recipe 自動下載
- ? 每次連線都會觸發 `ConnectionEstablished` 事件
- ? 每次連線都會執行 Recipe 下載
