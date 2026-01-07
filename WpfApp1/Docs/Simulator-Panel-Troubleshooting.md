# ?? 模擬器控制面板未彈出 - 故障排除指南

## ? 已修正的問題

### 1. PlcStatus 缺少 IsGlobal 屬性
**原因**: `MainWindow.xaml` 中的 PlcStatus 沒有設定 `IsGlobal="True"`  
**影響**: PlcContext.GlobalStatus 為 null，導致 SimulatorWindow 無法找到模擬器  
**修正**: 已加入 `IsGlobal="True"`

### 2. AutoConnect 設為 False
**原因**: AutoConnect="False" 導致 PLC 不會自動連線  
**影響**: 模擬器不會啟動，控制面板無法綁定  
**修正**: 已改為 `AutoConnect="True"`

### 3. 模擬器開啟邏輯不夠穩健
**原因**: 沒有檢查 PLC 連線狀態就嘗試開啟面板  
**影響**: 面板開啟但無法綁定模擬器  
**修正**: 加入連線狀態檢查和重試邏輯

---

## ?? 現在應該會正常運作

### 執行步驟

1. **啟動 WpfApp1** (按 F5)
2. **等待 PLC 連線**（約 1-2 秒）
   - 觀察 PlcStatus 控制項變成綠燈
   - StatusText 顯示 "CONNECTED"
3. **模擬器控制面板會自動彈出**（約 2 秒後）
4. **開始測試**

---

## ?? 如果還是沒有彈出，請檢查以下項目

### 檢查清單

#### ? 1. 確認模擬器已啟用

**查看 Output 視窗（Debug）**：
```
?? [PlcClientFactory] 使用虛擬 PLC 模擬器
?? 偵測到模擬器模式，準備開啟控制面板...
? 模擬器控制面板已成功綁定
```

如果沒有看到這些訊息：
- 檢查 `App.xaml.cs` 是否有 `PlcClientFactory.UseSimulator = true;`
- 確認在 DEBUG 模式下編譯（不是 Release）

---

#### ? 2. 確認 PLC 已連線

**查看主視窗**：
- PlcStatus 控制項應該是 **綠燈**
- StatusText 顯示 "CONNECTED" 或 "ONLINE (XXms)"

如果未連線：
- 點擊 PlcStatus 手動連線
- 查看 Output 視窗的錯誤訊息

---

#### ? 3. 手動開啟模擬器面板

**按 F12** 隨時開啟模擬器控制面板

如果按 F12 沒反應：
- 確認主視窗有焦點（不是被其他視窗遮住）
- 查看 Output 視窗是否有錯誤訊息

---

#### ? 4. 檢查 SimulatorWindow 是否存在

**在 Solution Explorer 中確認**：
```
WpfApp1/
├── SimulatorWindow.xaml
├── SimulatorWindow.xaml.cs
```

如果檔案不存在：
- 檔案已建立，重新編譯即可

---

#### ? 5. 檢查 SimulatorControlPanel 是否存在

**在 Solution Explorer 中確認**：
```
Stackdose.UI.Core/Controls/
├── SimulatorControlPanel.xaml
├── SimulatorControlPanel.xaml.cs
```

如果檔案不存在：
- 檔案已建立，重新編譯即可

---

## ?? 診斷命令

### 在 MainWindow_Loaded 加入診斷代碼

```csharp
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    await Task.Delay(500);
    
    // ?? 診斷 1: 檢查模擬器模式
    System.Diagnostics.Debug.WriteLine($"?? PlcClientFactory.UseSimulator = {Stackdose.Hardware.Plc.PlcClientFactory.UseSimulator}");
    
    // ?? 診斷 2: 檢查 PLC 類型
    var plcType = MainPlc?.CurrentManager?.PlcClient?.GetType().Name ?? "null";
    System.Diagnostics.Debug.WriteLine($"?? PLC Client Type = {plcType}");
    
    // ?? 診斷 3: 檢查連線狀態
    System.Diagnostics.Debug.WriteLine($"?? IsConnected = {MainPlc?.CurrentManager?.IsConnected}");
    
    // ?? 診斷 4: 檢查 GlobalStatus
    System.Diagnostics.Debug.WriteLine($"?? GlobalStatus = {PlcContext.GlobalStatus != null}");
}
```

---

## ?? 預期輸出（正常情況）

### Output 視窗應該顯示：

```
========== Application Starting ==========
?? [App] 開發模式：已啟用 PLC 模擬器
System initialized. Main PLC set.
?? [PlcClientFactory] 使用虛擬 PLC 模擬器
Connecting to PLC (192.168.22.39:3000)...
[SmartPlcSimulator] 已連線 (模擬模式) - Manufacturing
PLC Connection Established (192.168.22.39)
[PlcStatus] Triggering ConnectionEstablished event...

?? PlcClientFactory.UseSimulator = True
?? PLC Client Type = SmartPlcSimulator
?? IsConnected = True
?? GlobalStatus = True

?? 偵測到模擬器模式，準備開啟控制面板...
?? 模擬器控制面板已開啟
? 模擬器控制面板已成功綁定
```

---

## ?? 常見錯誤訊息

### ? 錯誤 1: "無法連接到模擬器"

**原因**: PlcStatus 尚未連線或不是使用模擬器

**解決方法**:
1. 確認 `PlcClientFactory.UseSimulator = true`
2. 點擊 PlcStatus 手動連線
3. 查看 PLC Client Type 是否為 `SmartPlcSimulator`

---

### ? 錯誤 2: "Simulator Not Found"

**原因**: PlcContext.GlobalStatus 為 null

**解決方法**:
1. 確認 PlcStatus 有設定 `IsGlobal="True"`
2. 重新啟動程式
3. 查看 Output 視窗確認 "System initialized. Main PLC set."

---

### ? 錯誤 3: 控制面板開啟但數值不更新

**原因**: SimulatorControlPanel 綁定失敗

**解決方法**:
1. 按 F12 關閉面板
2. 確認 PLC 已連線（綠燈）
3. 再按 F12 重新開啟

---

## ??? 手動測試流程

### 測試 1: 確認模擬器已啟用

```csharp
// 在 MainWindow 加入按鈕測試
private void TestSimulator_Click(object sender, RoutedEventArgs e)
{
    bool isSimulator = MainPlc?.CurrentManager?.PlcClient is SmartPlcSimulator;
    
    MessageBox.Show(
        $"模擬器模式: {isSimulator}\n" +
        $"PLC 類型: {MainPlc?.CurrentManager?.PlcClient?.GetType().Name}\n" +
        $"連線狀態: {MainPlc?.CurrentManager?.IsConnected}\n" +
        $"GlobalStatus: {PlcContext.GlobalStatus != null}",
        "模擬器狀態"
    );
}
```

---

### 測試 2: 手動開啟控制面板

```csharp
private void OpenSimPanel_Click(object sender, RoutedEventArgs e)
{
    if (MainPlc?.CurrentManager?.PlcClient is SmartPlcSimulator sim)
    {
        var simWindow = new SimulatorWindow();
        simWindow.Show();
        MessageBox.Show("? 模擬器面板已開啟");
    }
    else
    {
        MessageBox.Show("? 未找到模擬器");
    }
}
```

---

## ?? 進階診斷

### 如果以上都無法解決，請提供以下資訊：

1. **Output 視窗的完整內容**（從程式啟動開始）
2. **PlcStatus 的顯示狀態**（顏色、文字）
3. **按 F12 是否有任何反應**
4. **是否有錯誤訊息彈出**

---

## ? 成功指標

### 當一切正常時，您會看到：

1. ? 主視窗的 PlcStatus 變成綠燈
2. ? 1-2 秒後自動彈出模擬器控制面板
3. ? 控制面板顯示：
   - 溫度: 25.0°C
   - 軸位置: 0 μm
   - 統計資訊正常更新
4. ? 點擊「Start Heater」可以看到溫度上升
5. ? Output 視窗有完整的診斷訊息

---

**如果還是有問題，請執行診斷命令並提供 Output 視窗的內容。**
