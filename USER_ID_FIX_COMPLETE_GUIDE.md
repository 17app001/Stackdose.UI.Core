# UserId 格式自動修正完全指南

## 問題說明

當前系統中 User 欄位顯示為 `SuperAdmin`，而不是 FDA 21 CFR Part 11 要求的完整格式 `UID-000001 (SuperAdmin)`。

這是因為資料庫中的 Users 表的 UserId 欄位還是舊格式（例如 `SuperAdmin`, `admin`, `user1` 等），而不是新的 UID 格式（`UID-000001`, `UID-000002` 等）。

## 解決方案（3選1）

### ? 方案 A: 自動修正工具（推薦，最簡單）

我已經建立了一個自動修正工具，會自動：
- 備份原始資料
- 轉換所有 UserId 為 UID 格式
- 同步更新所有日誌表中的 UserId
- 顯示詳細的修正報告

#### 執行步驟：

1. **執行批次檔**:
   ```
   雙擊 RunUserIdFix.bat
   ```

2. **確認修正**:
   - 工具會顯示修正前後的使用者列表
   - 確認無誤後輸入 `Y` 執行修正

3. **重新啟動應用程式**

4. **重新登入**:
   - 登出當前帳號
   - 使用 `UID-000001` 或 `SuperAdmin` 重新登入
   - 密碼: `superadmin`

5. **驗證**:
   - 檢查介面上的 User 欄位是否顯示為 `UID-000001 (SuperAdmin)`

### 方案 B: 使用 SQL 腳本（需要 SQLite 工具）

如果您熟悉 SQLite 操作，可以使用 `FixUserIdFormat.sql` 腳本。

#### 執行步驟：

1. **開啟 SQLite 工具**:
   - 推薦使用 [DB Browser for SQLite](https://sqlitebrowser.org/)
   - 或使用 SQLite 命令列工具

2. **開啟資料庫**:
   ```
   檔案位置: ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
   ```

3. **執行腳本**:
   - 在 SQL 編輯器中開啟 `FixUserIdFormat.sql`
   - 執行整個腳本

4. **檢查結果**:
   - 腳本會顯示修正前後的對比
   - 確認所有 UserId 都已轉換為 UID 格式

5. **重新啟動應用程式並重新登入**

### 方案 C: 刪除資料庫重建（最乾淨）

如果您不介意清空所有資料（使用者、日誌等），可以刪除資料庫重建。

#### 執行步驟：

1. **關閉應用程式**

2. **刪除資料庫**:
   ```
   刪除檔案: ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
   ```

3. **重新啟動應用程式**:
   - 系統會自動建立新的資料庫
   - 預設建立 `UID-000001 (SuperAdmin)` 帳號

4. **使用預設帳號登入**:
   - 使用者: `UID-000001` 或 `SuperAdmin`
   - 密碼: `superadmin`

## 驗證方式

修正完成後，請執行以下驗證：

### 1. 檢查資料庫

```sql
-- 檢查 Users 表
SELECT Id, UserId, DisplayName, AccessLevel, IsActive 
FROM Users 
ORDER BY AccessLevel DESC;

-- 預期結果：
-- Id  UserId         DisplayName    AccessLevel
-- 1   UID-000001     SuperAdmin     5
```

### 2. 檢查日誌表

```sql
-- 檢查 PeriodicDataLogs
SELECT Id, Timestamp, UserId, BatchId 
FROM PeriodicDataLogs 
ORDER BY Timestamp DESC 
LIMIT 5;

-- 預期結果：UserId 欄位應顯示
-- UID-000001 (SuperAdmin)
```

### 3. 檢查應用程式介面

登入後，檢查以下位置：

- ? 右上角使用者顯示: `SuperAdmin (Level: SuperAdmin)`
- ? Batch 資訊欄位的 User: `UID-000001 (SuperAdmin)`
- ? 日誌檢視器中的 User 欄位: `UID-000001 (SuperAdmin)`

## 檔案說明

我已經建立以下檔案來協助您修正：

| 檔案名稱 | 說明 |
|---------|------|
| `UserIdFixTool\Program.cs` | C# 自動修正工具程式 |
| `UserIdFixTool\UserIdFixTool.csproj` | 工具專案檔 |
| `RunUserIdFix.bat` | 一鍵執行批次檔（推薦） |
| `FixUserIdFormat.sql` | SQL 修正腳本 |
| `USER_ID_FIX_COMPLETE_GUIDE.md` | 本檔案（完整指南） |

## 常見問題

### Q1: 執行 RunUserIdFix.bat 時出現 "找不到資料庫檔案"

**A**: 工具會自動尋找資料庫，如果找不到會要求您手動輸入完整路徑。

預設位置：
```
D:\工作區\Project\Stackdose.UI.Core\ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
```

### Q2: 修正後還是顯示舊格式？

**A**: 請確認已執行以下步驟：
1. ? 關閉並重新啟動應用程式
2. ? 登出當前帳號
3. ? 使用 `UID-000001` 或 `SuperAdmin` 重新登入

### Q3: 修正工具顯示 "0 個使用者需要修正"

**A**: 這表示您的資料庫已經是正確格式！問題可能是：
- 您目前登入的使用者還是舊的 session
- 解決方式：登出並重新登入

### Q4: 可以復原修正嗎？

**A**: 可以！工具會自動建立備份表 `Users_Backup_YYYYMMDDHHMMSS`。

如果需要復原：
```sql
-- 復原 Users 表
DROP TABLE Users;
ALTER TABLE Users_Backup_20250203120000 RENAME TO Users;
```

### Q5: 修正後舊的日誌記錄會受影響嗎？

**A**: 不會！工具會同步更新所有日誌表中的 UserId 欄位，確保格式一致。

包含的表：
- ? AuditTrails
- ? OperationLogs
- ? EventLogs
- ? PeriodicDataLogs

## 技術細節

### 修正邏輯

工具會執行以下操作：

1. **備份**:
   ```sql
   CREATE TABLE Users_Backup_YYYYMMDDHHMMSS AS SELECT * FROM Users;
   ```

2. **轉換 Users 表**:
   ```sql
   UPDATE Users 
   SET UserId = 'UID-' || printf('%06d', Id)
   WHERE UserId NOT LIKE 'UID-%';
   ```

3. **同步更新日誌表**:
   ```sql
   -- 範例：PeriodicDataLogs
   UPDATE PeriodicDataLogs 
   SET UserId = (
       SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
       FROM Users u 
       WHERE u.DisplayName = PeriodicDataLogs.UserId 
          OR u.UserId = PeriodicDataLogs.UserId
   )
   WHERE UserId NOT LIKE 'UID-%(%';
   ```

### UserId 格式說明

- **舊格式**: `SuperAdmin`, `admin`, `user1`
- **新格式**: `UID-000001`, `UID-000002`, `UID-000003`

### 完整格式說明

在日誌中會顯示完整格式：
- **格式**: `UID-XXXXXX (DisplayName)`
- **範例**: `UID-000001 (SuperAdmin)`

這符合 FDA 21 CFR Part 11 規範，可追蹤每個操作的執行者。

## 支援

如果以上方案都無法解決問題，請提供以下資訊：

1. **資料庫查詢結果**:
   ```sql
   SELECT * FROM Users WHERE IsActive = 1;
   ```

2. **最新的 PeriodicDataLogs**:
   ```sql
   SELECT * FROM PeriodicDataLogs ORDER BY Timestamp DESC LIMIT 5;
   ```

3. **應用程式 Debug 輸出**:
   ```
   查看 Visual Studio 的 Output 視窗，搜尋 "CurrentUser"
   ```

---

## 快速開始（TL;DR）

1. 雙擊 `RunUserIdFix.bat`
2. 輸入 `Y` 確認修正
3. 重新啟動應用程式
4. 登出並重新登入
5. 完成！?
