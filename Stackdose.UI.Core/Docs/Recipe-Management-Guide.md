# Recipe Management System - FDA Compliant

## ?? Overview

Recipe Management System 是一個符合 FDA 21 CFR Part 11 規範的製程配方管理系統,整合 PLC 監控和審計追蹤功能。

---

## ?? 核心功能

### 1. **自動載入機制**
- ? **等待 PLC 連線** - Recipe 只有在 PLC 第一次連線成功後才自動載入
- ? **自動註冊到 Monitor** - 載入成功後自動將所有參數註冊到 PLC Monitor
- ? **FDA 審計追蹤** - 所有載入操作都記錄到審計日誌

### 2. **手動載入功能**
- ? **權限控制** - 使用 `SecuredButton` 控制操作權限 (預設 Instructor 以上)
- ? **PLC 連線檢查** - 載入前檢查 PLC 是否連線
- ? **重新載入** - 支援重新載入當前 Recipe

### 3. **資料類型支援**

| 資料類型 | Monitor 長度 | 說明 |
|---------|-------------|------|
| **Short** | 1 Device | 16-bit 有符號整數 (單一 Word) |
| **Int** | 2 Devices | 32-bit 有符號整數 (連續兩個 Word) |
| **Float** | 2 Devices | 32-bit 浮點數 (連續兩個 Word) |
| **Bool** | 1 Device | 布林值 (單一 Bit) |

---

## ?? 使用方式

### XAML 配置

```xml
<Controls:RecipeLoader 
    Title="Recipe Management"
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="True"
    AutoRegisterMonitor="True"
    RequirePlcConnection="True"
    RequiredAccessLevel="Instructor"
    ShowDetails="True"/>
```

### 屬性說明

| 屬性 | 類型 | 預設值 | 說明 |
|-----|------|--------|------|
| `Title` | string | "Recipe Management" | 控制項標題 |
| `RecipeFilePath` | string | "Recipe.json" | Recipe 檔案路徑 |
| `AutoLoadOnStartup` | bool | True | 是否在 PLC 連線後自動載入 |
| `AutoRegisterMonitor` | bool | True | 是否自動註冊參數到 Monitor |
| `RequirePlcConnection` | bool | True | 是否強制要求 PLC 連線 |
| `RequiredAccessLevel` | AccessLevel | Instructor | 手動載入所需權限等級 |
| `ShowDetails` | bool | True | 是否顯示詳細資訊 |

---

## ?? Recipe.json 格式

```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "Standard Process Recipe A",
  "Version": "1.2.0",
  "CreatedBy": "Engineer_001",
  "ProductCode": "XYZ-100",
  "Status": "Active",
  "Items": [
    {
      "Name": "Heater Temperature",
      "Address": "D100",
      "Value": "180",
      "DataType": "Short",
      "Unit": "°C",
      "MinValue": 100,
      "MaxValue": 250,
      "IsEnabled": true
    },
    {
      "Name": "Total Production Count",
      "Address": "D300",
      "Value": "12345",
      "DataType": "Int",
      "Unit": "pcs",
      "IsEnabled": true
    }
  ]
}
```

---

## ?? 運作流程

### 1. 應用程式啟動
```
[App Starts] 
    ↓
[PlcStatus Loaded]
    ↓
[RecipeLoader Loaded]
    ↓
[Wait for PLC Connection...]
```

### 2. PLC 連線成功
```
[PLC Connecting...] 
    ↓
[PLC Connected ?] → Trigger ConnectionEstablished Event
    ↓
[RecipeLoader receives event]
    ↓
[Check: AutoLoadOnStartup = True?]
    ↓ Yes
[Load Recipe.json]
    ↓
[Validate Recipe data]
    ↓
[Register 14 parameters to Monitor]
    ↓
[Recipe Loaded Successfully ?]
```

### 3. FDA 審計追蹤記錄
```
? PLC Connection Established (192.168.22.39)
? Recipe Load: RECIPE-001 v1.2.0 (Auto-Load by System)
? Recipe Monitor Registration: 14 parameters registered
? [Recipe] Registered Heater Temperature (D100) to Monitor (length: 1)
? [Recipe] Registered Total Production Count (D300) to Monitor (length: 2)
? [Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (14 parameters)
```

---

## ?? FDA 21 CFR Part 11 合規性

### 審計追蹤 (Audit Trail)
所有 Recipe 操作都會記錄:
- ? 使用者身份 (User ID)
- ? 時間戳記 (Timestamp)
- ? 操作類型 (Load/Reload/Switch)
- ? 操作結果 (Success/Failed)
- ? 變更內容 (Recipe ID, Version, Parameters)
- ? 失敗原因 (PLC not connected, File not found, etc.)

### 電子簽章 (Electronic Signature)
- ? 手動載入需要對應權限等級
- ? 整合 `SecurityContext` 使用者管理
- ? 操作前驗證使用者權限

### 資料完整性 (Data Integrity)
- ? Recipe 資料驗證 (Validate 方法)
- ? 參數範圍檢查 (MinValue/MaxValue)
- ? 版本控制 (Version, CreatedBy, LastModifiedBy)

---

## ?? 程式碼範例

### 手動載入 Recipe

```csharp
// 載入指定的 Recipe
bool success = await RecipeContext.LoadRecipeAsync("Recipe.json");

if (success)
{
    Console.WriteLine($"Loaded: {RecipeContext.CurrentRecipe.RecipeName}");
}
```

### 取得參數值

```csharp
// 依名稱取得參數值
string temp = RecipeContext.GetParameterValue("Heater Temperature");

// 依位址取得參數值
string value = RecipeContext.GetParameterValueByAddress("D100");
```

### 切換 Recipe

```csharp
// 切換到另一個 Recipe
bool switched = RecipeContext.SwitchRecipe("RECIPE-002");
```

---

## ?? UI 狀態顯示

### 載入前 (PLC 未連線)
```
┌─────────────────────────┐
│   Recipe Management     │
├─────────────────────────┤
│ ??  PLC not connected   │
│                         │
│   ?? No Recipe loaded   │
│                         │
│ [Load Recipe] [Reload]  │
└─────────────────────────┘
```

### 載入成功
```
┌─────────────────────────┐
│   Recipe Management     │
├─────────────────────────┤
│ ? Successfully loaded   │
│   Recipe: Std Process A │
│   Version: v1.2.0       │
│   ID: RECIPE-001        │
│   Parameters: 14 items  │
│   Last Load: 2025-12-22 │
│                         │
│ [Load Recipe] [Reload]  │
└─────────────────────────┘
```

---

## ?? 進階配置

### 禁用 PLC 連線檢查 (測試環境)

```xml
<Controls:RecipeLoader 
    RequirePlcConnection="False"/>
```

### 禁用自動載入

```xml
<Controls:RecipeLoader 
    AutoLoadOnStartup="False"/>
```

### 程式碼控制

```csharp
// 動態控制自動載入
RecipeContext.AutoRegisterToMonitor = false;
RecipeContext.RequirePlcConnection = false;
```

---

## ?? 注意事項

1. **PLC 連線順序** - Recipe 必須在 PLC 連線後才載入
2. **Monitor 註冊** - Int (32-bit) 類型會自動註冊 2 個連續 Device
3. **權限管理** - 手動載入需要 Instructor 以上權限
4. **審計追蹤** - 所有操作都會寫入 SQLite 資料庫

---

## ?? 除錯資訊

### 檢查 Recipe 狀態

```csharp
// 是否有活動的 Recipe
bool hasRecipe = RecipeContext.HasActiveRecipe;

// 取得摘要資訊
string summary = RecipeContext.GetSummary();

// 最後載入訊息
string message = RecipeContext.LastLoadMessage;

// 最後載入時間
DateTime? lastLoad = RecipeContext.LastLoadTime;
```

### 檢查 PLC 連線

```csharp
var plcManager = PlcContext.GlobalStatus?.CurrentManager;
bool isConnected = plcManager?.IsConnected ?? false;
```

---

## ?? 相關文件

- `RecipeContext.cs` - Recipe 管理引擎
- `RecipeLoader.xaml.cs` - UI 控制項
- `Recipe.cs` / `RecipeItem.cs` - 資料模型
- `ComplianceContext.cs` - FDA 審計追蹤

---

**版本**: 1.0.0  
**最後更新**: 2024-12-22  
**作者**: Stackdose Development Team
