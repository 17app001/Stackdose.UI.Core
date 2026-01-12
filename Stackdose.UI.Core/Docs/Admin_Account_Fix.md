# ?? 修正預設管理員帳號 - 從 Admin 改為 admin01

## ?? 修改摘要

預設管理員帳號已從 `Admin` / `admin123` 修改為 `admin01` / `admin01admin01`。

---

## ?? 重要！舊資料庫需要刪除重建

如果您之前已經執行過程式，資料庫中可能已經建立了舊的 `Admin` 帳號。由於資料庫中已有管理員帳號，程式不會自動建立新的 `admin01` 帳號。

### ? 解決方法

**刪除資料庫檔案，讓程式重新建立：**

1. **關閉 WpfApp1 程式**（如果正在執行）
2. **前往執行目錄**：
   ```
   D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\
   ```
3. **刪除檔案**：
   - `StackDoseData.db`
   - `StackDoseData.db-shm`（如果存在）
   - `StackDoseData.db-wal`（如果存在）

4. **重新執行程式**
5. **使用新帳號登入**：
   - 帳號：`admin01`
   - 密碼：`admin01admin01`

---

## ?? 修改過的檔案

### 1. `UserManagementService.cs`
```csharp
// ? 預設帳號已改為 admin01 / admin01admin01
var (hash, salt) = HashPassword("admin01admin01");

conn.Execute(sql, new
{
    UserId = "admin01",
    DisplayName = "System Administrator",
    PasswordHash = hash,
    Salt = salt,
    // ...
    Remarks = "Default admin account (Password: admin01admin01)"
});
```

### 2. `SecurityContext.cs`
```csharp
// ? Fallback 帳號已改為 admin01 / admin01admin01
var defaultAccounts = new Dictionary<string, UserAccount>
{
    ["admin01"] = new UserAccount
    {
        Id = 1,
        UserId = "admin01",
        DisplayName = "系統管理員",
        PasswordHash = HashPassword("admin01admin01"),
        // ...
    }
};
```

### 3. `LoginDialog.xaml.cs`
```csharp
// ? DEBUG 模式預設填入已改為 admin01 / admin01admin01
#if DEBUG
UserIdTextBox.Text = "admin01";
PasswordBox.Password = "admin01admin01";
#endif
```

---

## ?? 測試步驟

1. **刪除舊資料庫**（如上述步驟）
2. **啟動 WpfApp1**
3. **確認登入對話框已預填入**：
   - 使用者：`admin01`
   - 密碼：`admin01admin01`（DEBUG 模式）
4. **按 Enter 或點擊登入**
5. **確認登入成功**並顯示主視窗

---

## ?? 除錯

如果登入失敗，請檢查：

1. **查看日誌檔案**：
   ```
   D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\login_debug.log
   ```

2. **檢查 Debug 輸出**（Visual Studio 的「輸出」視窗）：
   ```
   [UserManagementService] Default Admin created successfully!
   [UserManagementService] Login: admin01 / Password: admin01admin01
   ```

3. **確認資料庫中的帳號**：
   - 使用 DB Browser for SQLite 開啟 `StackDoseData.db`
   - 查詢：`SELECT UserId, DisplayName, AccessLevel FROM Users;`
   - 應該看到 `admin01` 帳號

---

## ?? 注意事項

- **生產環境**：建議管理員登入後立即修改密碼
- **安全性**：`admin01admin01` 僅為測試用途，正式環境請使用強密碼
- **舊帳號**：如果已有其他使用者帳號，刪除資料庫會遺失所有帳號資料

---

## ? 驗證成功

修改完成後，您應該能夠使用 `admin01` / `admin01admin01` 成功登入系統。
