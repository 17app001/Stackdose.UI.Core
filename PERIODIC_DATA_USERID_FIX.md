# PERIODIC DATA UserId + DisplayName 修正說明

## 問題描述
PeriodicDataLogs 資料表中的記錄沒有正確顯示 `UserId` 和 `DisplayName`，導致 FDA 21 CFR Part 11 合規性追蹤不完整。

## 根本原因
在 `FdaLogDataGenerator.cs` 的 `GeneratePeriodicDataLogs()` 方法中，生成測試資料時**沒有包含 `UserId` 欄位**，導致資料庫中的記錄缺少使用者追蹤資訊。

## 修正內容

### 1. FdaLogDataGenerator.cs - 新增 UserId 欄位到測試資料生成
**檔案位置**: `Stackdose.UI.Core\Helpers\FdaLogDataGenerator.cs`

**修正內容**:
- ? 在 `GeneratePeriodicDataLogs()` 方法中新增 `UserId` 欄位
- ? 使用 FDA 合規格式 `"UID-XXXXXX (DisplayName)"` 生成測試使用者
- ? 隨機生成 5 個測試使用者 (UID-000001 ~ UID-000005)

```csharp
// ?? Generate random test user in FDA compliant format "UID-XXXXXX (DisplayName)"
var userIndex = _random.Next(1, 6); // USER001-USER005
var userId = $"UID-{userIndex:D6} (Test User {userIndex})";

// ?? Now includes UserId for FDA 21 CFR Part 11 compliance
conn.Execute(
    @"INSERT INTO PeriodicDataLogs (Timestamp, BatchId, UserId, PredryTemp, DryTemp, CdaInletPressure) 
      VALUES (@Timestamp, @BatchId, @UserId, @PredryTemp, @DryTemp, @CdaInletPressure)",
    new
    {
        Timestamp = timestamp,
        BatchId = finalBatchId,
        UserId = userId, // ?? 新增此欄位
        PredryTemp = Math.Round(predryTemp, 2),
        DryTemp = Math.Round(dryTemp, 2),
        CdaInletPressure = Math.Round(cdaPressure, 3)
    },
    transaction
);
```

### 2. ComplianceContext.cs - 新增除錯日誌
**檔案位置**: `Stackdose.UI.Core\Helpers\ComplianceContext.cs`

**修正內容**:
- ? 在 `LogPeriodicData()` 方法中新增 Debug 輸出
- ? 顯示當前記錄的 BatchId、UserId 和製程參數

```csharp
#if DEBUG
System.Diagnostics.Debug.WriteLine($"[ComplianceContext] LogPeriodicData: BatchId={batchId}, UserId={userId}, PredryTemp={predryTemp}, DryTemp={dryTemp}, CDA={cdaInletPressure}");
#endif
```

### 3. SqliteLogger.cs - 強化除錯輸出
**檔案位置**: `Stackdose.UI.Core\Helpers\SqliteLogger.cs`

**修正內容 A**: `LogPeriodicData()` 方法
- ? 顯示 UserId 格式化前後的變化

```csharp
#if DEBUG
if (userId != formattedUserId)
{
    System.Diagnostics.Debug.WriteLine($"[SqliteLogger] LogPeriodicData - UserId formatted: '{userId}' -> '{formattedUserId}'");
}
#endif
```

**修正內容 B**: `FlushPeriodicDataLogs()` 方法
- ? 顯示批次寫入的 UserId 樣本

```csharp
#if DEBUG
// ?? Show sample UserId for verification
var sampleUserId = batch.FirstOrDefault()?.UserId ?? "N/A";
System.Diagnostics.Debug.WriteLine($"[SqliteLogger] PeriodicDataLogs 批次寫入: {batch.Count} 筆 (累計: {_totalPeriodicDataLogs}) - Sample UserId: {sampleUserId}");
#endif
```

## 測試方式

### 1. 重新生成測試資料
在測試程式中執行：
```csharp
// 清空舊資料（選填）
// DELETE FROM PeriodicDataLogs;

// 生成新的測試資料（包含 UserId）
FdaLogDataGenerator.GeneratePeriodicDataLogs(500);
```

### 2. 驗證資料格式
執行 SQL 查詢：
```sql
SELECT 
    Id,
    Timestamp,
    BatchId,
    UserId,
    PredryTemp,
    DryTemp,
    CdaInletPressure
FROM PeriodicDataLogs
ORDER BY Timestamp DESC
LIMIT 10;
```

**預期結果**:
```
UserId 欄位應顯示：UID-000001 (Test User 1)
                    UID-000002 (Test User 2)
                    ... 等
```

### 3. 檢查 Debug 輸出
在 Visual Studio 的「輸出」視窗中應該會看到：
```
[ComplianceContext] LogPeriodicData: BatchId=BATCH-20260203-001, UserId=UID-000001 (Super Administrator), PredryTemp=65.2, DryTemp=78.5, CDA=6.15
[SqliteLogger] PeriodicDataLogs 批次寫入: 100 筆 (累計: 100) - Sample UserId: UID-000001 (Super Administrator)
```

## 實際生產環境行為
當系統在實際生產環境中記錄週期性資料時：

1. **自動取得當前使用者**: 
   - `ComplianceContext.LogPeriodicData()` 會自動從 `SecurityContext.CurrentSession.CurrentUser` 取得當前登入使用者
   - 格式為 `"UID-XXXXXX (DisplayName)"`

2. **符合 FDA 21 CFR Part 11 規範**:
   - ? 每筆週期性製程資料都包含操作使用者 ID
   - ? 可追蹤「誰」在「何時」執行了「哪個批次」的製程
   - ? 符合電子紀錄與電子簽章要求

## 相關檔案
1. `Stackdose.UI.Core\Helpers\FdaLogDataGenerator.cs` - 測試資料生成器
2. `Stackdose.UI.Core\Helpers\ComplianceContext.cs` - 合規引擎 API
3. `Stackdose.UI.Core\Helpers\SqliteLogger.cs` - 資料庫記錄器
4. `Stackdose.UI.Core\Helpers\SecurityContext.cs` - 安全上下文（使用者工作階段管理）

## 後續工作
- ? 修正完成 - 所有新記錄的 PeriodicData 都會包含 UserId
- ?? 舊有資料 - 資料庫初始化時的修正程式碼會自動將空白/NULL 的 UserId 更新為 `"UID-000001 (Super Administrator)"`
- ? 編譯成功 - 所有修正已通過編譯驗證

## 總結
此修正確保所有週期性製程資料記錄都包含完整的使用者追蹤資訊，符合 FDA 21 CFR Part 11 電子紀錄規範要求。
