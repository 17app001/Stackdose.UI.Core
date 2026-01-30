# FDA 21 CFR Part 11 Compliance Fix Summary

## 修正項目

### 1. AuditLog 方法簽名更新

**舊版本：**
```csharp
SqliteLogger.LogAudit(user, action, device, oldVal, newVal, reason);
```

**新版本：**
```csharp
SqliteLogger.LogAudit(user, action, device, oldVal, newVal, reason, parameter, batchId);
```

**需要更新的檔案：**
- `Stackdose.UI.Core\Helpers\ComplianceContext.cs` - LogAuditTrail 方法
- `Stackdose.UI.Core\Helpers\SecurityContext.cs` - 登入/登出記錄
- 其他呼叫 LogAudit 的地方

### 2. 新增 LogPeriodicData 方法

製程中每5秒記錄一筆資料：
```csharp
SqliteLogger.LogPeriodicData(batchId, predryTemp, dryTemp, cdaInletPressure);
```

### 3. 時間戳記精度

所有時間戳記已使用 `DateTime.Now` (包含毫秒精度)

### 4. 必要事件記錄

需要在適當位置添加：
- EMO 開/關 → EventLog (Safety Event, Critical)
- CDA Loss → EventLog (Warning, Major)
- 生產啟動/結束 → OperationLog
- 緊急停止/恢復 → OperationLog + EventLog

## 修正步驟

1. ? 更新 SqliteLogger 資料結構
2. ? 添加 LogPeriodicData 方法
3. ? 更新 ComplianceContext.LogAuditTrail 簽名
4. ? 更新 SecurityContext 登入/登出記錄
5. ? 添加 FlushPeriodicDataLogs 方法
6. ? 更新統計資訊方法

