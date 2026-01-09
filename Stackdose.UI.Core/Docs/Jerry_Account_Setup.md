# Jerry 帳號設定與診斷指南

## ?? 問題診斷

### **您的情況：**
- Windows 使用者：`jerry`
- Windows 密碼：`me516888`
- 無法登入系統

### **原因：**
jerry 這個 Windows 帳號**尚未在系統中建立**。

---

## ? 解決方式（兩種方法）

### **方法 1：使用 Admin 帳號建立 jerry（推薦）**

#### **步驟 1：使用 Admin 登入**
1. 執行應用程式
2. 登入畫面輸入：
   - User ID: `Admin`
   - Password: `admin123`
3. 登入成功

#### **步驟 2：進入 User Management**
1. 在主視窗找到「User Management」選項
2. 點擊進入使用者管理畫面

#### **步驟 3：新增 jerry 帳號**
1. 點擊「Add User」按鈕
2. 選擇「From AD」（從 Windows AD 建立）
3. 從下拉選單選擇 `jerry`
4. 設定權限等級：
   - **Operator** (一般操作員)
   - **Supervisor** (主管)
   - **Admin** (管理員)
5. 點擊「Create」建立

#### **步驟 4：登出並使用 jerry 登入**
1. 登出 Admin 帳號
2. 重新啟動應用程式
3. 登入畫面應該會自動填入 `jerry`
4. 輸入 Windows 密碼：`me516888`
5. 登入成功！

---

### **方法 2：直接查看資料庫（進階）**

如果想確認 jerry 是否已建立，可以查看資料庫：

#### **步驟 1：找到資料庫檔案**
位置：
```
D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\StackDoseData.db
```

#### **步驟 2：使用 DB Browser for SQLite 開啟**
下載：https://sqlitebrowser.org/

#### **步驟 3：查看 Users 資料表**
執行 SQL：
```sql
SELECT Id, UserId, DisplayName, AccessLevel, IsActive, CreatedAt, CreatedBy 
FROM Users 
ORDER BY Id;
```

#### **預期結果：**
```
| Id | UserId | DisplayName            | AccessLevel | IsActive | CreatedAt           | CreatedBy |
|----|--------|------------------------|-------------|----------|---------------------|-----------|
| 1  | Admin  | System Administrator   | 3 (Admin)   | 1        | 2025-01-10 09:00:00 | System    |
```

**如果沒有 jerry，表示尚未建立**

---

## ?? 權限等級對照表

| AccessLevel 數值 | 權限名稱   | 說明                     |
|------------------|------------|--------------------------|
| 0                | Guest      | 訪客（唯讀）             |
| 1                | Operator   | 操作員（可操作設備）     |
| 2                | Supervisor | 主管（可管理參數）       |
| 3                | Admin      | 管理員（完整權限）       |

---

## ?? 手動建立 jerry 帳號（SQL 方式）

如果您熟悉 SQL，可以直接在資料庫中建立：

```sql
-- 注意：PasswordHash 和 Salt 是使用預設密碼 'password123' 的範例
-- 實際應使用系統的 HashPassword 方法生成

INSERT INTO Users (
    UserId, 
    DisplayName, 
    PasswordHash, 
    Salt, 
    AccessLevel, 
    IsActive, 
    CreatedAt, 
    CreatedBy, 
    Email, 
    Department, 
    Remarks
) VALUES (
    'jerry',
    'Jerry',
    'VeryLongHashString...', -- 需要從程式生成
    'VeryLongSaltString...', -- 需要從程式生成
    1, -- Operator
    1, -- Active
    datetime('now'),
    'Admin',
    'jerry@company.com',
    'Production',
    'Created manually for Windows AD user: jerry'
);
```

**?? 不推薦手動 SQL，因為密碼 Hash 難以手動生成！**

---

## ?? 查看現有帳號

### **方法 1：使用 Admin 登入後查看**
1. 登入為 Admin
2. 進入 User Management
3. 查看使用者列表

### **方法 2：查看資料庫**
```sql
SELECT 
    Id, 
    UserId, 
    DisplayName, 
    CASE AccessLevel 
        WHEN 0 THEN 'Guest'
        WHEN 1 THEN 'Operator'
        WHEN 2 THEN 'Supervisor'
        WHEN 3 THEN 'Admin'
    END AS AccessLevelName,
    CASE IsActive 
        WHEN 1 THEN 'Active'
        ELSE 'Inactive'
    END AS Status,
    Email,
    Department,
    CreatedAt,
    CreatedBy
FROM Users
ORDER BY AccessLevel DESC, UserId;
```

---

## ?? 快速測試流程

### **測試 1：確認 Admin 可以登入**
- User ID: `Admin`
- Password: `admin123`
- 預期：? 登入成功

### **測試 2：確認 jerry 尚未建立**
- User ID: `jerry`
- Password: `me516888`
- 預期：? 顯示「帳號未在系統中註冊」

### **測試 3：使用 Admin 建立 jerry**
1. 登入為 Admin
2. User Management → Add User → From AD → 選擇 jerry
3. 設定權限為 Operator
4. 建立成功

### **測試 4：使用 jerry 登入**
- User ID: `jerry`
- Password: `me516888`
- 預期：? 登入成功

---

## ?? 審計軌跡

所有操作都會記錄到 `UserAuditLogs` 資料表：

```sql
SELECT 
    Timestamp,
    OperatorUserName,
    Action,
    TargetUserName,
    Details
FROM UserAuditLogs
ORDER BY Timestamp DESC
LIMIT 10;
```

---

## ?? 如果還是無法登入

### **檢查清單：**
- [ ] jerry 是否存在於 Windows 本機使用者中
- [ ] jerry 的 Windows 密碼是否正確（`me516888`）
- [ ] jerry 是否已在系統資料庫中建立
- [ ] jerry 的 `IsActive` 狀態是否為 1
- [ ] 應用程式是否有讀取資料庫的權限

### **查詢 jerry 的詳細資訊：**
```sql
SELECT * FROM Users WHERE UserId = 'jerry';
```

---

## ? 建議操作順序

1. **使用 Admin 登入** (`Admin` / `admin123`)
2. **進入 User Management**
3. **點擊 Add User → From AD**
4. **選擇 jerry → 設定權限為 Operator**
5. **登出**
6. **使用 jerry 登入** (`jerry` / `me516888`)

---

## ?? 重要提示

### **Windows AD 驗證優先順序：**
1. **優先使用 Windows 密碼驗證**（如果 jerry 在 Windows 中存在）
2. **Fallback 到本地密碼驗證**（如果 Windows 驗證失敗）

### **本地密碼用途：**
- 當 Windows AD 服務不可用時使用
- 作為備用登入方式
- 不建議設定（優先使用 Windows 密碼）

---

如果您需要幫助建立 jerry 帳號，請執行：

1. 使用 Admin 登入
2. 進入 User Management
3. 告訴我看到了什麼畫面！??
