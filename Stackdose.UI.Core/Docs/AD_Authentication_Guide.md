# Windows AD 整合說明文件

## ?? 功能概述

本系統已整合 **Windows Active Directory (AD) 身份驗證**，支援以下功能：

### 1?? **雙重驗證模式**
- ? **優先使用 Windows AD 驗證**（透過網域帳號密碼）
- ? **Fallback 到本地密碼**（當 AD 不可用時）
- ? **權限由本地資料庫管理**（新增/刪除/修改權限）

### 2?? **自動同步使用者資訊**
- 從 AD 自動取得：顯示名稱、Email、部門等資訊
- 登入時自動更新本地使用者資料

### 3?? **管理員功能**
- 從 AD 快速建立使用者
- 查看本機所有 AD 使用者清單
- 檢查 AD 使用者是否已註冊

---

## ?? 使用方式

### ?? **方式 1：使用者登入（一般使用）**

```csharp
// 在 LoginDialog 或 MainWindow 中
using Stackdose.UI.Core.Helpers;

// 使用者輸入帳號密碼
string username = "jerry";  // Windows AD 使用者名稱
string password = "user_password";

// 登入（會自動嘗試 AD 驗證 → 本地密碼驗證）
bool success = SecurityContext.Login(username, password);

if (success)
{
    MessageBox.Show($"歡迎，{SecurityContext.CurrentSession.CurrentUserName}！");
}
else
{
    MessageBox.Show("登入失敗！請檢查帳號密碼。");
}
```

**登入流程說明：**
```
1. 系統優先使用 Windows AD 驗證帳號密碼
   ├─ ? AD 驗證成功 → 檢查本地資料庫是否有該使用者
   │   ├─ ? 有 → 登入成功，套用本地權限
   │   └─ ? 無 → 提示需要管理員建立帳號
   │
   └─ ? AD 驗證失敗 → 使用本地資料庫密碼驗證
       ├─ ? 本地密碼正確 → 登入成功
       └─ ? 本地密碼錯誤 → 登入失敗
```

---

### ?? **方式 2：管理員建立 AD 使用者**

```csharp
using Stackdose.UI.Core.Services;

var userService = new UserManagementService();

// 1?? 取得本機所有 AD 使用者（顯示在下拉選單）
var adUsers = userService.GetAvailableAdUsers();
// 結果：["Administrator", "Guest", "jerry", "WDAGUtilityAccount", ...]

// 2?? 檢查使用者是否已註冊
bool isRegistered = await userService.IsAdUserRegisteredAsync("jerry");

// 3?? 從 AD 建立使用者（給予權限）
if (!isRegistered)
{
    var (success, message, user) = await userService.CreateUserFromAdAsync(
        adUsername: "jerry",
        accessLevel: AccessLevel.Operator,  // 給予操作員權限
        creatorUserId: SecurityContext.CurrentSession.CurrentUser.Id,
        defaultPassword: null  // 可選，用於 AD 不可用時的 fallback
    );

    if (success)
    {
        MessageBox.Show($"成功建立使用者：{user.DisplayName}");
    }
    else
    {
        MessageBox.Show($"建立失敗：{message}");
    }
}
```

---

### ?? **方式 3：取得當前 Windows 使用者**

```csharp
using Stackdose.UI.Core.Services;

// 取得當前 Windows 登入使用者（不含 Domain）
string currentUser = AdAuthenticationService.GetCurrentWindowsUser();
// 結果：jerry

// 取得完整名稱（含 Domain）
string fullName = AdAuthenticationService.GetCurrentWindowsUserWithDomain();
// 結果：DESKTOP-ABC123\jerry
```

---

### ?? **方式 4：啟用/停用 AD 驗證**

```csharp
using Stackdose.UI.Core.Helpers;

// 停用 AD 驗證（只使用本地密碼）
SecurityContext.EnableAdAuthentication = false;

// 啟用 AD 驗證（預設）
SecurityContext.EnableAdAuthentication = true;
```

---

## ?? 資料庫結構

### Users 資料表

| 欄位 | 說明 | AD 整合 |
|------|------|---------|
| `UserId` | 使用者帳號 | ? 與 AD 使用者名稱一致 |
| `DisplayName` | 顯示名稱 | ? 從 AD 自動同步 |
| `Email` | Email | ? 從 AD 自動同步 |
| `PasswordHash` | 密碼雜湊 | ?? 用於 AD 不可用時的 fallback |
| `Salt` | 密碼鹽值 | ?? 用於 AD 不可用時的 fallback |
| `AccessLevel` | 權限等級 | ? 由本地資料庫管理 |
| `IsActive` | 是否啟用 | ? 由本地資料庫管理 |
| `Department` | 部門 | ? 從 AD Description 欄位同步 |
| `Remarks` | 備註 | ? 標記 "Created from Windows AD" |

---

## ?? 疑難排解

### ? 問題 1：AD 驗證失敗

**可能原因：**
- 電腦未加入 Domain
- AD 服務不可用
- 使用者名稱或密碼錯誤

**解決方式：**
1. 檢查 Debug 輸出視窗的錯誤訊息
2. 確認電腦是否加入 Domain：
   ```bash
   systeminfo | findstr /B /C:"Domain"
   ```
3. 停用 AD 驗證，使用本地密碼：
   ```csharp
   SecurityContext.EnableAdAuthentication = false;
   ```

---

### ? 問題 2：AD 驗證成功但無法登入

**可能原因：**
- 該 AD 使用者尚未在本地資料庫建立

**解決方式：**
1. 使用管理員帳號登入
2. 在「使用者管理」介面建立該 AD 使用者
3. 給予適當的權限等級

---

### ? 問題 3：需要測試 AD 驗證

**測試方式：**
```csharp
var adService = new AdAuthenticationService();

// 1. 檢查 AD 是否可用
bool isAvailable = adService.IsAvailable();
Console.WriteLine($"AD Available: {isAvailable}");

// 2. 測試驗證
bool isValid = adService.ValidateCredentials("jerry", "password123");
Console.WriteLine($"Validation Result: {isValid}");

// 3. 取得使用者資訊
var userInfo = adService.GetUserInfo("jerry");
if (userInfo != null)
{
    Console.WriteLine($"DisplayName: {userInfo.DisplayName}");
    Console.WriteLine($"Email: {userInfo.Email}");
}
```

---

## ?? 審計軌跡

所有 AD 相關操作都會記錄到 `AuditTrails` 資料表：

| 事件 | 記錄內容 |
|------|----------|
| AD 驗證成功 | `Login from {MachineName} via Windows AD` |
| AD 驗證失敗但本地密碼成功 | `Login from {MachineName} via Local Database` |
| 從 AD 建立使用者 | `從 AD 建立使用者: {DisplayName} ({AccessLevel})` |
| AD 驗證成功但未建立帳號 | `Failed (Not in local database)` |

---

## ?? 安全性說明

1. **密碼不會儲存在本地**
   - AD 驗證時，密碼直接傳送給 Windows AD 驗證
   - 本地只儲存備用密碼（用於 AD 不可用時）

2. **權限由本地管理**
   - AD 只負責身份驗證
   - 權限等級（Admin/Supervisor/Operator...）由本地資料庫管理
   - 管理員可隨時調整使用者權限

3. **雙重驗證機制**
   - 即使 AD 服務中斷，仍可使用本地密碼登入
   - 確保系統持續運作

---

## ?? 開發者備註

### NuGet 套件
```xml
<PackageReference Include="System.DirectoryServices.AccountManagement" Version="8.0.0" />
```

### 關鍵類別
- `AdAuthenticationService` - AD 驗證核心
- `SecurityContext` - 登入驗證邏輯
- `UserManagementService` - 使用者管理服務

### 設定檔案
- `Stackdose.UI.Core\Services\AdAuthenticationService.cs`
- `Stackdose.UI.Core\Helpers\SecurityContext.cs`
- `Stackdose.UI.Core\Services\UserManagementService.cs`

---

## ?? 使用場景

### 場景 1：新使用者首次登入
1. 使用者輸入 Windows AD 帳號密碼
2. AD 驗證成功，但本地資料庫無此帳號
3. 系統提示：「請聯絡管理員建立帳號」
4. 管理員登入 → 使用「從 AD 建立使用者」功能
5. 給予該使用者適當權限（如 Operator）
6. 使用者可以正常登入

### 場景 2：AD 服務中斷
1. 使用者輸入帳號密碼
2. AD 驗證失敗（網路問題）
3. 系統自動使用本地密碼驗證
4. 驗證成功，登入系統

### 場景 3：權限調整
1. 管理員登入系統
2. 進入「使用者管理」介面
3. 選擇使用者 → 點擊「編輯」
4. 調整權限等級（如從 Operator 升級為 Supervisor）
5. 下次該使用者登入時，自動套用新權限

---

## ? 完成事項

- [x] 建立 `AdAuthenticationService` 類別
- [x] 整合 AD 驗證到 `SecurityContext.Login`
- [x] 新增 `CreateUserFromAdAsync` 方法
- [x] 新增 `GetAvailableAdUsers` 方法
- [x] 新增 `IsAdUserRegisteredAsync` 方法
- [x] 支援自動同步 AD 使用者資訊
- [x] 記錄審計軌跡
- [x] 建置測試通過

---

## ?? 技術支援

如有任何問題，請聯絡開發團隊或參考：
- Windows AD 官方文件：https://docs.microsoft.com/en-us/windows-server/identity/ad-ds/
- .NET DirectoryServices 文件：https://docs.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement
