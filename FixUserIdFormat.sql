-- ========================================
-- UserId 格式自動修正 SQL 腳本
-- Auto-fix Script for UserId Format
-- ========================================

-- 1. 顯示修正前的狀態
SELECT '========== 修正前 (BEFORE) ==========' as Status;
SELECT Id, UserId, DisplayName, AccessLevel, IsActive 
FROM Users 
ORDER BY AccessLevel DESC, Id;

-- 2. 建立備份表（加上時間戳記）
CREATE TABLE IF NOT EXISTS Users_Backup_Manual AS SELECT * FROM Users;

-- 3. 開始事務
BEGIN TRANSACTION;

-- 4. 修正 Users 表的 UserId
UPDATE Users 
SET UserId = 'UID-' || printf('%06d', Id)
WHERE UserId NOT LIKE 'UID-%';

-- 5. 修正 AuditTrails 表的 User 欄位
UPDATE AuditTrails 
SET User = COALESCE(
    (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
     FROM Users u 
     WHERE u.DisplayName = AuditTrails.User 
        OR u.UserId = AuditTrails.User),
    'System'
)
WHERE User NOT LIKE 'UID-%(%)'
  AND User IS NOT NULL
  AND User != '';

-- 6. 修正 OperationLogs 表的 UserId 欄位
UPDATE OperationLogs 
SET UserId = COALESCE(
    (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
     FROM Users u 
     WHERE u.DisplayName = OperationLogs.UserId 
        OR u.UserId = OperationLogs.UserId),
    'System'
)
WHERE UserId NOT LIKE 'UID-%(%)'
  AND UserId IS NOT NULL
  AND UserId != '';

-- 7. 修正 EventLogs 表的 UserId 欄位
UPDATE EventLogs 
SET UserId = COALESCE(
    (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
     FROM Users u 
     WHERE u.DisplayName = EventLogs.UserId 
        OR u.UserId = EventLogs.UserId),
    'System'
)
WHERE UserId NOT LIKE 'UID-%(%)'
  AND UserId IS NOT NULL
  AND UserId != '';

-- 8. 修正 PeriodicDataLogs 表的 UserId 欄位
UPDATE PeriodicDataLogs 
SET UserId = COALESCE(
    (SELECT 'UID-' || printf('%06d', u.Id) || ' (' || u.DisplayName || ')'
     FROM Users u 
     WHERE u.DisplayName = PeriodicDataLogs.UserId 
        OR u.UserId = PeriodicDataLogs.UserId),
    'System'
)
WHERE UserId NOT LIKE 'UID-%(%)'
  AND UserId IS NOT NULL
  AND UserId != '';

-- 9. 提交事務
COMMIT;

-- 10. 顯示修正後的狀態
SELECT '========== 修正後 (AFTER) ==========' as Status;
SELECT Id, UserId, DisplayName, AccessLevel, IsActive 
FROM Users 
ORDER BY AccessLevel DESC, Id;

-- 11. 統計資訊
SELECT '========== 統計資訊 (STATISTICS) ==========' as Status;
SELECT 
    (SELECT COUNT(*) FROM Users) as TotalUsers,
    (SELECT COUNT(*) FROM Users WHERE UserId LIKE 'UID-%') as UidFormatUsers,
    (SELECT COUNT(*) FROM AuditTrails WHERE User LIKE 'UID-%(%') as AuditTrailsFixed,
    (SELECT COUNT(*) FROM OperationLogs WHERE UserId LIKE 'UID-%(%') as OperationLogsFixed,
    (SELECT COUNT(*) FROM EventLogs WHERE UserId LIKE 'UID-%(%') as EventLogsFixed,
    (SELECT COUNT(*) FROM PeriodicDataLogs WHERE UserId LIKE 'UID-%(%') as PeriodicDataFixed;

-- 完成
SELECT '? 修正完成！請重新啟動應用程式並重新登入。' as Message;
