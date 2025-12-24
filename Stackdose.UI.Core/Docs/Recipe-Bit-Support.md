# Recipe Bit Support - 位元寫入指南

## 概述

Recipe 系統現在支援**Bit 位元**的寫入，您可以設定 PLC 的 Bit 位址（例如 R2002 的第 5 個 bit 或 M100）。

## 支援的 Bit 類型

### 1. Word Bit（字位元）

**格式**：`裝置類型 + 地址 + . + Bit位置`

**範例**：
- `R2002.5` - R2002 的第 5 個 bit
- `D100.0` - D100 的第 0 個 bit
- `D100.15` - D100 的第 15 個 bit

**有效 Bit 位置**：0 ~ 15（16 個 bit）

### 2. Pure Bit（純位元裝置）

**格式**：`裝置類型 + 地址`

**範例**：
- `M100` - M（輔助繼電器）100
- `X10` - X（輸入）10
- `Y20` - Y（輸出）20

## Recipe.json 範例

### 完整範例（包含 Bit 類型）

```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "Recipe 1: Standard Process",
  "Version": "1.2.0",
  "Items": [
    {
      "Name": "Heater Temperature",
      "Address": "D100",
      "Value": "200000",
      "DataType": "Int",
      "Unit": "°C",
      "Description": "Main heater target temperature",
      "IsEnabled": true
    },
    {
      "Name": "Motor Enable",
      "Address": "R2002.0",
      "Value": "1",
      "DataType": "Bit",
      "Unit": "",
      "Description": "Motor enable signal (R2002 bit 0)",
      "IsEnabled": true
    },
    {
      "Name": "Heater Enable",
      "Address": "R2002.1",
      "Value": "1",
      "DataType": "Bit",
      "Unit": "",
      "Description": "Heater enable signal (R2002 bit 1)",
      "IsEnabled": true
    },
    {
      "Name": "Alarm Reset",
      "Address": "R2002.5",
      "Value": "0",
      "DataType": "Bit",
      "Unit": "",
      "Description": "Alarm reset signal (R2002 bit 5)",
      "IsEnabled": true
    },
    {
      "Name": "Auto Mode",
      "Address": "M100",
      "Value": "1",
      "DataType": "Bit",
      "Unit": "",
      "Description": "Auto mode enable (M100)",
      "IsEnabled": true
    },
    {
      "Name": "Manual Mode",
      "Address": "M101",
      "Value": "0",
      "DataType": "Bit",
      "Unit": "",
      "Description": "Manual mode enable (M101)",
      "IsEnabled": true
    }
  ]
}
```

## DataType 定義

### 支援的 DataType

| DataType | 說明 | 範例地址 | 值範圍 | PLC 佔用 |
|----------|------|---------|--------|---------|
| **Bit** | 位元 | R2002.5, M100 | 0 或 1 | 1 bit |
| Short | 16-bit 整數 | D100 | -32768 ~ 32767 | 1 個暫存器 |
| Word | 16-bit 無符號 | D100 | 0 ~ 65535 | 1 個暫存器 |
| Int | 32-bit 整數 | D100 | -2147483648 ~ 2147483647 | 2 個暫存器 |
| DWord | 32-bit 無符號 | D100 | 0 ~ 4294967295 | 2 個暫存器 |
| Float | 32-bit 浮點數 | D100 | 浮點數 | 2 個暫存器 |

## Bit 位址格式

### Word Bit 格式

```
裝置類型 + 地址 + . + Bit位置

範例：
  R2002.5  → R2002 的第 5 個 bit
  D100.0   → D100 的第 0 個 bit
  D100.15  → D100 的第 15 個 bit
```

**支援的裝置類型**：
- `D` - 資料暫存器（Data Register）
- `R` - 檔案暫存器（File Register）
- `W` - 連結暫存器（Link Register）
- `M` - 輔助繼電器（Auxiliary Relay）

**Bit 位置範圍**：
- 0 ~ 15（共 16 個 bit）

### Pure Bit 格式

```
裝置類型 + 地址

範例：
  M100  → M（輔助繼電器）100
  X10   → X（輸入）10
  Y20   → Y（輸出）20
```

**支援的裝置類型**：
- `M` - 輔助繼電器（Auxiliary Relay）
- `X` - 輸入（Input）
- `Y` - 輸出（Output）

## 寫入邏輯

### Word Bit 寫入

```csharp
// Recipe Item:
{
  "Address": "R2002.5",
  "Value": "1",
  "DataType": "Bit"
}

// 寫入邏輯:
await plcClient.WriteBitAsync("R", 2002, 5, 1);
```

**流程**：
1. 解析地址：`R2002.5` → device="R", address=2002, bitPos=5
2. 驗證 bitPos：0 ? bitPos ? 15
3. 驗證 value：0 或 1
4. 寫入 PLC：`WriteBitAsync("R", 2002, 5, 1)`

### Pure Bit 寫入

```csharp
// Recipe Item:
{
  "Address": "M100",
  "Value": "1",
  "DataType": "Bit"
}

// 寫入邏輯:
await plcClient.WriteBitAsync("M", 100, 1);
```

**流程**：
1. 解析地址：`M100` → device="M", address=100
2. 驗證 value：0 或 1
3. 寫入 PLC：`WriteBitAsync("M", 100, 1)`

## Bit 在 PLC 中的表示

### R2002 的 Bit 分佈

```
R2002 (16-bit Word)
┌─────────────────────────────────────────────────────────┐
│ Bit 15 │ ... │ Bit 5 │ Bit 4 │ ... │ Bit 1 │ Bit 0 │
└─────────────────────────────────────────────────────────┘

範例：
  R2002.0  → 最低位（LSB）
  R2002.5  → 第 5 個 bit
  R2002.15 → 最高位（MSB）
```

### 實際範例

假設 R2002 = 0b0000000000100011（十進位 35）

```
Bit 位置:  15 14 13 12 11 10  9  8  7  6  5  4  3  2  1  0
Bit 值:     0  0  0  0  0  0  0  0  0  0  1  0  0  0  1  1
                                         ↑           ↑  ↑
                                       Bit 5       Bit 1,0

R2002.0 = 1 ?
R2002.1 = 1 ?
R2002.2 = 0
R2002.3 = 0
R2002.4 = 0
R2002.5 = 1 ?
```

## 監控地址生成

### GenerateMonitorAddresses() 行為

Recipe 中的 Bit 類型會自動生成監控地址：

```json
{
  "Items": [
    { "Address": "R2002.0", "DataType": "Bit" },
    { "Address": "R2002.1", "DataType": "Bit" },
    { "Address": "R2002.5", "DataType": "Bit" },
    { "Address": "M100", "DataType": "Bit" },
    { "Address": "D100", "DataType": "Int" }
  ]
}
```

**生成的監控地址**：
```
R2002:1,M100:1,D100:2
```

**說明**：
- `R2002:1` - 監控 R2002（涵蓋所有 Bit：0, 1, 5）
- `M100:1` - 監控 M100
- `D100:2` - 監控 D100 和 D101（Int 類型）

**優化**：
- 多個 Word Bit（如 R2002.0, R2002.1, R2002.5）會**合併**為一個 R2002:1
- 避免重複註冊同一個 Word

## 下載日誌

### Word Bit 下載

```
[Recipe] Downloading Recipe to PLC: Recipe 1: Standard Process v1.2.0
[Recipe Download] Motor Enable (R2002.0) = 1
[Recipe Download] Heater Enable (R2002.1) = 1
[Recipe Download] Alarm Reset (R2002.5) = 0
```

### Pure Bit 下載

```
[Recipe Download] Auto Mode (M100) = 1
[Recipe Download] Manual Mode (M101) = 0
```

## 常見使用場景

### 場景 1：控制訊號

```json
{
  "Name": "Motor Start",
  "Address": "M200",
  "Value": "1",
  "DataType": "Bit",
  "Description": "Start motor"
}
```

### 場景 2：模式選擇

```json
{
  "Name": "Auto Mode",
  "Address": "M100",
  "Value": "1",
  "DataType": "Bit",
  "Description": "Enable auto mode"
},
{
  "Name": "Manual Mode",
  "Address": "M101",
  "Value": "0",
  "DataType": "Bit",
  "Description": "Disable manual mode"
}
```

### 場景 3：設備啟用

```json
{
  "Name": "Heater Enable",
  "Address": "R2002.0",
  "Value": "1",
  "DataType": "Bit",
  "Description": "Enable heater"
},
{
  "Name": "Cooler Enable",
  "Address": "R2002.1",
  "Value": "1",
  "DataType": "Bit",
  "Description": "Enable cooler"
}
```

### 場景 4：警報重置

```json
{
  "Name": "Alarm Reset",
  "Address": "R2002.5",
  "Value": "0",
  "DataType": "Bit",
  "Description": "Reset alarm (normally 0)"
}
```

### 場景 5：設備狀態組合

```json
{
  "Name": "System Ready",
  "Address": "D100.0",
  "Value": "1",
  "DataType": "Bit",
  "Description": "System ready status"
},
{
  "Name": "Error Status",
  "Address": "D100.1",
  "Value": "0",
  "DataType": "Bit",
  "Description": "Error status (0=no error)"
},
{
  "Name": "Running Status",
  "Address": "D100.2",
  "Value": "1",
  "DataType": "Bit",
  "Description": "Running status"
}
```

## 錯誤處理

### 錯誤 1：無效的 Bit 位置

```json
{
  "Address": "R2002.20",  // ? Bit 位置超出範圍
  "DataType": "Bit"
}
```

**錯誤訊息**：
```
[Recipe Download Error] Motor Enable: Invalid bit position 20 (must be 0-15)
```

### 錯誤 2：無效的 Bit 值

```json
{
  "Address": "M100",
  "Value": "2",  // ? 值必須是 0 或 1
  "DataType": "Bit"
}
```

**錯誤訊息**：
```
[Recipe Download Error] Auto Mode: Invalid Bit value '2' (must be 0 or 1)
```

### 錯誤 3：地址格式錯誤

```json
{
  "Address": "R2002.A",  // ? Bit 位置必須是數字
  "DataType": "Bit"
}
```

**錯誤訊息**：
```
[Recipe Download Error] Motor Enable: Invalid address format R2002.A
```

## 驗證方法

### 方法 1：使用 PlcDeviceEditor

1. 勾選 **DWord** CheckBox（如果是 Word Bit）
2. 輸入地址：`R2002`
3. 點擊 **Read** 按鈕
4. 查看數值並檢查對應的 Bit

**範例**：
```
R2002 = 35 (二進位: 0000000000100011)
  → Bit 0 = 1
  → Bit 1 = 1
  → Bit 5 = 1
```

### 方法 2：使用 GX Works

1. 開啟 GX Works
2. 監控 R2002
3. 查看 Bit 狀態

### 方法 3：檢查日誌

```
[Recipe Download] Motor Enable (R2002.0) = 1
[Recipe Download] Heater Enable (R2002.1) = 1
[Recipe Download] Alarm Reset (R2002.5) = 0
```

## DataType 比較表

| DataType | 地址範例 | 值範例 | PLC 寫入 | 監控地址 |
|----------|---------|-------|---------|---------|
| **Bit** | `R2002.5` | `1` | `WriteBitAsync("R", 2002, 5, 1)` | `R2002:1` |
| **Bit** | `M100` | `1` | `WriteBitAsync("M", 100, 1)` | `M100:1` |
| Short | `D100` | `180` | `WriteWordAsync("D", 100, 180)` | `D100:1` |
| Int | `D100` | `200000` | `WriteDWordAsync("D", 100, 200000)` | `D100:2` |

## 最佳實踐

### 1. 使用有意義的名稱

? **好的命名**：
```json
{
  "Name": "Motor Enable",
  "Address": "R2002.0",
  "Description": "Enable motor operation"
}
```

? **不好的命名**：
```json
{
  "Name": "Bit 0",
  "Address": "R2002.0",
  "Description": "Some bit"
}
```

### 2. 組織相關的 Bit

將相關的 Bit 放在同一個 Word 中：

```json
{
  "Name": "Motor Enable",
  "Address": "R2002.0"
},
{
  "Name": "Heater Enable",
  "Address": "R2002.1"
},
{
  "Name": "Cooler Enable",
  "Address": "R2002.2"
}
```

### 3. 明確的描述

```json
{
  "Name": "Alarm Reset",
  "Address": "R2002.5",
  "Value": "0",
  "Description": "Reset alarm signal (normally 0, set to 1 to reset)"
}
```

### 4. 預設值設定

```json
{
  "Name": "Emergency Stop",
  "Address": "M500",
  "Value": "0",
  "Description": "Emergency stop signal (0=normal, 1=stop)"
}
```

## 總結

### 新增功能

- ? 支援 Word Bit（例如：R2002.5）
- ? 支援 Pure Bit（例如：M100）
- ? 自動合併監控地址
- ? 完整的錯誤處理
- ? 審計追蹤記錄

### DataType 支援列表

| DataType | 狀態 |
|----------|------|
| Bit | ? 支援 |
| Short | ? 支援 |
| Word | ? 支援 |
| Int | ? 支援 |
| DWord | ? 支援 |
| Int32 | ? 支援 |
| Float | ? 支援 |

### Recipe.json 範例

```json
{
  "Items": [
    { "Address": "D100", "DataType": "Int", "Value": "200000" },
    { "Address": "R2002.0", "DataType": "Bit", "Value": "1" },
    { "Address": "R2002.5", "DataType": "Bit", "Value": "0" },
    { "Address": "M100", "DataType": "Bit", "Value": "1" },
    { "Address": "D104", "DataType": "Short", "Value": "120" }
  ]
}
```

**現在您可以在 Recipe 中控制 PLC 的任何 Bit 位址了！** ??
