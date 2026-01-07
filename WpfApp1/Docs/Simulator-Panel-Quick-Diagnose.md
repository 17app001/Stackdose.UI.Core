# ?? 模擬器控制面板診斷指南（快速版）

## ? 已加入的改進

### 1. **新增「Open Simulator」按鈕**
位置：Permission Test 區域底部  
功能：手動開啟模擬器控制面板  
快捷鍵：F12

### 2. **詳細的診斷訊息**
所有關鍵步驟都會輸出到 Output 視窗（Debug）

### 3. **錯誤提示**
如果無法開啟，會顯示詳細的錯誤訊息

---

## ?? 立即測試

### **步驟 1：啟動程式**
```
按 F5 啟動 WpfApp1
```

### **步驟 2：查看 Output 視窗**
打開 `View` → `Output` 視窗，查找：

```
[MainWindow_Loaded] 開始檢查模擬器...
[MainWindow_Loaded] PlcClientFactory.UseSimulator = True    ← 應該是 True
[MainWindow_Loaded] MainPlc = True
[MainWindow_Loaded] CurrentManager = True
[MainWindow_Loaded] PlcClient Type = SmartPlcSimulator      ← 應該是這個
[MainWindow_Loaded] IsConnected = True                      ← 應該是 True
[MainWindow_Loaded] Is Simulator = True                     ← 應該是 True
```

### **步驟 3：等待自動彈出**
- 如果一切正常，模擬器控制面板會在 **1-2 秒後自動彈出**
- LiveLogViewer 會顯示：
  ```
  ?? 偵測到模擬器模式，準備開啟控制面板...
  ? PLC 已連線，正在開啟模擬器控制面板...
  ?? 模擬器控制面板已開啟
  ```

### **步驟 4：手動開啟（如果沒有自動彈出）**
- 點擊 **「?? Open Simulator (F12)」** 按鈕
- 或按 **F12** 鍵

---

## ?? 常見問題排查

### ? 問題 1: Output 顯示 `PlcClientFactory.UseSimulator = False`

**原因**：模擬器未啟用

**解決方法**：
1. 打開 `WpfApp1/App.xaml.cs`
2. 檢查是否有：
```csharp
#if DEBUG
PlcClientFactory.UseSimulator = true;
#endif
```
3. 確認在 **Debug 模式** 下編譯（不是 Release）

---

### ? 問題 2: `PlcClient Type = FX3UPlcClient`

**原因**：PlcManager 使用了真實 PLC 客戶端

**解決方法**：
PlcManager 的建立必須在 `PlcClientFactory.UseSimulator = true` **之後**

確認 `App.xaml.cs` 中：
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    #if DEBUG
    PlcClientFactory.UseSimulator = true;  // ← 這行必須在最前面
    #endif
    
    base.OnStartup(e);  // ← MainWindow 建立在這之後
}
```

---

### ? 問題 3: `IsConnected = False`

**原因**：PLC 尚未連線

**解決方法**：
1. 等待 2-3 秒（自動連線需要時間）
2. 查看 PlcStatus 控制項是否變成綠燈
3. 手動點擊 PlcStatus 連線

---

### ? 問題 4: 按「Open Simulator」出現錯誤

**查看錯誤訊息**，通常會顯示：
- "當前不是模擬器模式" → 回到問題 1
- "PLC 尚未連線" → 回到問題 3
- "發生錯誤" → 查看 Output 視窗的詳細錯誤

---

## ?? 完整診斷流程

### 執行診斷代碼

在 `MainWindow.xaml.cs` 加入測試按鈕：

```csharp
private void DiagnoseSimulator_Click(object sender, RoutedEventArgs e)
{
    var diag = new System.Text.StringBuilder();
    diag.AppendLine("?? 模擬器診斷報告\n");
    
    diag.AppendLine($"1. PlcClientFactory.UseSimulator = {Stackdose.Hardware.Plc.PlcClientFactory.UseSimulator}");
    diag.AppendLine($"2. MainPlc 存在 = {MainPlc != null}");
    diag.AppendLine($"3. CurrentManager 存在 = {MainPlc?.CurrentManager != null}");
    diag.AppendLine($"4. PLC Client Type = {MainPlc?.CurrentManager?.PlcClient?.GetType().Name ?? "null"}");
    diag.AppendLine($"5. IsConnected = {MainPlc?.CurrentManager?.IsConnected}");
    diag.AppendLine($"6. Is Simulator = {MainPlc?.CurrentManager?.PlcClient is SmartPlcSimulator}");
    diag.AppendLine($"7. GlobalStatus 存在 = {PlcContext.GlobalStatus != null}");
    
    MessageBox.Show(diag.ToString(), "診斷報告");
}
```

---

## ? 預期結果

### **正常情況下，您會看到：**

#### Output 視窗：
```
[MainWindow_Loaded] 開始檢查模擬器...
[MainWindow_Loaded] PlcClientFactory.UseSimulator = True
[MainWindow_Loaded] MainPlc = True
[MainWindow_Loaded] CurrentManager = True
[MainWindow_Loaded] PlcClient Type = SmartPlcSimulator
[MainWindow_Loaded] IsConnected = False
[MainWindow_Loaded] Is Simulator = True
[MainWindow_Loaded] Retry 1/20, IsConnected = False
[MainWindow_Loaded] Retry 2/20, IsConnected = True
[MainWindow_Loaded] ? PLC 已連線，準備開啟模擬器面板...
[OpenSimulatorWindow] 開始開啟模擬器視窗...
[OpenSimulatorWindow] 建立新的 SimulatorWindow
[OpenSimulatorWindow] ? SimulatorWindow 已顯示
```

#### 主視窗：
- PlcStatus 顯示綠燈 "CONNECTED"
- LiveLogViewer 顯示連線成功訊息

#### 模擬器控制面板：
- 自動彈出
- 顯示溫度 25.0°C
- 顯示統計資訊

---

## ?? 快速測試清單

- [ ] 啟動 WpfApp1
- [ ] 查看 Output 視窗診斷訊息
- [ ] PlcStatus 變成綠燈（約 1-2 秒）
- [ ] 模擬器控制面板自動彈出（約 2 秒後）
- [ ] 如果沒彈出，點擊「Open Simulator」按鈕
- [ ] 按 F12 測試快捷鍵
- [ ] 點擊「Start Heater」測試溫度上升

---

## ?? 仍然無法解決？

### 請提供以下資訊：

1. **Output 視窗的完整內容**（從 `[MainWindow_Loaded]` 開始）
2. **PlcStatus 的顯示狀態**（顏色、文字）
3. **點擊「Open Simulator」按鈕的錯誤訊息**
4. **診斷報告的內容**（如果有執行）

---

**版本**: v2.0 - 加強診斷  
**更新**: 2024-01  
**快速開啟**: 點擊「?? Open Simulator」按鈕或按 F12
