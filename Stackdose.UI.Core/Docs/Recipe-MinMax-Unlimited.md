# Recipe MinValue/MaxValue 無限制設定指南

## 概述

Recipe 的 `MinValue` 和 `MaxValue` 是**可選的**（nullable），如果不設定或設定為 `null`，則表示該方向**無限制**。

## 設定方式

### 1. 完全無限制（推薦）

**不設定 MinValue 和 MaxValue**：

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "Unit": "°C",
  "Description": "Main heater target temperature",
  "IsEnabled": true
}
```

**效果**：可以設定任何數值，沒有限制。

### 2. 只限制最小值

```json
{
  "Name": "Pressure",
  "Address": "D102",
  "Value": "100",
  "DataType": "Short",
  "Unit": "bar",
  "Description": "System pressure",
  "MinValue": 0,
  "IsEnabled": true
}
```

**效果**：
- ? 可以設定 0 或更大的值
- ? 不能設定負數
- ? 沒有上限

### 3. 只限制最大值

```json
{
  "Name": "Speed",
  "Address": "D104",
  "Value": "50",
  "DataType": "Short",
  "Unit": "rpm",
  "Description": "Motor speed",
  "MaxValue": 1000,
  "IsEnabled": true
}
```

**效果**：
- ? 沒有下限
- ? 不能超過 1000
- ? 可以設定負數（如果需要的話）

### 4. 雙向限制

```json
{
  "Name": "Temperature",
  "Address": "D106",
  "Value": "25",
  "DataType": "Short",
  "Unit": "°C",
  "Description": "Room temperature",
  "MinValue": -20,
  "MaxValue": 60,
  "IsEnabled": true
}
```

**效果**：
- ? 必須在 -20 到 60 之間
- ? 超出範圍會被拒絕

## Recipe.json 範例

### 完整範例（混合使用）

```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "Standard Process Recipe A",
  "Version": "1.2.0",
  "Items": [
    {
      "Name": "Heater Temperature",
      "Address": "D100",
      "Value": "200000",
      "DataType": "Int",
      "Unit": "°C",
      "Description": "Main heater target temperature (無限制)",
      "IsEnabled": true
    },
    {
      "Name": "Cooling Water Pressure",
      "Address": "D103",
      "Value": "35",
      "DataType": "Word",
      "Unit": "bar",
      "Description": "Cooling system pressure (只限制最小值)",
      "MinValue": 0,
      "IsEnabled": true
    },
    {
      "Name": "Conveyor Speed",
      "Address": "D104",
      "Value": "120",
      "DataType": "Short",
      "Unit": "mm/s",
      "Description": "Main conveyor belt speed (雙向限制)",
      "MinValue": 50,
      "MaxValue": 200,
      "IsEnabled": true
    },
    {
      "Name": "Counter Value",
      "Address": "D110",
      "Value": "99999",
      "DataType": "Int",
      "Unit": "",
      "Description": "Production counter (無限制)",
      "IsEnabled": true
    },
    {
      "Name": "Safety Timeout",
      "Address": "D112",
      "Value": "30",
      "DataType": "Short",
      "Unit": "sec",
      "Description": "Safety timeout (只限制最小值)",
      "MinValue": 10,
      "IsEnabled": true
    }
  ]
}
```

## 驗證行為

### MinValue 和 MaxValue 都不設定

```csharp
// RecipeItem.IsValueInRange()
public bool IsValueInRange(double value)
{
    if (MinValue.HasValue && value < MinValue.Value) return false;  // 跳過
    if (MaxValue.HasValue && value > MaxValue.Value) return false;  // 跳過
    return true;  // ? 永遠通過
}
```

**結果**：任何數值都有效。

### 只設定 MinValue

```json
{
  "MinValue": 100
}
```

```csharp
// value = 50
if (MinValue.HasValue && value < MinValue.Value)  // 100 > 50
    return false;  // ? 不通過

// value = 150
if (MinValue.HasValue && value < MinValue.Value)  // 100 < 150
    // 跳過
return true;  // ? 通過
```

### 只設定 MaxValue

```json
{
  "MaxValue": 200
}
```

```csharp
// value = 250
if (MaxValue.HasValue && value > MaxValue.Value)  // 250 > 200
    return false;  // ? 不通過

// value = 150
if (MaxValue.HasValue && value > MaxValue.Value)  // 150 < 200
    // 跳過
return true;  // ? 通過
```

## 實際應用場景

### 1. 計數器/累加器（無限制）

```json
{
  "Name": "Production Counter",
  "Address": "D1000",
  "Value": "0",
  "DataType": "Int",
  "Unit": "pcs",
  "Description": "Total production count",
  "IsEnabled": true
}
```

**理由**：計數器應該可以無限累加，不應該有上限。

### 2. 溫度設定（只限制最小值）

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "180",
  "DataType": "Short",
  "Unit": "°C",
  "Description": "Heater temperature setpoint",
  "MinValue": 0,
  "IsEnabled": true
}
```

**理由**：溫度不能是負數，但理論上可以很高（由設備限制，而非 Recipe）。

### 3. 馬達速度（雙向限制）

```json
{
  "Name": "Motor Speed",
  "Address": "D104",
  "Value": "1200",
  "DataType": "Short",
  "Unit": "rpm",
  "Description": "Motor rotation speed",
  "MinValue": 0,
  "MaxValue": 3000,
  "IsEnabled": true
}
```

**理由**：馬達有物理限制，速度必須在安全範圍內。

### 4. 偏移值/修正值（無限制）

```json
{
  "Name": "Position Offset",
  "Address": "D200",
  "Value": "-50",
  "DataType": "Short",
  "Unit": "mm",
  "Description": "Position correction offset",
  "IsEnabled": true
}
```

**理由**：偏移值可以是正數或負數，範圍視實際需求而定。

## 您的 Recipe.json 修改建議

### 目前的設定

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "MinValue": 100,    ← 有限制
  "MaxValue": 250,    ← 有限制
  "IsEnabled": true
}
```

**問題**：
- `Value`: 200000
- `MaxValue`: 250
- **200000 > 250** → 驗證失敗！?

### 修改方案 1：移除限制

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "Unit": "°C",
  "Description": "Main heater target temperature",
  "IsEnabled": true
}
```

**效果**：可以設定任何數值 ?

### 修改方案 2：調整限制範圍

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "Unit": "°C",
  "Description": "Main heater target temperature",
  "MinValue": 0,
  "MaxValue": 300000,
  "IsEnabled": true
}
```

**效果**：可以設定 0 到 300000 之間的數值 ?

### 修改方案 3：只限制最小值

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "Unit": "°C",
  "Description": "Main heater target temperature",
  "MinValue": 0,
  "IsEnabled": true
}
```

**效果**：可以設定 0 或更大的數值 ?

## 驗證錯誤訊息

如果數值超出範圍，載入 Recipe 時會顯示：

```
Recipe validation failed: 項目 'Heater Temperature' 值 200000 超出範圍 [100~250]
```

## JSON 格式注意事項

### ? 正確的格式（無限制）

```json
{
  "Name": "Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "IsEnabled": true
}
```

或

```json
{
  "Name": "Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int",
  "MinValue": null,
  "MaxValue": null,
  "IsEnabled": true
}
```

### ? 錯誤的格式

```json
{
  "MinValue": "unlimited",  // ? 應該是數字或不設定
  "MaxValue": "none"        // ? 應該是數字或不設定
}
```

## 總結

| 設定方式 | MinValue | MaxValue | 效果 |
|---------|----------|----------|------|
| **完全無限制** | 不設定 | 不設定 | 任何數值都可以 |
| **只限制最小值** | 設定數值 | 不設定 | >= MinValue |
| **只限制最大值** | 不設定 | 設定數值 | <= MaxValue |
| **雙向限制** | 設定數值 | 設定數值 | MinValue <= value <= MaxValue |

**建議**：
- ? 計數器/累加器：完全無限制
- ? 溫度/壓力：只限制最小值（>= 0）
- ? 馬達速度：雙向限制（安全範圍）
- ? 偏移值/修正值：完全無限制或根據實際需求

**您的 Recipe.json 建議**：
- 將 `Heater Temperature` 的 `MinValue` 和 `MaxValue` 移除
- 或將 `MaxValue` 調整為更大的值（如 300000）
