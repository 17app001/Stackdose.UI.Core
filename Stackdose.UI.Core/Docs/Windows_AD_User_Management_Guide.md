# ?? WpfApp1 Windows AD 使用者管理完整指南

## ? 重大更新！

**WpfApp1 現在可以直接操作 Windows AD，完全比照 WinFormsAD 的實作！**

---

## ?? 核心功能

### 1. **建立 Windows 使用者**
   - ? 直接在 Windows 系統中建立本機帳號
   - ? 自動加入指定的 App_ 群組
   - ? 設定密碼、顯示名稱、描述

### 2. **管理 Windows 群組**
   - ? 加入/移除 App_ 群組
   - ? 支援多群組管理
   - ? 即時更新群組成員資格

### 3. **停用/啟用帳號**
   - ? 直接停用 Windows 使用者
   - ? 重新啟用已停用帳號
   - ? 防止停用自己的帳號

### 4. **重設密碼**
   - ? 直接重設 Windows 使用者密碼
   - ? 無需舊密碼（管理員權限）

---

## ?? 重要前提

### 必須以**管理員權限**執行

**為什麼需要管理員權限？**
- 建立/刪除 Windows 使用者
- 修改 Windows 群組成員
- 停用/啟用帳號
- 重設密碼

**如何以管理員身分執行？**
1. 右鍵點擊 `WpfApp1.exe`
2. 選擇「以系統管理員身分執行」

---

## ?? 使用步驟

### 步驟 1：以管理員身分執行

```powershell
# 方法 A：右鍵執行
# 右鍵 WpfApp1.exe → 以系統管理員身分執行

# 方法 B：PowerShell（以管理員身分）
cd "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows"
Start-Process .\WpfApp1.exe -Verb RunAs
```

---

### 步驟 2：登入系統

使用您的 Windows 帳號登入（例如：`admin01` / `admin01admin01`）

---

### 步驟 3：開啟使用者管理

1. 點擊側邊欄的「使用者管理」
2. 系統會自動載入所有 App_ 群組的使用者

---

## ?? 使用者管理功能

### ? 新增使用者

1. 點擊「? 新增使用者」
2. 填寫資料：
   - **使用者名稱**：Windows 帳號名稱（例如：`operator01`）
   - **顯示名稱**：完整名稱（例如：`操作員01`）
   - **密碼**：至少 8 個字元
   - **確認密碼**：再次輸入密碼
   - **群組**：選擇 `App_Operators`、`App_Instructors`、`App_Supervisors`、或 `App_Admins`
   - **描述**：可選，帳號說明
3. 點擊「? 確定」

**結果：**
- ? Windows 中建立新使用者
- ? 自動加入選擇的群組
- ? 密碼永不過期
- ? 帳號已啟用

---

### ? 變更群組

1. 選擇使用者
2. 點擊「? 編輯」（變更群組）
3. 勾選/取消勾選群組：
   - `App_Operators` (操作員)
   - `App_Instructors` (指導員)
   - `App_Supervisors` (主管)
   - `App_Admins` (管理員)
4. 點擊「? 確定」

**結果：**
- ? 使用者立即加入新群組
- ? 從取消勾選的群組中移除
- ? 登入權限立即更新

---

### ? 重設密碼

1. 選擇使用者
2. 點擊「?? 重設密碼」
3. 輸入新密碼
4. 點擊「確定」

**結果：**
- ? Windows 密碼已重設
- ? 使用者下次登入使用新密碼
- ? 記錄 Audit Trail

---

### ? 停用/啟用帳號

1. 選擇使用者
2. 點擊「?? 停用」或「? 啟用」
3. 確認操作

**結果：**
- ? 帳號立即停用/啟用
- ? 停用的帳號無法登入
- ? 啟用的帳號可立即登入

---

### ? 檢視使用者資訊

1. 選擇使用者
2. 點擊「?? 檢視記錄」
3. 查看：
   - 使用者名稱
   - 顯示名稱
   - 狀態（啟用/停用）
   - 所屬群組清單

---

## ?? 與 WinFormsAD 的對比

| 功能 | WinFormsAD | WpfApp1 | 
|------|-----------|---------|
| 建立 Windows 使用者 | ? | ? |
| 變更群組 | ? | ? |
| 停用/啟用帳號 | ? | ? |
| 重設密碼 | ? | ? |
| 需要管理員權限 | ? | ? |
| 使用 DirectoryServices.AccountManagement | ? | ? |
| Audit Trail 記錄 | ? | ? |

**完全一致！** ?

---

## ??? 權限對應表

| Windows 群組 | AccessLevel | 可管理 |
|-------------|-------------|--------|
| `App_Admins` | Admin (L4) | 所有群組 |
| `App_Supervisors` | Supervisor (L3) | Operators, Instructors, Supervisors |
| `App_Instructors` | Instructor (L2) | Operators |
| `App_Operators` | Operator (L1) | 無（僅檢視） |

---

## ?? 常見問題

### Q1: 為什麼顯示「此操作需要系統管理員權限」？

**原因：** 程式未以管理員身分執行

**解決方法：**
1. 右鍵點擊 `WpfApp1.exe`
2. 選擇「以系統管理員身分執行」

---

### Q2: 我可以刪除 Windows 使用者嗎？

**目前版本不支援刪除**，建議使用「停用」功能：
- ? 停用帳號（無法登入）
- ? 不刪除帳號（保留 Audit Trail）

如需刪除，請使用「電腦管理」：
```
右鍵「本機」→ 管理 → 本機使用者和群組 → 使用者
```

---

### Q3: 如何確認帳號是否建立成功？

**方法 A：WpfApp1**
1. 點擊「?? 重新整理」
2. 確認使用者出現在清單中

**方法 B：PowerShell**
```powershell
# 檢查使用者
net user operator01

# 檢查群組成員
net localgroup App_Operators
```

**方法 C：電腦管理**
```
右鍵「本機」→ 管理 → 本機使用者和群組 → 使用者
```

---

### Q4: 密碼有什麼要求？

- ? 至少 8 個字元
- ? 建議包含大小寫字母、數字、符號
- ? 不能與使用者名稱相同

---

### Q5: 可以一次將使用者加入多個群組嗎？

**可以！**
1. 點擊「? 編輯」（群組管理）
2. 勾選多個群組
3. 點擊「? 確定」

---

### Q6: 修改群組後需要重新登入嗎？

**是的。**
- ? 群組變更立即生效
- ? 已登入的使用者需要重新登入才能更新權限

---

### Q7: Audit Trail 會記錄什麼？

- ? 建立使用者（隱含，透過 Windows Event Log）
- ? 重設密碼
- ? 停用/啟用帳號
- ? 變更群組（隱含）

---

## ?? 安全建議

### 1. **管理員帳號保護**
- ? 不要讓太多人擁有 `App_Admins` 群組成員資格
- ? 定期檢查群組成員

### 2. **密碼政策**
- ? 強制使用強密碼
- ? 定期更換密碼
- ? 不要重複使用舊密碼

### 3. **帳號管理**
- ? 離職人員立即停用帳號
- ? 定期檢查停用帳號清單
- ? 使用描述欄位記錄帳號用途

---

## ?? 測試範例

### 範例 1：建立操作員帳號

```
使用者名稱：operator01
顯示名稱：操作員01 - 早班
密碼：Test1234!
確認密碼：Test1234!
群組：App_Operators
描述：早班操作員
```

**登入測試：**
- 使用者：`operator01`
- 密碼：`Test1234!`
- 權限：Operator (L1)

---

### 範例 2：建立管理員帳號

```
使用者名稱：admin02
顯示名稱：系統管理員 - IT
密碼：Admin1234!
確認密碼：Admin1234!
群組：App_Admins
描述：IT 部門管理員
```

**登入測試：**
- 使用者：`admin02`
- 密碼：`Admin1234!`
- 權限：Admin (L4)

---

## ?? 技術細節

### 核心類別

1. **`WindowsAccountService.cs`**
   - 完全比照 WinFormsAD 的 `AccountService.cs`
   - 使用 `System.DirectoryServices.AccountManagement`
   - 支援 `ContextType.Machine`（本機）和 `ContextType.Domain`（網域）

2. **`UserManagementPanel.xaml.cs`**
   - 改用 `WindowsAccountService`
   - 不再使用資料庫的 `UserManagementService`

3. **`UserEditorDialog.xaml.cs`**
   - 建立 Windows 使用者
   - 直接呼叫 Windows AD API

4. **`GroupManagementDialog.xaml.cs`**
   - 新增，用於管理群組成員資格
   - 支援多群組勾選

---

## ?? 完成！

**WpfApp1 現在完全比照 WinFormsAD 的實作，可以直接操作 Windows AD！**

---

## ?? 支援

如有問題，請查看：
- `login_debug.log` - 登入日誌
- Windows Event Viewer - 使用者帳號事件
- `StackDoseData.db` - Audit Trail 記錄
