# Multiple Recipe Support - User Guide

## 概述

Recipe 系統現在支援**多個 Recipe 配方**，您可以在面板上選擇 1、2、3 三個不同的 Recipe 進行載入和下載。

## 功能特點

### 三個 Recipe 配方

| Recipe | 檔案名稱 | 說明 | 預設選擇 |
|--------|---------|------|---------|
| **Recipe 1** | Recipe1.json | Standard Process | ? |
| **Recipe 2** | Recipe2.json | High Temperature Process | ? |
| **Recipe 3** | Recipe3.json | Low Temperature Process | ? |

### Recipe 內容範例

#### Recipe 1: Standard Process
```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "Recipe 1: Standard Process",
  "Items": [
    { "Name": "Heater Temperature", "Address": "D100", "Value": "200000" },
    { "Name": "Cooling Water Pressure", "Address": "D103", "Value": "35" },
    { "Name": "Conveyor Speed", "Address": "D104", "Value": "120" },
    ...
  ]
}
```

#### Recipe 2: High Temperature Process
```json
{
  "RecipeId": "RECIPE-002",
  "RecipeName": "Recipe 2: High Temperature Process",
  "Items": [
    { "Name": "Heater Temperature", "Address": "D100", "Value": "250000" },
    { "Name": "Cooling Water Pressure", "Address": "D103", "Value": "45" },
    { "Name": "Conveyor Speed", "Address": "D104", "Value": "80" },
    ...
  ]
}
```

#### Recipe 3: Low Temperature Process
```json
{
  "RecipeId": "RECIPE-003",
  "RecipeName": "Recipe 3: Low Temperature Process",
  "Items": [
    { "Name": "Heater Temperature", "Address": "D100", "Value": "150000" },
    { "Name": "Cooling Water Pressure", "Address": "D103", "Value": "25" },
    { "Name": "Conveyor Speed", "Address": "D104", "Value": "160" },
    ...
  ]
}
```

## UI 介面

### RecipeLoader 控制項

```
┌─────────────────────────────────────────────┐
│          Recipe 配方管理                     │
├─────────────────────────────────────────────┤
│                                              │
│  Recipe: Recipe 1: Standard Process  v1.2.0 │
│  ID:     ID: RECIPE-001                      │
│  Parameters: 10 items                        │
│                                              │
│  Last Load: 2025-12-22 16:27:05             │
│                                              │
├─────────────────────────────────────────────┤
│                                              │
│  [Recipe 1] [Recipe 2] [Recipe 3]           │
│                                              │
│  [     Load Selected Recipe     ]           │
│                                              │
└─────────────────────────────────────────────┘
```

### 按鈕說明

| 按鈕 | 功能 | 顏色 |
|------|------|------|
| **Recipe 1** | 選擇 Recipe 1 | 藍色（未選中）/ 綠色（選中） |
| **Recipe 2** | 選擇 Recipe 2 | 藍色（未選中）/ 綠色（選中） |
| **Recipe 3** | 選擇 Recipe 3 | 藍色（未選中）/ 綠色（選中） |
| **Load Selected Recipe** | 載入並下載選中的 Recipe | 綠色 |

## 使用流程

### 場景 1：啟動應用程式（自動載入 Recipe 1）

```
1. 啟動 WpfApp1
   ↓
2. Recipe 1 自動載入 ?
   → Recipe: Recipe 1: Standard Process v1.2.0
   → ID: RECIPE-001
   → Parameters: 10 items
   ↓
3. 連線 PLC
   ↓
4. Recipe 1 自動下載到 PLC ?
   → D100 = 200000 (Heater Temperature)
   → D103 = 35 (Cooling Water Pressure)
   ...
```

### 場景 2：切換到 Recipe 2

```
1. 點擊 [Recipe 2] 按鈕
   ↓
2. Recipe 2 按鈕變綠色 ?
   → 狀態："Recipe 2 selected"
   ↓
3. 點擊 [Load Selected Recipe] 按鈕
   ↓
4. Recipe 2 載入並下載到 PLC ?
   → Recipe: Recipe 2: High Temperature Process v1.0.0
   → D100 = 250000 (更高的溫度)
   → D103 = 45 (更高的壓力)
   ...
```

### 場景 3：切換到 Recipe 3

```
1. 點擊 [Recipe 3] 按鈕
   ↓
2. Recipe 3 按鈕變綠色 ?
   ↓
3. 點擊 [Load Selected Recipe] 按鈕
   ↓
4. Recipe 3 載入並下載到 PLC ?
   → Recipe: Recipe 3: Low Temperature Process v1.1.0
   → D100 = 150000 (較低的溫度)
   → D103 = 25 (較低的壓力)
   ...
```

## 日誌輸出

### 選擇 Recipe

```
[User clicks Recipe 2 button]
Recipe 2 selected
```

### 載入 Recipe

```
[User clicks Load Selected Recipe button]
[Recipe] Successfully loaded Recipe: Recipe 2: High Temperature Process v1.0.0 (10 parameters)
[Recipe] Downloading Recipe to PLC: Recipe 2: High Temperature Process v1.0.0
[Recipe Download] Heater Temperature (D100) = 250000 °C
[Recipe Download] Cooling Water Pressure (D103) = 45 bar
[Recipe Download] Conveyor Speed (D104) = 80 mm/s
...
[Recipe] Download completed successfully: 10/10 parameters written
Recipe 2 載入並下載成功: 10 個參數
```

### 彈出訊息

```
┌───────────────────────────────────────┐
│              Success                   │
├───────────────────────────────────────┤
│                                        │
│  Recipe 2 loaded and downloaded        │
│  successfully!                         │
│                                        │
│  10 parameters written to PLC.         │
│                                        │
│              [OK]                      │
└───────────────────────────────────────┘
```

## 移除的功能

### Reload 按鈕（已移除）

**之前**：
```
[Load Recipe] [Reload]
```

**現在**：
```
[Recipe 1] [Recipe 2] [Recipe 3]
[Load Selected Recipe]
```

**原因**：
- ? Reload 按鈕功能重複（與 Load 類似）
- ? 直接選擇不同的 Recipe 更直觀
- ? 簡化操作流程

## 權限控制

### 所有按鈕都需要 Instructor 權限

```xaml
<local:SecuredButton 
    Content="Recipe 1"
    RequiredLevel="Instructor"  ← 需要 Instructor 或更高權限
    Click="Recipe1Button_Click"/>
```

**權限級別**：
- ? Operator（操作員）：無法載入 Recipe
- ? **Instructor**（工程師）：可以載入 Recipe
- ? Supervisor（主管）：可以載入 Recipe
- ? Admin（管理員）：可以載入 Recipe

## Recipe 檔案管理

### 檔案位置

```
WpfApp1/
├── Recipe1.json  ← Recipe 1 (預設)
├── Recipe2.json  ← Recipe 2
├── Recipe3.json  ← Recipe 3
└── ...
```

### 新增 Recipe

如果您需要新增更多 Recipe（例如 Recipe 4、Recipe 5），請：

1. **創建新的 JSON 檔案**：
```
WpfApp1/Recipe4.json
```

2. **更新 RecipeLoader UI**：
```xaml
<local:SecuredButton 
    x:Name="Recipe4Button"
    Content="Recipe 4"
    Click="Recipe4Button_Click"/>
```

3. **添加按鈕事件**：
```csharp
private void Recipe4Button_Click(object sender, RoutedEventArgs e)
{
    _selectedRecipeNumber = 4;
    UpdateRecipeButtonStates();
}
```

### 修改 Recipe

直接編輯 JSON 檔案：
```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "Recipe 1: Modified",
  "Items": [
    {
      "Name": "Heater Temperature",
      "Value": "220000"  ← 修改數值
    }
  ]
}
```

儲存後，點擊對應的 Recipe 按鈕和 Load 按鈕即可載入新的數值。

## 驗證方法

### 1. 檢查 UI 顯示

```
RecipeLoader 應該顯示：
- Recipe Name: Recipe X: XXX Process
- Version: vX.X.X
- ID: RECIPE-00X
- Parameters: 10 items
```

### 2. 檢查 PLC 數值

使用 PlcDeviceEditor 或 GX Works 監控：
```
Recipe 1: D100 = 200000
Recipe 2: D100 = 250000
Recipe 3: D100 = 150000
```

### 3. 檢查日誌

```
[Recipe] Successfully loaded Recipe: Recipe X v1.X.0 (10 parameters)
[Recipe] Download completed successfully: 10/10 parameters written
```

## 常見問題

### Q: 如何知道當前載入的是哪個 Recipe？

**A**: 查看 RecipeLoader 的 Recipe Name：
```
Recipe: Recipe 2: High Temperature Process v1.0.0
```

### Q: 可以在 PLC 連線前選擇 Recipe 嗎？

**A**: 可以！選擇 Recipe 後點擊 Load，Recipe 會先載入到記憶體，等 PLC 連線後再下載。

### Q: 如何快速切換 Recipe？

**A**: 
1. 點擊 Recipe X 按鈕（例如 Recipe 2）
2. 點擊 Load Selected Recipe 按鈕
3. 完成！

### Q: Reload 按鈕去哪了？

**A**: Reload 功能已整合到選擇+載入流程中。如果需要重新載入當前 Recipe，只需再次點擊同一個 Recipe 按鈕和 Load 按鈕即可。

### Q: 可以新增更多 Recipe 嗎？

**A**: 可以！參考「新增 Recipe」章節，創建 Recipe4.json 並添加對應的按鈕。

## Recipe 比較表

| 項目 | Recipe 1 | Recipe 2 | Recipe 3 |
|------|---------|---------|---------|
| **Heater Temperature** | 200000°C | 250000°C | 150000°C |
| **Cooling Water Pressure** | 35 bar | 45 bar | 25 bar |
| **Conveyor Speed** | 120 mm/s | 80 mm/s | 160 mm/s |
| **Mixer RPM** | 450 RPM | 600 RPM | 350 RPM |
| **Pressure Time** | 30 sec | 45 sec | 20 sec |
| **Holding Time** | 120 sec | 180 sec | 80 sec |
| **Cooling Time** | 90 sec | 120 sec | 60 sec |
| **Material A Dosage** | 250 g | 300 g | 200 g |
| **Material B Dosage** | 150 g | 200 g | 100 g |
| **Alarm Critical Temp** | 200°C | 280°C | 180°C |

## 總結

### 主要改進

- ? 支援多個 Recipe（1、2、3）
- ? UI 選擇按鈕（直觀）
- ? 移除 Reload 按鈕（簡化）
- ? 預設載入 Recipe 1
- ? 自動下載到 PLC
- ? 保留權限控制
- ? 審計追蹤記錄

### 使用流程

```
選擇 Recipe → Load → PLC 更新 ?
```

**簡單、直觀、高效！** ??
