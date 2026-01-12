# ?? WpfApp1 純 Windows AD 登入指南

## ? 重大變更！

**WpfApp1 現在使用純 Windows AD 驗證，完全比照 WinFormsAD 專案的做法。**

---

## ?? 登入方式

### ? 正確做法（純 Windows AD）

1. **使用 Windows 本機帳號**
   - 帳號：您的 Windows 使用者名稱（例如：`Jerry`、`Admin`、`Operator01` 等）
   - 密碼：**Windows 本機密碼**（不是資料庫密碼）

2. **必須屬於以下群組之一**
   - `App_Operators` → 操作員權限 (Level 1)
   - `App_Instructors` → 指導員權限 (Level 2)
   - `App_Supervisors` → 主管權限 (Level 3)
   - `App_Admins` → 管理員權限 (Level 4)

---

## ? 不再需要

- ? 不需要預先在資料庫建立帳號
- ? 不需要預設的 `admin01` 帳號
- ? 不需要執行「使用者管理」功能來建立帳號
- ? 資料庫 `Users` 表格不再使用（僅保留 Audit Trail）

---

## ?? 設定步驟

### 1. 建立 Windows 本機帳號

在 Windows 中建立本機帳號（如果還沒有）：

```powershell
# 以管理員身份執行 PowerShell

# 建立新使用者（例如：Jerry）
net user Jerry YourPassword123! /add

# 設定密碼永不過期（選用）
wmic useraccount where "name='Jerry'" set PasswordExpires=false
```

---

### 2. 建立 App_ 群組

使用 `WinFormsAD` 專案中的 `GroupInitializerForm` 或手動建立：

```powershell
# 建立四個群組
net localgroup App_Operators /add
net localgroup App_Instructors /add
net localgroup App_Supervisors /add
net localgroup App_Admins /add
```

---

### 3. 將使用者加入群組

```powershell
# 將 Jerry 加入 App_Admins 群組（管理員權限）
net localgroup App_Admins Jerry /add

# 或加入其他群組
net localgroup App_Operators Operator01 /add
net localgroup App_Instructors Instructor01 /add
net localgroup App_Supervisors Supervisor01 /add
```

---

### 4. 確認群組成員

```powershell
# 查看群組成員
net localgroup App_Admins
net localgroup App_Operators
net localgroup App_Instructors
net localgroup App_Supervisors
```

---

## ?? 測試登入

### 範例 1：管理員登入

- **帳號**：`Jerry`（Windows 本機帳號）
- **密碼**：`YourPassword123!`（Windows 密碼）
- **群組**：`App_Admins`
- **結果**：? 登入成功，權限等級 = Admin (L4)

### 範例 2：操作員登入

- **帳號**：`Operator01`
- **密碼**：Windows 密碼
- **群組**：`App_Operators`
- **結果**：? 登入成功，權限等級 = Operator (L1)

### 範例 3：一般使用者（無群組）

- **帳號**：`Guest`
- **密碼**：Windows 密碼
- **群組**：無（不屬於任何 App_ 群組）
- **結果**：? 登入失敗，顯示「不屬於任何 App_ 群組」

---

## ?? 登入流程

```
使用者輸入 Windows 帳號密碼
    ↓
Windows AD 驗證 (5秒超時)
    ↓
    ├─ 密碼錯誤 → ? 登入失敗
    └─ 密碼正確 → 檢查群組
                   ↓
                   ├─ 屬於 App_Admins → ? 登入成功 (Admin)
                   ├─ 屬於 App_Supervisors → ? 登入成功 (Supervisor)
                   ├─ 屬於 App_Instructors → ? 登入成功 (Operator)
                   ├─ 屬於 App_Operators → ? 登入成功 (Operator)
                   └─ 不屬於任何 App_ 群組 → ? 登入失敗
```

---

## ?? 權限對應表

| Windows 群組 | AccessLevel | 說明 |
|-------------|-------------|------|
| `App_Admins` | **Admin (L4)** | 完整系統權限 |
| `App_Supervisors` | **Supervisor (L3)** | 主管權限 |
| `App_Instructors` | **Operator (L2)** | 指導員權限 |
| `App_Operators` | **Operator (L1)** | 操作員權限 |
| **無群組** | ? 無法登入 | 必須屬於上述群組之一 |

---

## ??? 除錯

### 登入失敗時，檢查以下內容：

1. **查看日誌檔案**：
   ```
   {執行目錄}\login_debug.log
   ```

2. **確認 Windows 密碼正確**：
   ```powershell
   # 測試帳號密碼（會提示輸入密碼）
   runas /user:Jerry cmd
   ```

3. **確認群組成員**：
   ```powershell
   net localgroup App_Admins
   net localgroup App_Operators
   ```

4. **檢查 Debug 輸出**（Visual Studio 的「輸出」視窗）：
   ```
   [SecurityContext] ? Login COMPLETED in XXXms
   [SecurityContext]    Auth Method: Windows AD
   [SecurityContext]    AccessLevel: Admin
   ```

---

## ?? 與 WinFormsAD 的一致性

| 功能 | WinFormsAD | WpfApp1 |
|------|-----------|---------|
| 驗證方式 | Windows AD | ? Windows AD |
| 需要資料庫帳號 | ? 否 | ? ? 否 |
| 群組檢測 | ? App_ 群組 | ? App_ 群組 |
| 權限對應 | ? 自動 | ? 自動 |
| 預設帳號 | ? 無 | ? ? 無 |

---

## ? 快速測試清單

- [ ] 建立 Windows 本機帳號（例如：Jerry）
- [ ] 建立 `App_Admins` 群組
- [ ] 將 Jerry 加入 `App_Admins` 群組
- [ ] 刪除舊的 `StackDoseData.db`（不再需要）
- [ ] 執行 WpfApp1
- [ ] 使用 Jerry 的 Windows 密碼登入
- [ ] 確認登入成功，顯示 Admin 權限

---

## ?? 常見問題

### Q1: 我忘記 Windows 密碼怎麼辦？

使用管理員帳號重設：
```powershell
net user Jerry NewPassword123!
```

### Q2: 可以使用網域帳號嗎？

可以，但需要修改 `SecurityContext.UseLocalMachineOnly = false`。

### Q3: 資料庫還有用嗎？

資料庫僅用於儲存 Audit Trail（審計軌跡）和 Data Logs（生產數據），不再儲存使用者帳號。

### Q4: 如何查看我的群組？

```powershell
whoami /groups
```

---

## ?? 完成！

現在 WpfApp1 已經完全比照 WinFormsAD 的做法，使用純 Windows AD 驗證。

**不再需要預設帳號、不再需要資料庫註冊，只要是 Windows 使用者 + App_ 群組，就能登入！**
