# PrintHeadStatus ?度?示??指南

## ?? ??描述
??成功后，?度?有?示。需要确?：
1. ?度?取是否成功
2. UI 是否正确更新
3. ?据流是否完整

---

## ?? 修改?容

### 1?? **增? `FeiyangPrintHead.GetTemperatures()`**
```csharp
public float[] GetTemperatures()
{
    if (_native == null)
    {
        LogInfo("?? GetTemperatures: _native is null");
        return Array.Empty<float>();
    }

    if (!_connected)
    {
        LogInfo("?? GetTemperatures: Not connected");
        return Array.Empty<float>();
    }

    try
    {
        LogInfo("[GetTemperatures] Reading status...");
        var s = _native.ReadStatusRaw();
        
        if (s == null)
        {
            LogInfo("?? GetTemperatures: ReadStatusRaw returned null");
            return Array.Empty<float>();
        }

        float inkTemp = (float)s.InkTemperatureA;
        float headTemp = (float)s.PrintheadTemperatureA;
        
        LogInfo($"? GetTemperatures: Ink={inkTemp:F1}°C, Head={headTemp:F1}°C");
        
        return new[] { inkTemp, headTemp };
    }
    catch (Exception ex)
    {
        LogInfo($"? 讀取溫度失敗: {ex.Message}\nStack: {ex.StackTrace}");
        return Array.Empty<float>();
    }
}
```

**改??：**
- ? 添加??的???查
- ? ??每次?度?取?果
- ? 捕?并??异常堆?

---

### 2?? **增??度?控?程日志**
```csharp
private void StartTemperatureMonitoring()
{
    StopTemperatureMonitoring();
    _temperatureMonitorCts = new CancellationTokenSource();
    var token = _temperatureMonitorCts.Token;

    ComplianceContext.LogSystem(
        "[PrintHead] ??? Starting temperature monitoring...",
        LogLevel.Info,
        showInUi: false
    );

    Task.Run(async () =>
    {
        int successCount = 0;
        int errorCount = 0;

        while (!token.IsCancellationRequested && _isConnected)
        {
            try
            {
                var temps = _printHead?.GetTemperatures();

                if (temps != null && temps.Length > 0)
                {
                    successCount++;
                    if (successCount % 10 == 0)
                    {
                        ComplianceContext.LogSystem(
                            $"[PrintHead] Temperature read OK (count: {successCount}, errors: {errorCount})",
                            LogLevel.Info,
                            showInUi: false
                        );
                    }

                    Dispatcher.Invoke(() =>
                    {
                        UpdateTemperatureDisplay(temps);
                    });
                }
                else
                {
                    errorCount++;
                    ComplianceContext.LogSystem(
                        $"[PrintHead] ?? Temperature read returned empty (error count: {errorCount})",
                        LogLevel.Warning,
                        showInUi: false
                    );
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                ComplianceContext.LogSystem(
                    $"[PrintHead] ? Temperature read error: {ex.Message}",
                    LogLevel.Warning,
                    showInUi: false
                );
            }

            await Task.Delay(200, token);
        }

        ComplianceContext.LogSystem(
            $"[PrintHead] Monitoring stopped (success: {successCount}, errors: {errorCount})",
            LogLevel.Info,
            showInUi: false
        );
    }, token);
}
```

**改??：**
- ? 添加成功/失???器
- ? 每 10 次成功?取??一次（避免日志?多）
- ? ???控??/停止事件

---

### 3?? **?置 Log 委托**
```csharp
private async Task ConnectAsync()
{
    // ...

    // ? ?例化 FeiyangPrintHead
    _printHead = new FeiyangPrintHead(ConfigFilePath);
    
    // ? ?置日志委托
    _printHead.Log = (msg) =>
    {
        ComplianceContext.LogSystem(msg, LogLevel.Info, showInUi: false);
    };
    
    // 建立??...
}
```

**重要性：**
- 如果?有?置 `Log` 委托，`FeiyangPrintHead` 的日志不??出
- ?在所有日志都???到 `ComplianceContext`

---

## ?? ??步?

### **步? 1：?行 WpfAppPrintHead**
```bash
# ???目
dotnet run --project WpfAppPrintHead
```

### **步? 2：?? Connect 按?**
?察日志?出：

#### **?期日志?序：**
```
[PrintHead] Connecting to A-Head1 (192.168.22.68:10000)...
[Setup] Starting setup for A-Head1...
[PrintHead] Socket connected, configuring Firmware...
[Setup] ? Firmware configured successfully
[PrintHead] Firmware configured, setting print mode...
? [Feiyang] 列印模式設定成功 (DPI=600)
[PrintHead] ? Connection established: A-Head1
[PrintHead] ??? Starting temperature monitoring...
[GetTemperatures] Reading status...
? GetTemperatures: Ink=38.5°C, Head=39.2°C
[PrintHead] UI updated: CH1: 38.5°C, CH2: 39.2°C
[PrintHead] Temperature read OK (count: 10, errors: 0)
```

### **步? 3：?查 UI ?示**
在 `PrintHeadStatus` 控件的底部?度?域，??看到：

```
??? Temperatures
┌──────────────┬──────────────┐
│ CH1: 38.5°C  │ CH2: 39.2°C  │
└──────────────┴──────────────┘
```

---

## ? 常???排查

### **?? 1：日志?示 "Temperature read returned empty array"**

**原因：**
- `_native.ReadStatusRaw()` 返回 `null`
- SDK ?取失?

**解?方法：**
1. ?查噴頭是否真正??成功
2. 确? `Setup()` ?行成功
3. ?查 SDK 版本是否匹配

---

### **?? 2：日志?示?度?取成功，但 UI ?有?示**

**原因：**
- `Dispatcher.Invoke()` 失?
- `TemperaturesPanel` ?定??

**解?方法：**
```csharp
// ?查 UI ?程?用
Dispatcher.Invoke(() =>
{
    UpdateTemperatureDisplay(temps);
    
    // 添加?外?查
    if (TemperaturesPanel.ItemsSource == null)
    {
        ComplianceContext.LogSystem(
            "[UI] ?? TemperaturesPanel.ItemsSource is null",
            LogLevel.Warning
        );
    }
});
```

---

### **?? 3：?度一直?示 0°C**

**原因：**
- 噴頭加?器未??
- SDK 返回的?度字段不正确

**解?方法：**
```csharp
// ?查完整的???构
var s = _native.ReadStatusRaw();
LogInfo($"Full Status: InkTempA={s.InkTemperatureA}, HeadTempA={s.PrintheadTemperatureA}");
```

---

## ?? 性能优化

### **?整?度刷新?率**
```csharp
// ?前：每 200ms 更新一次
await Task.Delay(200, token);

// 可?整?：
await Task.Delay(500, token);  // 每 500ms（更省?源）
await Task.Delay(100, token);  // 每 100ms（更即?）
```

### **?少日志?出**
```csharp
// 每 50 次成功??一次（而不是 10 次）
if (successCount % 50 == 0)
{
    ComplianceContext.LogSystem(...);
}
```

---

## ? ??清?

- [ ] ?? Connect 后日志?示 "Starting temperature monitoring"
- [ ] 日志?示 "GetTemperatures: Ink=XX°C, Head=XX°C"
- [ ] UI ?示?度?值（不是 N/A 或空白）
- [ ] ?度?值持?更新（每 200ms）
- [ ] ??后?度?控停止

---

## ?? 下一步

如果?度?示正常，可以????：

1. **?度告警** - 超??值??示警告
2. **?度曲??** - 使用 LiveCharts ?示?度??
3. **自?保存?度日志** - ??到 SQLite ?据?
4. **多噴頭支持** - 同??控多?噴頭?度

---

**Created:** 2025-01-10  
**Author:** Copilot  
**Status:** ? Complete
