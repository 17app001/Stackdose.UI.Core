# ??? PrintHeadStatus ?度?示??指南

## ? **修改完成摘要**

### **改??容：**

1. ? **?化日志?出** - 避免日志爆炸
   - 只在第一次成功?取???日志
   - ??信息只??前 3 次

2. ? **降低刷新?率** - ? 200ms 改? 500ms
   - ?少 CPU 占用
   - 降低网?流量

3. ? **?度?据流确?**
   - `GetTemperatures()` → 返回 `float[]`
   - `UpdateTemperatureDisplay()` → 更新 UI
   - `TemperaturesPanel` → ?示?度

---

## ?? **??步?**

### **Step 1: ?行 WpfAppPrintHead**

```bash
cd WpfAppPrintHead
dotnet run
```

### **Step 2: ?? Connect 按?**

?察日志?出：

```
[PrintHead] Connecting to A-Head1 (192.168.22.68:10000)...
[Setup] Starting setup for A-Head1...
[Setup] ? Firmware configured successfully
? [Feiyang] 列印模式設定成功 (DPI=600)
[PrintHead] ? Connection established: A-Head1
[PrintHead] ??? Temperature monitoring started
[PrintHead] ? First temperature read: 38.5°C, 39.2°C
```

### **Step 3: ?查 UI ?示**

在 `PrintHeadStatus` 控件的底部?度?域，??看到：

```
??? Temperatures
┌──────────────┬──────────────┐
│ CH1: 38.5°C  │ CH2: 39.2°C  │
└──────────────┴──────────────┘
```

**?度??每 500ms 自?更新！**

---

## ? **故障排查**

### **情? 1：?度?示 "N/A" 或空白**

**原因：**
- `GetTemperatures()` 返回空??
- `ReadStatusRaw()` 返回 `null`

**解?方法：**
1. ?查日志是否?示 `?? Temperature read returned empty`
2. 确?噴頭是否真正??成功（看到 "CONNECTED" ?色??）
3. ?查 SDK 是否正常工作

**??命令：**
```csharp
// 在 GetTemperatures() 中添加??日志
LogInfo($"[DEBUG] ReadStatusRaw: {s?.InkTemperatureA}, {s?.PrintheadTemperatureA}");
```

---

### **情? 2：?度?示 0°C**

**原因：**
- 噴頭加?器未??
- SDK 返回的?度字段不正确

**解?方法：**
1. 确?加??度?置：
```json
"HeatTemperature": 40.0
```

2. ?查 SDK ???构：
```csharp
var s = _native.ReadStatusRaw();
Console.WriteLine($"InkTempA={s.InkTemperatureA}, HeadTempA={s.PrintheadTemperatureA}");
```

---

### **情? 3：日志?示?度?取成功，但 UI ?有更新**

**原因：**
- `Dispatcher.Invoke()` 失?
- `TemperaturesPanel` ?定??

**解?方法：**
```csharp
// 在 UpdateTemperatureDisplay 中添加日志
ComplianceContext.LogSystem(
    $"[DEBUG] TemperaturesPanel.ItemsSource set to: {string.Join(", ", displayList)}",
    LogLevel.Info
);
```

---

## ?? **?期?行效果**

### **??成功后：**

1. ? **???示** - "CONNECTED" ?色
2. ? **?度?控??** - 日志?示 "Temperature monitoring started"
3. ? **首次?取成功** - 日志?示 "First temperature read: XX°C, XX°C"
4. ? **UI ??更新** - ?度每 500ms 刷新一次

### **UI 界面：**

```
┌────────────────────────────────────────┐
│ Feiyang Head 1           【CONNECTED】 │
├────────────────────────────────────────┤
│ Config: feiyang_head1.json             │
│ Board: 192.168.22.68:10000             │
│ Model: Feiyang-M1536                   │
│                                        │
│ ??? Temperatures                        │
│ ┌──────────────┬──────────────┐        │
│ │ CH1: 38.5°C  │ CH2: 39.2°C  │        │
│ └──────────────┴──────────────┘        │
├────────────────────────────────────────┤
│ [  Connect  ]  [ Disconnect ]          │
└────────────────────────────────────────┘
```

---

## ?? **性能优化**

### **?前配置：**
- 刷新?率：500ms（每秒 2 次）
- 日志?率：?首次成功 + 前 3 次??

### **可?整??：**

```csharp
// 更快刷新（适合???控）
await Task.Delay(200, token);  // 每秒 5 次

// 更慢刷新（适合省?源）
await Task.Delay(1000, token); // 每秒 1 次
```

---

## ?? **下一步改?**

1. **?度告警** - 超??值??示?色警告
2. **?度曲??** - 使用 LiveCharts ?示?度??
3. **多通道支持** - ?示 4 ?通道的?度（InkTempA/B, HeadTempA/B）
4. **?史??** - ???度到 SQLite ?据?

---

**Created:** 2025-01-10  
**Status:** ? Ready for Testing
