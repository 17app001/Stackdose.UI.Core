# Recipe Download to PLC 功能說明

## 概述

Recipe Download 功能允許將載入的 Recipe 配方參數值**寫入 PLC**，實現配方的自動下載和應用。

**重要**：Load Recipe 按鈕現在會**自動執行下載**，無需額外操作！

## 問題說明

**之前的行為**：
- ? Recipe 載入成功（從 JSON 檔案讀取）
- ? Recipe 監控啟動（讀取 PLC 數值）
- ? **PLC 數值沒有改變**（只有監控，沒有寫入）

**範例**：
```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "180",    ← Recipe 設定值
  "DataType": "Short"
}
```

PLC D100 = 100（原始值）
Recipe 載入後，PLC D100 **仍然是 100**，不會變成 180！

## 新功能：自動下載

### Load Recipe = 載入 + 下載

**現在的行為**：
```
點擊 "Load Recipe" 按鈕
  ↓
1. 從 JSON 載入 Recipe
  ↓
2. 檢查 PLC 是否連線
  ↓
3. 如果已連線 → 自動下載到 PLC ?
4. 如果未連線 → 等待 PLC 連線後自動下載 ?
```

**範例**：
```
PLC D100 = 100（原始值）
點擊 "Load Recipe"
  ↓
Recipe 載入成功
  ↓
自動下載到 PLC
  ↓
PLC D100 = 180 ?（自動更新！)
```

## 使用方式

### 方式 1：PLC 已連線

```
1. 連線 PLC
   ↓
2. 點擊 "Load Recipe"
   ↓
3. Recipe 載入並自動下載到 PLC ?
   → 彈出訊息："Recipe loaded and downloaded successfully! 10 parameters written to PLC."
```

### 方式 2：PLC 未連線

```
1. 點擊 "Load Recipe"
   ↓
2. Recipe 載入成功
   → 彈出訊息："Recipe loaded successfully. Note: PLC is not connected."
   ↓
3. 連線 PLC
   ↓
4. 自動下載到 PLC ?
   → 日誌："[Recipe] Auto-downloaded to PLC: 10 parameters written"
```

### 方式 3：自動載入

```
1. 應用程式啟動（Recipe 自動載入）
   ↓
2. 連線 PLC
   ↓
3. 自動下載到 PLC ?
   → 日誌："[Recipe] Auto-loaded and downloaded: 10 parameters written to PLC"
```

## RecipeLoader UI

**按鈕配置**：
```
[Load Recipe] [Reload]
```

**不再需要**：
- ? ~~Download to PLC 按鈕~~（已整合到 Load 按鈕）
- ? ~~確認對話框~~（自動執行，無需確認）

## 完整流程

### 流程圖

```
Load Recipe 按鈕
  ↓
載入 Recipe JSON
  ↓
載入成功？
  ├─ No → 顯示錯誤
  └─ Yes
      ↓
      PLC 已連線？
      ├─ No → 顯示 "Recipe loaded (PLC not connected)"
      │       等待 PLC 連線後自動下載
      └─ Yes
          ↓
          自動下載 Recipe 到 PLC
          ↓
          下載成功？
          ├─ Yes → 顯示 "Recipe loaded and downloaded successfully!"
          └─ No → 顯示 "Recipe loaded but download failed"
```

## RecipeContext API

### DownloadRecipeToPLCAsync()

雖然 UI 不再需要手動調用，但 API 仍然可用於程式控制：

```csharp
// 手動下載 Recipe 到 PLC
int count = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
Console.WriteLine($"Downloaded {count} parameters");
```

## 日誌輸出

### 成功載入並下載（PLC 已連線）

```
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 180 °C
[Recipe Download] Cooling Water Pressure (D102) = 3.5 bar
[Recipe Download] Conveyor Speed (D104) = 120 mm/s
...
[Recipe] Download completed successfully: 10/10 parameters written
```

### 載入成功但 PLC 未連線

```
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
```

### PLC 連線時自動下載

```
[PLC] Connection established, checking Recipe status...
[Recipe] Auto-downloaded to PLC: 10 parameters written
```

## DataType 處理

### Short / Word (16-bit)

**單一暫存器**

```csharp
// Recipe.json
{
  "Address": "D100",
  "Value": "180",
  "DataType": "Short"
}

// PLC 寫入
await plcClient.WriteWordAsync("D", 100, 180);
// D100 = 180
```

### DWord / Int / Int32 (32-bit)

**兩個連續暫存器**

```csharp
// Recipe.json
{
  "Address": "D102",
  "Value": "1000",
  "DataType": "DWord"
}

// PLC 寫入
await plcClient.WriteDWordAsync("D", 102, 1000);
// D102-D103 = 1000 (32-bit)
```

### Float (32-bit 浮點數)

**兩個連續暫存器（定點數表示）**

```csharp
// Recipe.json
{
  "Address": "D104",
  "Value": "3.5",
  "DataType": "Float"
}

// PLC 寫入（轉換為定點數）
float value = 3.5f;
int intValue = (int)(value * 10); // 3.5 -> 35
await plcClient.WriteDWordAsync("D", 104, intValue);
// D104-D105 = 35 (需要 PLC 端除以 10)
```

## FDA 21 CFR Part 11 審計追蹤

每次下載 Recipe 都會記錄：

```sql
Action: Recipe Download
Target: RECIPE-001
OldValue: N/A
NewValue: Standard Process Recipe A v1.2.0
Reason: Downloaded by Engineer_001 - 10 success, 0 failed
Timestamp: 2024-01-15 14:30:00
```

## 安全性與權限

### 權限控制

RecipeLoader 的 Load 按鈕使用 `SecuredButton`：

```xml
<Controls:SecuredButton 
    Content="Load Recipe"
    Theme="Success"
    RequiredLevel="Instructor"  ← 需要 Instructor 權限
    Click="LoadButton_Click"/>
```

### 自動下載

- ? 自動執行，無需確認對話框
- ? 只在 PLC 連線時執行
- ? 失敗時顯示警告訊息
- ? 記錄審計追蹤

## 錯誤處理

### 1. Recipe 載入失敗

```
Recipe 載入失敗
[錯誤訊息]

[OK]
```

### 2. 下載失敗（部分參數）

```
Recipe loaded but download to PLC failed. Check logs for details.

[OK]
```

狀態顯示：`Recipe 載入成功，但下載失敗`（橙色）

### 3. PLC 未連線

```
Recipe loaded successfully.

Note: PLC is not connected. Recipe will be downloaded when PLC connects.

[OK]
```

## 常見問題

### Q: Recipe 載入後 PLC 數值會立即更新嗎？

**A**: **會！**如果 PLC 已連線，Load Recipe 會自動下載到 PLC。

### Q: 如果 PLC 未連線呢？

**A**: Recipe 會先載入，等待 PLC 連線後**自動下載**。

### Q: 需要手動點擊 Download 按鈕嗎？

**A**: **不需要！**Load Recipe 已經包含自動下載功能。

### Q: 可以關閉自動下載嗎？

**A**: 目前不行。自動下載是設計的核心功能，確保 Recipe 載入後立即應用。

### Q: Reload 按鈕也會自動下載嗎？

**A**: **會！**Reload 按鈕會重新載入 Recipe 並自動下載到 PLC。

### Q: 如何確認下載成功？

**A**: 
1. 查看彈出訊息：`Recipe loaded and downloaded successfully! N parameters written to PLC.`
2. 查看狀態列：`Recipe 載入並下載成功: N 個參數`（綠色）
3. 查看日誌：`[Recipe] Download completed successfully: N/N parameters written`
4. 查看 PlcLabel 顯示的數值是否已更新

## 與之前版本的差異

| 項目 | 之前 | 現在 |
|------|------|------|
| 按鈕數量 | 3 個（Load, Reload, Download） | 2 個（Load, Reload） |
| 操作步驟 | 1. Load → 2. Download | 1. Load（自動下載） |
| 確認對話框 | 需要確認 | 不需要（自動執行） |
| PLC 未連線 | 需要手動 Download | 自動等待並下載 |
| 使用複雜度 | 較複雜（兩步驟） | 簡單（一步驟） |

## 總結

### 設計理念

**Load Recipe = 載入 + 應用**

Recipe 載入後應該**立即生效**，而不是只讀取檔案。

### 優點

- ? **簡化操作**：一個按鈕完成所有事情
- ? **自動化**：無需手動觸發下載
- ? **智能**：根據 PLC 狀態自動處理
- ? **安全**：保留權限控制和審計追蹤
- ? **直觀**：符合使用者預期（載入 = 應用）

### 核心流程

```
Recipe.json → Load → 自動下載 → PLC 更新 ?
```

**一鍵完成，無需多步操作！**
