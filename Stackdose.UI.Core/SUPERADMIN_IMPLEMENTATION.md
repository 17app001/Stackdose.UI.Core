# SuperAdmin 權限實作完成

## ? 完成的修改

### 1?? **AccessLevel.cs** - 添加 SuperAdmin (L5)

```csharp
public enum AccessLevel
{
    Guest = 0,           // 未登入
    Operator = 1,        // L1 - 操作員
    Instructor = 2,      // L2 - 指導員
    Supervisor = 3,      // L3 - 主管
    Admin = 4,           // L4 - 管理員
    SuperAdmin = 5       // L5 - 超級管理員 ? 新增
}
```

---

### 2?? **預設帳號創建**

| 帳號 | 密碼 | 權限等級 | Email |
|------|------|---------|-------|
| `superadmin` | `superadminsuperadmin` | SuperAdmin (L5) | superadmin@stackdose.com |
| `admin01` | `admin123` | Admin (L4) | admin@stackdose.com |

**自動創建時機**：
- 程式啟動時，UserManagementService 會自動檢查
- 如果資料庫中沒有 SuperAdmin 或 Admin，會自動創建

---

### 3?? **權限層級對照表**

| 功能 | L1<br>Operator | L2<br>Instructor | L3<br>Supervisor | L4<br>Admin | L5<br>SuperAdmin |
|------|:--------------:|:----------------:|:----------------:|:-----------:|:----------------:|
| **登入畫面** | ? | ? | ? | ? | ? |
| **設備狀態頁** | ? | ? | ? | ? | ? |
| **設備初始化** | ? | ? | ? | ? | ? |
| **啟動製程** | ? | ? | ? | ? | ? |
| **暫停製程** | ? | ? | ? | ? | ? |
| **取消製程** | ? | ? | ? | ? | ? |
| **異常排除** | ? | ? | ? | ? | ? |
| **紀錄檢視** | ? | ? | ? | ? | ? |
| **紀錄匯出** | ? | ? | ? | ? | ? |
| **帳號管理** | ? | ? | ? | ? | ? |
| **製程參數管理** | ? | ? | ? | ? | ? **（專屬）** |
| **工程模式** | ? | ? | ? | ? | ? **（專屬）** |

---

### 4?? **帳號管理權限**

#### **CyberFrame.xaml.cs** - 修正進入條件

```csharp
// ? 修正前（允許 Supervisor 進入）
if (session.CurrentLevel < AccessLevel.Supervisor)

// ? 修正後（只允許 Admin 和 SuperAdmin）
if (session.CurrentLevel < AccessLevel.Admin)
```

#### **UserEditorDialog** - 可管理的權限範圍

| 當前登入者 | 可以創建/編輯的權限等級 |
|-----------|----------------------|
| **SuperAdmin (L5)** | L1 ~ L5（所有等級） |
| **Admin (L4)** | L1 ~ L4（不包含 SuperAdmin） |
| **Supervisor (L3)** | L1 ~ L3 |
| **Instructor (L2)** | L1 |
| **Operator (L1)** | 無 |

---

### 5?? **查看使用者列表的權限**

```csharp
GetManagedUsersAsync(int operatorUserId)
```

| 當前登入者 | 可以查看的使用者 |
|-----------|-----------------|
| **SuperAdmin (L5)** | 所有使用者（包含其他 SuperAdmin） |
| **Admin (L4)** | 所有使用者（不包含 SuperAdmin） |
| **Supervisor (L3)** | L1 ~ L3（不包含 Admin 和 SuperAdmin） |
| **Instructor (L2)** | 只能看自己 |
| **Operator (L1)** | 只能看自己 |

---

### 6?? **AD 群組對應**

如果使用 Windows AD 認證，群組對應如下：

| AD 群組名稱 | 對應權限等級 |
|------------|-------------|
| `App_SuperAdmins` | SuperAdmin (L5) |
| `App_Admins` | Admin (L4) |
| `App_Supervisors` | Supervisor (L3) |
| `App_Instructors` | Instructor (L2) |
| `App_Operators` | Operator (L1) |
| `Domain Admins` | SuperAdmin (L5) |
| `Enterprise Admins` | SuperAdmin (L5) |
| `Administrators` | Admin (L4) |

---

## ?? **修改的檔案清單**

1. ? `Stackdose.UI.Core\Models\AccessLevel.cs`
   - 添加 `SuperAdmin = 5`

2. ? `Stackdose.UI.Core\Services\UserManagementService.cs`
   - 修改 `EnsureDefaultAdminExists()` 添加 SuperAdmin 帳號創建
   - 更新 `GetManagedUsersAsync()` 讓 Admin 看不到 SuperAdmin
   - 更新 `CanManageUser()` 限制 Admin 無法管理 SuperAdmin
   - 更新 `CanDeleteUser()` SuperAdmin 專屬權限
   - 更新 `DetermineAccessLevelFromAdGroups()` 添加 SuperAdmin 群組判斷

3. ? `Stackdose.UI.Core\Controls\CyberFrame.xaml.cs`
   - 修正 `UserManagementToggleButton_Click()` 權限檢查
   - 從 `AccessLevel.Supervisor` 改為 `AccessLevel.Admin`

4. ? `Stackdose.UI.Core\Controls\UserEditorDialog.xaml.cs`
   - 更新 `InitializeForm()` 添加 SuperAdmin 選項
   - SuperAdmin 可以選擇所有等級
   - Admin 只能選擇 L1 ~ L4

---

## ?? **測試步驟**

### 1. **測試 SuperAdmin 登入**

```
帳號：superadmin
密碼：superadminsuperadmin
```

登入後應該：
- ? 可以進入「帳號管理」
- ? 可以看到所有使用者（包含其他 SuperAdmin）
- ? 可以創建任何等級的帳號（L1 ~ L5）
- ? 可以編輯/刪除任何帳號

### 2. **測試 Admin 登入**

```
帳號：admin01
密碼：admin123
```

登入後應該：
- ? 可以進入「帳號管理」
- ? 可以看到所有使用者（**不包含** SuperAdmin）
- ? 可以創建 L1 ~ L4 等級的帳號
- ? **無法創建 SuperAdmin 帳號**
- ? **無法看到/編輯/刪除 SuperAdmin 帳號**

### 3. **測試 Supervisor 登入**

```
需要先用 Admin/SuperAdmin 創建 Supervisor 帳號
```

登入後應該：
- ? **無法進入「帳號管理」**（權限不足）
- ? 可以進行其他 L3 權限操作（取消製程、查看紀錄等）

---

## ?? **重要提醒**

1. **首次啟動時**：
   - 程式會自動創建 `superadmin` 和 `admin01` 兩個帳號
   - 密碼為明文儲存在程式碼中，僅供開發/測試使用

2. **生產環境建議**：
   - 登入後立即修改預設密碼
   - 建議只保留一個 SuperAdmin 帳號
   - 考慮啟用雙因素認證（2FA）

3. **權限設計原則**：
   - 最小權限原則：使用者只獲得完成工作所需的最低權限
   - 職責分離：不同等級負責不同範圍的功能
   - SuperAdmin 專屬：製程參數管理、工程模式

---

## ? **完成狀態**

- ? SuperAdmin 權限定義完成
- ? 預設 SuperAdmin 帳號創建完成（superadmin / superadminsuperadmin）
- ? 帳號管理權限修正完成（只有 Admin 和 SuperAdmin 可進入）
- ? UserEditorDialog 支援 SuperAdmin 選項
- ? 權限檢查邏輯更新完成
- ? AD 群組對應更新完成
- ? 編譯成功

---

## ?? **下一步**

可以開始實作以下功能，限制為 SuperAdmin 專屬：

1. **製程參數管理頁面**
   - 檢查 `session.CurrentLevel >= AccessLevel.SuperAdmin`

2. **工程模式頁面**
   - 檢查 `session.CurrentLevel >= AccessLevel.SuperAdmin`

3. **SecuredButton 更新**
   - 支援 `RequiredLevel="SuperAdmin"`
