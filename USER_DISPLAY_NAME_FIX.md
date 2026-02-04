# User 顯示名稱修正指南

## 問題分析

從您的截圖可以看到，User 欄位顯示的是 `SuperAdmin`，而不是 `UID-000001 (SuperAdmin)` 的完整格式。

這表示問題出在**登入時使用的使用者帳號本身的 UserId 欄位**。

## 檢查當前登入使用者

使用以下 SQL 查詢檢查資料庫中的使用者資料：

```sql
SELECT 
    Id,
    UserId,
    DisplayName,
    AccessLevel,
    IsActive,
    CreatedAt
FROM Users
WHERE IsActive = 1
ORDER BY AccessLevel DESC, UserId;
```

## 問題場景

### 場景 1: UserId 還沒有轉換為 UID 格式

如果資料庫中的使用者記錄是：
```
Id  UserId         DisplayName    AccessLevel
1   SuperAdmin     SuperAdmin     5
```

那麼當您登入後，`CurrentUser` 會返回 `SuperAdmin (SuperAdmin)`，而不是 `UID-000001 (SuperAdmin)`。

### 場景 2: UserId 已轉換，但顯示名稱不正確

如果資料庫中的使用者記錄是：
```
Id  UserId         DisplayName    AccessLevel
1   UID-000001     SuperAdmin     5
```

那麼 `CurrentUser` 會正確返回 `UID-000001 (SuperAdmin)`。

## 解決方案

### 方案 A: 使用資料庫腳本修正（推薦）

執行以下 SQL 腳本修正所有使用者的 UserId：

```sql
-- 1. 檢查當前狀態
SELECT 'BEFORE' as Status, UserId, DisplayName, AccessLevel FROM Users ORDER BY AccessLevel DESC;

-- 2. 修正 SuperAdmin 的 UserId
UPDATE Users 
SET UserId = 'UID-000001' 
WHERE Id = 1 AND UserId != 'UID-000001';

-- 3. 修正其他使用者的 UserId（如果有多個使用者）
UPDATE Users 
SET UserId = 'UID-' || printf('%06d', Id)
WHERE UserId NOT LIKE 'UID-%';

-- 4. 檢查修正後的狀態
SELECT 'AFTER' as Status, UserId, DisplayName, AccessLevel FROM Users ORDER BY AccessLevel DESC;
```

### 方案 B: 刪除資料庫重建

1. 關閉程式
2. 刪除 `StackDoseData.db`
3. 重新啟動程式（會自動建立新資料庫）
4. 預設帳號會是 `UID-000001 (SuperAdmin)`，密碼 `superadmin`

### 方案 C: 重新登入（如果資料庫已正確）

如果資料庫中的 UserId 已經是 UID 格式，但目前顯示仍不正確：

1. **登出**當前帳號
2. **重新登入**：
   - 使用者名稱: `UID-000001` 或 `SuperAdmin`
   - 密碼: `superadmin`

## 驗證步驟

### 1. 檢查資料庫

使用 SQLite 工具執行：

```sql
SELECT 
    Id,
    UserId,
    DisplayName,
    AccessLevel,
    IsActive,
    CreatedAt,
    LastLoginAt
FROM Users
WHERE IsActive = 1
ORDER BY AccessLevel DESC;
```

預期結果應該是：
```
Id  UserId         DisplayName    AccessLevel  IsActive
1   UID-000001     SuperAdmin     5            1
```

### 2. 檢查當前登入狀態

在程式中執行以下 Debug 輸出（已包含在 ComplianceContext.cs 中）：

```csharp
#if DEBUG
var user = SecurityContext.CurrentSession?.CurrentUser;
if (user != null)
{
    System.Diagnostics.Debug.WriteLine($"Current User:");
    System.Diagnostics.Debug.WriteLine($"  Id: {user.Id}");
    System.Diagnostics.Debug.WriteLine($"  UserId: {user.UserId}");
    System.Diagnostics.Debug.WriteLine($"  DisplayName: {user.DisplayName}");
    System.Diagnostics.Debug.WriteLine($"  AccessLevel: {user.AccessLevel}");
    System.Diagnostics.Debug.WriteLine($"  CurrentUser (formatted): {ComplianceContext.CurrentUser}");
}
#endif
```

預期輸出應該是：
```
Current User:
  Id: 1
  UserId: UID-000001
  DisplayName: SuperAdmin
  AccessLevel: SuperAdmin
  CurrentUser (formatted): UID-000001 (SuperAdmin)
```

### 3. 檢查 PeriodicDataLogs

執行以下 SQL 查詢：

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

預期結果 UserId 欄位應該顯示：
```
UserId
UID-000001 (SuperAdmin)
UID-000001 (SuperAdmin)
...
```

## 測試記錄新的 Periodic Data

在程式中執行：

```csharp
// 確認當前使用者
System.Diagnostics.Debug.WriteLine($"Current User: {ComplianceContext.CurrentUser}");
System.Diagnostics.Debug.WriteLine($"Current UserId: {ComplianceContext.CurrentUserId}");
System.Diagnostics.Debug.WriteLine($"Current UserName: {ComplianceContext.CurrentUserName}");

// 記錄新的 Periodic Data
ComplianceContext.LogPeriodicData("TEST-BATCH-001", 65.2, 78.5, 6.15);

// 刷新到資料庫
ComplianceContext.FlushLogs();
```

然後查詢資料庫：

```sql
SELECT * FROM PeriodicDataLogs 
WHERE BatchId = 'TEST-BATCH-001' 
ORDER BY Timestamp DESC 
LIMIT 1;
```

檢查 UserId 欄位是否為 `UID-000001 (SuperAdmin)`。

## 常見問題

### Q1: 為什麼 User 欄位只顯示 DisplayName?

**A**: 這表示當前登入的使用者帳號的 `UserId` 欄位不是 UID 格式。需要：
1. 檢查資料庫中的 Users 表
2. 確認 UserId 是否為 `UID-000001` 格式
3. 如果不是，使用方案 A 或 B 修正

### Q2: 我已經修正資料庫，但顯示還是不對?

**A**: 需要重新登入：
1. 登出當前帳號
2. 重新登入（系統會重新從資料庫讀取使用者資料）

### Q3: 如何確認 ComplianceContext.CurrentUser 返回正確?

**A**: 在任何地方加入以下 Debug 輸出：
```csharp
System.Diagnostics.Debug.WriteLine($"CurrentUser: {ComplianceContext.CurrentUser}");
System.Diagnostics.Debug.WriteLine($"CurrentUserId: {ComplianceContext.CurrentUserId}");
System.Diagnostics.Debug.WriteLine($"CurrentUserName: {ComplianceContext.CurrentUserName}");
```

預期輸出：
```
CurrentUser: UID-000001 (SuperAdmin)
CurrentUserId: UID-000001
CurrentUserName: SuperAdmin
```

## 建議操作流程

1. ? **第一步**: 使用 SQLite 工具查詢資料庫，確認 Users 表中的 UserId 格式
2. ? **第二步**: 如果不是 UID 格式，執行方案 A 的 SQL 腳本修正
3. ? **第三步**: 在程式中登出並重新登入
4. ? **第四步**: 記錄新的 Periodic Data 並檢查資料庫

## SQL 快速修正腳本（複製貼上執行）

```sql
-- ========== 一鍵修正所有使用者的 UserId ==========

BEGIN TRANSACTION;

-- 1. 備份 (optional)
CREATE TABLE IF NOT EXISTS Users_Backup AS SELECT * FROM Users;

-- 2. 修正所有 UserId 為 UID 格式
UPDATE Users 
SET UserId = 'UID-' || printf('%06d', Id)
WHERE UserId NOT LIKE 'UID-%';

-- 3. 確認修正結果
SELECT 
    'Users Updated' as Status,
    COUNT(*) as Total,
    SUM(CASE WHEN UserId LIKE 'UID-%' THEN 1 ELSE 0 END) as UID_Format_Count
FROM Users;

-- 4. 顯示所有使用者
SELECT Id, UserId, DisplayName, AccessLevel, IsActive 
FROM Users 
ORDER BY AccessLevel DESC, UserId;

COMMIT;
```

執行後應該會看到所有使用者的 UserId 都變成 `UID-XXXXXX` 格式。

## 聯絡支援

如果以上方案都無法解決問題，請提供以下資訊：

1. **資料庫查詢結果**:
   ```sql
   SELECT * FROM Users WHERE IsActive = 1;
   ```

2. **Debug 輸出**:
   ```csharp
   System.Diagnostics.Debug.WriteLine($"CurrentUser: {ComplianceContext.CurrentUser}");
   ```

3. **PeriodicDataLogs 最新記錄**:
   ```sql
   SELECT * FROM PeriodicDataLogs ORDER BY Timestamp DESC LIMIT 5;
   ```
