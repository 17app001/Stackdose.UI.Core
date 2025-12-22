# Recipe Int/DWord 寫入說明

## 問題

您的 Recipe.json 中有：
```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int"
}
```

**疑問**：Int 類型會寫入連續的 D100、D101 嗎？

## 答案：會！?

### WriteDWordAsync 實現

```csharp
// FX3UPlcClient.cs 行 396-402
public async Task WriteDWordAsync(string device, int address, int value)
{
    // 分離成兩個 word（低位、高位）
    short low = (short)(value & 0xFFFF);
    short high = (short)((value >> 16) & 0xFFFF);
    await WriteWordsAsync(device, address, new short[] { low, high });
}
```

### 寫入過程

**範例：** `WriteDWordAsync("D", 100, 200000)`

#### 步驟 1：分解 32-bit 值

```
value = 200000 (十進位)
      = 0x00030D40 (十六進位)
      = 0000 0000 0000 0011 0000 1101 0100 0000 (二進位)
```

#### 步驟 2：分離為兩個 16-bit Word

```csharp
short low  = (short)(200000 & 0xFFFF);        // 低 16-bit
           = (short)(0x00030D40 & 0xFFFF)
           = (short)0x0D40
           = 3392

short high = (short)((200000 >> 16) & 0xFFFF); // 高 16-bit
           = (short)((0x00030D40 >> 16) & 0xFFFF)
           = (short)0x0003
           = 3
```

#### 步驟 3：寫入 PLC

```csharp
await WriteWordsAsync("D", 100, new short[] { 3392, 3 });
```

**結果**：
- **D100 = 3392** (0x0D40) ← 低 16-bit
- **D101 = 3** (0x0003) ← 高 16-bit

#### 步驟 4：驗證

從 PLC 讀取：
```csharp
int value = await ReadDWordAsync("D", 100);
// D100 = 3392 (low)
// D101 = 3 (high)
// value = (high << 16) | low
//       = (3 << 16) | 3392
//       = 0x00030000 | 0x0D40
//       = 0x00030D40
//       = 200000 ?
```

## DataType 對應

### Recipe 中的 DataType

| DataType | PLC 暫存器數量 | 範例值 | PLC 地址 |
|----------|--------------|-------|---------|
| **Short** | 1 | 180 | D100 |
| **Word** | 1 | 180 | D100 |
| **Int** | **2** | 200000 | **D100, D101** |
| **DWord** | **2** | 200000 | **D100, D101** |
| **Int32** | **2** | 200000 | **D100, D101** |
| **Float** | **2** | 3.5 | **D100, D101** |

### RecipeContext 下載邏輯

```csharp
// RecipeContext.cs DownloadRecipeToPLCAsync()

if (item.DataType?.Equals("Short", StringComparison.OrdinalIgnoreCase) == true ||
    item.DataType?.Equals("Word", StringComparison.OrdinalIgnoreCase) == true)
{
    // Short/Word: 單一暫存器
    await plcManager.PlcClient.WriteWordAsync(device, address, value);
}
else if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
         item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
         item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
{
    // DWord/Int/Int32: 兩個連續暫存器 ?
    await plcManager.PlcClient.WriteDWordAsync(device, address, value);
}
```

## 監控地址生成

### GenerateMonitorAddresses()

```csharp
// RecipeContext.cs
public static string GenerateMonitorAddresses()
{
    foreach (var item in CurrentRecipe.Items.Where(x => x.IsEnabled))
    {
        int length = 1;

        // DWord/Float/Int 需要兩個連續暫存器
        if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Float", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
        {
            length = 2; // ? 兩個暫存器
        }

        addresses.Add($"{item.Address}:{length}");
    }
}
```

**輸出範例**：
```
D100:2,D103:1,D104:1,D106:1,D110:1,...
```

說明：
- **D100:2** → 監控 D100 和 D101（Int 類型）
- **D103:1** → 只監控 D103（Word 類型）

## 完整流程

### Recipe.json

```json
{
  "Name": "Heater Temperature",
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int"
}
```

### 載入 Recipe

```
1. Load Recipe
   ↓
2. RecipeContext.LoadRecipeAsync("Recipe.json")
   ↓
3. Recipe 驗證成功
```

### 下載到 PLC

```
4. RecipeContext.DownloadRecipeToPLCAsync(plcManager)
   ↓
5. 檢測到 DataType = "Int"
   ↓
6. 調用 plcManager.PlcClient.WriteDWordAsync("D", 100, 200000)
   ↓
7. WriteDWordAsync 分解為：
   - low = 3392 (0x0D40)
   - high = 3 (0x0003)
   ↓
8. WriteWordsAsync("D", 100, [3392, 3])
   ↓
9. PLC 寫入：
   - D100 = 3392 ?
   - D101 = 3 ?
```

### 監控註冊

```
10. PlcStatus 連線成功
    ↓
11. 自動調用 RecipeContext.GenerateMonitorAddresses()
    ↓
12. 返回 "D100:2,D103:1,D104:1,..."
    ↓
13. PlcStatus.RegisterMonitors("D100:2,...")
    ↓
14. PlcMonitorService 註冊：
    - D100 (length=2) → 監控 D100 和 D101 ?
```

## 日誌輸出

### Recipe 下載

```
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
  → PLC D100 = 3392 (low)
  → PLC D101 = 3 (high)
[Recipe] Download completed successfully: 10/10 parameters written
```

### 監控註冊

```
[AutoRegister] Recipe: D100:2,D103:1,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
  → D100:2 表示監控 D100 和 D101
```

## 驗證方法

### 方法 1：檢查 PLC 數值

使用 PLC 監控工具（如 GX Works）：
```
D100 = 3392 (0x0D40)
D101 = 3 (0x0003)

組合：(D101 << 16) | D100
    = (3 << 16) | 3392
    = 0x00030000 | 0x0D40
    = 200000 ?
```

### 方法 2：使用 PlcLabel 顯示

```xaml
<Controls:PlcLabel 
    Address="D100"
    DataType="DWord"
    Label="Heater Temperature" />
```

應該顯示：**200000** ?

### 方法 3：檢查日誌

```
[Recipe Download] Heater Temperature (D100) = 200000 °C
```

## 重要說明

### 1. 地址不衝突

如果您的 Recipe 有：
```json
[
  { "Address": "D100", "DataType": "Int" },   // 佔用 D100, D101
  { "Address": "D102", "DataType": "Short" }  // 佔用 D102 ?
]
```

**正確**：D100-D101（Int）和 D102（Short）不衝突。

### 2. 地址衝突（錯誤示範）

```json
[
  { "Address": "D100", "DataType": "Int" },   // 佔用 D100, D101
  { "Address": "D101", "DataType": "Short" }  // ? 衝突！
]
```

**錯誤**：D101 被兩個參數使用！

### 3. 您的 Recipe.json

```json
[
  { "Address": "D100", "DataType": "Int" },    // D100, D101
  { "Address": "D103", "DataType": "Word" },   // D103 ?
  { "Address": "D104", "DataType": "Short" },  // D104 ?
  ...
]
```

**正確**：沒有衝突！?

## 總結

### 問題答案

**Q**: Int 類型會寫入連續的 D100、D101 嗎？

**A**: **會！** ?

### 寫入行為

| DataType | 寫入方法 | PLC 地址 | 說明 |
|----------|---------|---------|------|
| Short/Word | `WriteWordAsync` | D100 | 單一暫存器 |
| Int/DWord/Int32 | `WriteDWordAsync` | D100, D101 | **連續兩個暫存器** |

### 數值範圍

| DataType | 位元數 | 範圍 | PLC 佔用 |
|----------|-------|------|---------|
| Short | 16-bit | -32,768 ~ 32,767 | 1 個暫存器 |
| Word | 16-bit | 0 ~ 65,535 | 1 個暫存器 |
| Int/DWord | 32-bit | -2,147,483,648 ~ 2,147,483,647 | 2 個暫存器 |

### 您的 Recipe

```json
{
  "Address": "D100",
  "Value": "200000",
  "DataType": "Int"
}
```

**寫入結果**：
- D100 = 3392 (低 16-bit)
- D101 = 3 (高 16-bit)
- 組合 = 200000 ?

**完全正確！** ??
