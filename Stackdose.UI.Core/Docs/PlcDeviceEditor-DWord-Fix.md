# PlcDeviceEditor DWord 讀取失敗修復

## 問題描述

使用 `PlcDeviceEditor` 讀取 DWord 類型的地址（例如 D100）時，會失敗並顯示**紅色框**。

### 錯誤現象

```
PlcDeviceEditor:
  Address: D100
  DWord (32-bit): [?] 勾選
  
點擊 Read 按鈕：
  ? 顯示紅色框
  ? 讀取失敗
```

### 錯誤原因

1. **PlcDeviceEditor 使用 `manager.ReadDWord(addr)`**
   ```csharp
   var dwordValue = manager.ReadDWord(addr);
   ```

2. **ReadDWord 依賴 PlcMonitorService 的快取資料**
   ```csharp
   public int? ReadDWord(string device)
   {
       return _monitor?.GetDWord(device);
   }
   ```

3. **GetDWord 需要讀取兩個連續的暫存器**
   ```csharp
   public int? GetDWord(string device)
   {
       var low = GetWord($"{dev}{baseAddr}");      // D100
       var high = GetWord($"{dev}{baseAddr + 1}");  // D101
       if (low == null || high == null) return null; // ? 如果任一個未註冊，返回 null
   }
   ```

4. **PlcDeviceEditor 沒有自動註冊監控地址**
   - Recipe 會自動註冊（`D100:2` 表示 D100 和 D101）?
   - PlcLabel 會自動註冊 ?
   - **PlcDeviceEditor 沒有自動註冊** ?

5. **結果**：
   - 如果 D100 和 D101 沒有被其他控制項註冊
   - `GetWord("D100")` 返回 null
   - `GetWord("D101")` 返回 null
   - `GetDWord("D100")` 返回 null ?
   - PlcDeviceEditor 顯示錯誤（紅色框）

## 解決方案

### 自動註冊監控地址

在 `PlcDeviceEditor` 載入時，根據 **DWord 模式**自動註冊監控地址。

#### 1. 控制項載入時註冊

```csharp
public PlcDeviceEditor()
{
    InitializeComponent();
    
    // ...existing code...
    
    // ? 控制項載入時，自動註冊監控地址
    this.Loaded += OnControlLoaded;
}

private void OnControlLoaded(object sender, RoutedEventArgs e)
{
    // 如果有設定 Address，自動註冊到監控服務
    if (!string.IsNullOrWhiteSpace(Address))
    {
        RegisterMonitorAddress();
    }
}
```

#### 2. 根據 DWord 模式註冊

```csharp
private void RegisterMonitorAddress()
{
    var status = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
    var manager = status?.CurrentManager;

    if (manager?.Monitor == null) return;

    string addr = Address?.Trim().ToUpper() ?? "";
    if (string.IsNullOrEmpty(addr)) return;

    // 解析地址
    var match = Regex.Match(addr, @"^([DR])(\d+)$");
    if (!match.Success) return; // 只支援 D/R 裝置

    // DWord 模式需要註冊兩個連續暫存器
    int length = IsDWordMode ? 2 : 1;

    manager.Monitor.Register(addr, length);

    #if DEBUG
    System.Diagnostics.Debug.WriteLine($"[PlcDeviceEditor] Auto-registered: {addr}:{length}");
    #endif
}
```

#### 3. DWord CheckBox 變更時重新註冊

```csharp
private void ChkDWord_Checked(object sender, RoutedEventArgs e)
{
    #if DEBUG
    System.Diagnostics.Debug.WriteLine("[PlcDeviceEditor] DataType: DWord (32-bit)");
    #endif
    
    // ? 重新註冊監控地址（DWord 需要 2 個暫存器）
    RegisterMonitorAddress();
}

private void ChkDWord_Unchecked(object sender, RoutedEventArgs e)
{
    #if DEBUG
    System.Diagnostics.Debug.WriteLine("[PlcDeviceEditor] DataType: Word (16-bit)");
    #endif
    
    // ? 重新註冊監控地址（Word 只需要 1 個暫存器）
    RegisterMonitorAddress();
}
```

## 修復後的流程

### 1. 控制項載入

```xaml
<Controls:PlcDeviceEditor 
    Label="PLC Input"
    Address="D100"
    Value="0"
    RequiredLevel="Supervisor"
    EnableAuditTrail="True"/>
```

```
1. PlcDeviceEditor 載入
   ↓
2. OnControlLoaded 觸發
   ↓
3. RegisterMonitorAddress()
   ↓
4. 檢查 DWord 模式
   - 未勾選 → 註冊 D100:1
   - 已勾選 → 註冊 D100:2 ?
   ↓
5. PlcMonitorService 開始監控 D100 和 D101 ?
```

### 2. 勾選 DWord CheckBox

```
1. 使用者勾選 DWord CheckBox
   ↓
2. ChkDWord_Checked 觸發
   ↓
3. RegisterMonitorAddress()
   ↓
4. 註冊 D100:2（覆蓋之前的 D100:1）
   ↓
5. PlcMonitorService 開始監控 D100 和 D101 ?
```

### 3. 點擊 Read 按鈕

```
1. 使用者點擊 Read 按鈕
   ↓
2. BtnRead_Click()
   ↓
3. manager.ReadDWord("D100")
   ↓
4. _monitor?.GetDWord("D100")
   ↓
5. GetWord("D100") → 從快取返回 D100 的值 ?
6. GetWord("D101") → 從快取返回 D101 的值 ?
   ↓
7. 組合成 32-bit DWord
   ↓
8. 顯示結果 ?
```

## 日誌輸出

### Debug 日誌

```
[PlcDeviceEditor] Auto-registered: D100:2
[PlcDeviceEditor] DataType: DWord (32-bit)
[PlcDeviceEditor] DWord Read: D100 = 200000
```

### PlcMonitorService 日誌

```
[PlcMonitor] Registered: D100 Length=2
[PlcMonitor] Monitoring: D100, D101
[PlcMonitor] D100 = 3392 (0x0D40)
[PlcMonitor] D101 = 3 (0x0003)
```

## 驗證方法

### 測試案例 1：Word 模式

```xaml
<Controls:PlcDeviceEditor 
    Address="D100"
    Value="0"/>
```

**操作**：
1. 不勾選 DWord CheckBox
2. 點擊 Read 按鈕

**預期結果**：
- 註冊：D100:1
- 讀取：D100 的 Word 值（16-bit）?

### 測試案例 2：DWord 模式

```xaml
<Controls:PlcDeviceEditor 
    Address="D100"
    Value="0"/>
```

**操作**：
1. 勾選 DWord CheckBox
2. 點擊 Read 按鈕

**預期結果**：
- 註冊：D100:2
- 讀取：D100-D101 的 DWord 值（32-bit）?
- 如果 PLC D100=3392, D101=3 → 顯示 200000 ?

### 測試案例 3：切換模式

**操作**：
1. 不勾選 DWord（Word 模式）
2. 勾選 DWord（DWord 模式）
3. 點擊 Read 按鈕

**預期結果**：
- 第一次註冊：D100:1
- 第二次註冊：D100:2（覆蓋）
- 讀取：DWord 值 ?

## Recipe 中的 D100

如果您的 Recipe.json 有：

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int"
}
```

**流程**：

```
1. Load Recipe
   ↓
2. RecipeContext.GenerateMonitorAddresses()
   → 返回 "D100:2,D103:1,..."
   ↓
3. PlcStatus 自動註冊
   → D100:2 (D100 和 D101)
   ↓
4. PlcDeviceEditor 載入
   → 檢測到 D100 已註冊（或重新註冊）
   ↓
5. 勾選 DWord CheckBox
   → 確保註冊 D100:2
   ↓
6. 點擊 Read 按鈕
   → 成功讀取 200000 ?
```

## 與其他控制項的對比

| 控制項 | 自動註冊 | 支援 DWord | 說明 |
|--------|---------|-----------|------|
| **PlcLabel** | ? | ? | 根據 DataType 自動註冊 |
| **SensorViewer** | ? | ? | 根據 Sensors.json 自動註冊 |
| **Recipe** | ? | ? | 根據 Recipe.json 自動註冊 |
| **PlcDeviceEditor** | **? (修復後)** | ? | 根據 DWord CheckBox 自動註冊 |

## 重要說明

### 1. 為什麼需要自動註冊？

`PlcDeviceEditor` 使用 `ReadDWord` 方法，這個方法依賴 `PlcMonitorService` 的快取資料。如果地址沒有被註冊，快取中就沒有資料，讀取會失敗。

### 2. 為什麼不直接從 PLC 讀取？

因為 `ReadDWord` 的設計是為了效能和即時性：
- ? 快取：從記憶體讀取，速度快
- ? 即時：Monitor 持續更新，資料最新
- ? 直接讀取：每次都要發送 PLC 命令，速度慢

### 3. 註冊會重複嗎？

不會！`PlcMonitorService.Register` 會自動合併重複的地址範圍。

### 4. 如果 Recipe 已經註冊了 D100，PlcDeviceEditor 還需要註冊嗎？

理論上不需要，但為了保險起見，PlcDeviceEditor 仍然會註冊。`PlcMonitorService` 會自動合併，不會造成問題。

## 總結

### 問題

PlcDeviceEditor 讀取 DWord 時失敗，因為 D101 沒有被註冊到監控服務。

### 解決方案

在控制項載入和 DWord CheckBox 變更時，自動註冊監控地址：
- **Word 模式**：註冊 1 個暫存器（D100:1）
- **DWord 模式**：註冊 2 個暫存器（D100:2）

### 修復後

```
PlcDeviceEditor + DWord 勾選 + D100
  ↓
自動註冊 D100:2
  ↓
Monitor 監控 D100 和 D101
  ↓
ReadDWord 成功 ?
  ↓
顯示正確的 32-bit 數值 ?
```

**現在 PlcDeviceEditor 可以正確讀取 DWord 類型的地址了！** ??
