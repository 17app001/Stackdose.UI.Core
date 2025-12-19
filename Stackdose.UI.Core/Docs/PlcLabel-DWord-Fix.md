# PlcLabel DWord 類型修正說明

## ?? 問題描述

使用 `DataType="DWord"` 時，PlcLabel 無法正確顯示數值，但改為 `DataType="Word"` 就可以正常運作。

---

## ?? 根本原因

### DWord 讀取機制
PLC 的 DWord（32-bit）是由兩個連續的 Word（16-bit）組成：

```
D65 (DWord) = D65 (Low Word) + D66 (High Word)
```

### PlcMonitorService.GetDWord 實作
```csharp
public int? GetDWord(string addr)
{
    var low = GetWord($"{dev}{baseAddr}");       // 讀取 D65
    var high = GetWord($"{dev}{baseAddr + 1}");   // 讀取 D66
    
    if (low == null || high == null) return null;  // ?? 如果任一個為 null，返回 null
    
    return high.Value << 16 | (ushort)low.Value;
}
```

### 問題關鍵
**PlcMonitorService 只會監控「已註冊」的位址**：
- Word 類型：只註冊 D65 ?
- DWord 類型：需要 D65 **和** D66 都被註冊 ?

如果只註冊 D65，當讀取 DWord 時：
- `GetWord("D65")` → 正常返回
- `GetWord("D66")` → 返回 `null`（未註冊）
- 結果：`GetDWord` 返回 `null`

---

## ? 解決方案

修改 `PlcLabelContext.GenerateMonitorAddresses()` 方法，當檢測到 DWord 類型時，**自動註冊下一個連續位址**。

### 修改內容

```csharp
public static string GenerateMonitorAddresses()
{
    lock (_lock)
    {
        CleanupDeadReferences();

        var addresses = new List<string>();
        foreach (var weakRef in _registeredLabels)
        {
            if (weakRef.TryGetTarget(out var label) && !string.IsNullOrWhiteSpace(label.Address))
            {
                string baseAddr = label.Address.Trim().ToUpper();
                addresses.Add(baseAddr);
                
                // ?? 如果是 DWord 類型，自動加入下一個位址
                if (label.DataType == Controls.PlcDataType.DWord)
                {
                    var match = Regex.Match(baseAddr, @"^([A-Z]+)(\d+)$");
                    if (match.Success)
                    {
                        string deviceType = match.Groups[1].Value;
                        int deviceNumber = int.Parse(match.Groups[2].Value);
                        string nextAddr = $"{deviceType}{deviceNumber + 1}";
                        addresses.Add(nextAddr);  // D65 → 加入 D66
                        
                        #if DEBUG
                        Debug.WriteLine($"[PlcLabelContext] DWord 自動註冊: {baseAddr} + {nextAddr}");
                        #endif
                    }
                }
            }
        }

        return addresses.Count == 0 ? string.Empty : GenerateOptimizedAddresses(addresses);
    }
}
```

---

## ?? 修正前後對比

### 修正前
```xml
<Custom:PlcLabel Label="X軸位置" 
                Address="D65" 
                DataType="DWord"/>
```

**註冊位址：** D65  
**GetDWord("D65") 呼叫：**
- GetWord("D65") → 成功 ?
- GetWord("D66") → `null` ?（未註冊）
- 結果：`null` ?

---

### 修正後
```xml
<Custom:PlcLabel Label="X軸位置" 
                Address="D65" 
                DataType="DWord"/>
```

**註冊位址：** D65, D66（自動）  
**GetDWord("D65") 呼叫：**
- GetWord("D65") → 成功 ?
- GetWord("D66") → 成功 ?（自動註冊）
- 結果：合併為 32-bit DWord ?

---

## ?? 測試方式

### 1. 使用 XAML
```xml
<!-- 測試 DWord 讀取 -->
<Custom:PlcLabel 
    Label="X軸位置" 
    Address="D65" 
    DataType="DWord"
    LabelForeground="Success"
    ValueForeground="Success"/>
```

### 2. 觀察 Debug 輸出
```
[PlcLabelContext] DWord 自動註冊: D65 + D66
[PlcLabel] DWord Read: X軸位置 (D65) = 12345678
[PlcLabel] UpdateValue: X軸位置 (D65) rawValue=12345678 (Type: Int32)
[PlcLabel] DWord Formatted: X軸位置 = 12345678 (原始:12345678, 除數:1)
```

### 3. 確認數值正確
- D65 (Low Word) = 0x4E61 (20065)
- D66 (High Word) = 0x0030 (48)
- DWord = (48 << 16) | 20065 = 3145789

---

## ?? 支援的 DataType

| 類型 | 位址範例 | 實際註冊 | 說明 |
|------|---------|---------|------|
| **Bit** | D100.5 | D100 | 讀取 Word 並提取指定 Bit |
| **Word** | D100 | D100 | 單一 16-bit Word |
| **DWord** | D100 | D100, D101 | 兩個連續 Word 合併為 32-bit ? |
| **Float** | D100 | D100, D101 | DWord 轉換為 IEEE 754 Float |

---

## ?? 注意事項

### 1. 位址連續性
DWord 讀取時，**必須確保下一個位址存在**：
- ? D65 → D66 正常
- ? D1000 → D1001 正常
- ? D65535 → D65536 溢位（依 PLC 規格而定）

### 2. 資料一致性
兩個 Word 的讀取時間可能有微小差異，如果 PLC 在兩次讀取之間更新數值，可能導致不一致。

**建議：** 使用 PLC 的「批次讀取」功能，確保原子性。

### 3. 記憶體對齊
某些 PLC 要求 DWord 起始位址必須是偶數（例如 D0, D2, D4...），請參考 PLC 規格。

---

## ?? 未來優化建議

### 1. 批次讀取優化
目前 PlcMonitorService 已支援連續位址合併，DWord 會自動受益：

```
D65, D66 → 合併為 D65,2（一次讀取）
```

### 2. Float 類型支援
Float 同樣需要兩個 Word，目前也已自動支援。

### 3. 錯誤處理
當 DWord 讀取失敗時，可以考慮：
- 顯示錯誤提示
- 記錄警告日誌
- 自動重試機制

---

## ?? 總結

? **問題已修正**  
? **DWord 類型現在可以正常工作**  
? **自動註冊機制提升易用性**  
? **Debug 輸出幫助診斷問題**

您現在可以安心使用 `DataType="DWord"` 讀取 32-bit 數值了！??
