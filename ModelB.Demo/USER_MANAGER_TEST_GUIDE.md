# UserManager 創建使用者測試指南

## ?? 測試目的

完整驗證 `UserManagementService.CreateUserAsync` 方法是否正常運作，並找出「找不到創建資訊」錯誤的根本原因。

---

## ?? 執行測試程式

### 方法 1：從 Visual Studio 啟動（推薦）

1. **開啟 Visual Studio**
2. **設定專案屬性**：
   - 右鍵點擊 `ModelB.Demo` 專案
   - 選擇「屬性」
   - 在「偵錯」頁面
   - 在「應用程式引數」欄位輸入：`/test`
   - 按 F5 啟動

3. **或者直接修改啟動設定**：
   - 開啟 `Properties/launchSettings.json`
   - 加入：
   ```json
   {
     "profiles": {
       "ModelB.Demo (Test)": {
         "commandName": "Project",
         "commandLineArgs": "/test"
       }
     }
   }
   ```

### 方法 2：從命令列啟動

```powershell
cd D:\工作區\Project\Stackdose.UI.Core\ModelB.Demo\bin\Debug\net8.0-windows
.\ModelB.Demo.exe /test
```

---

## ?? 測試步驟

### 1. 視窗啟動

啟動後會看到：
- 標題：「UserManager Test Program」
- 輸出區域：黑色背景的日誌視窗
- 按鈕：「Test Create User」（藍色）、「Clear Output」（橙色）、「Close」（灰色）

### 2. 自動初始化檢查

程式會自動執行以下檢查：

```
[1/6] 初始化 ComplianceContext...
? ComplianceContext 初始化成功

[2/6] 初始化 UserManagementService...
? UserManagementService 初始化成功

[3/6] 檢查預設 Admin 帳號...
   資料庫中的使用者數量: X
? 找到預設 Admin 帳號:
   UserId: admin01
   DisplayName: System Administrator
   AccessLevel: Admin
   IsActive: True
   CreatedBy: System

[4/6] 測試登入 admin01...
? 找到 admin01 帳號:
   UserId: admin01
   DisplayName: System Administrator
   AccessLevel: Admin

[5/6] 顯示測試按鈕...
? 測試環境準備完成

?? 請點擊「Test Create User」按鈕
```

### 3. 點擊測試按鈕

點擊「Test Create User」後，程式會執行以下步驟：

```
[Step 1] 取得 admin01 使用者資訊...
? 找到 admin01:
   Id: 1
   UserId: admin01
   DisplayName: System Administrator

[Step 2] 創建測試使用者 test01...

========================================
??? 創建成功！???
========================================
   UserId: test01
   DisplayName: Test User 01
   AccessLevel: Operator
   Email: test01@example.com
   Department: 測試部門
   CreatedBy: System Administrator
   CreatedAt: 2025-01-14 15:30:25
========================================

[Step 3] 驗證使用者是否存在於資料庫...
? 驗證成功：使用者已存在於資料庫
   資料庫中的 UserId: test01
   資料庫中的 DisplayName: Test User 01
```

---

## ? 可能的錯誤情況

### 錯誤 1：找不到 admin01

```
? 錯誤：找不到 admin01 使用者
   解決方法：
   1. 確認 UserManagementService 的 EnsureDefaultAdminExists() 有執行
   2. 檢查資料庫檔案權限
   3. 查看 Debug 輸出中的錯誤訊息
```

**解決步驟**：
1. 刪除 `StackDoseData.db` 檔案
2. 重新啟動測試程式（會自動重建資料庫）

---

### 錯誤 2：創建失敗 - 找不到創建者資訊

```
========================================
??? 創建失敗！???
========================================
   錯誤訊息: 找不到創建者資訊
========================================

?? 錯誤分析:
   可能原因：
   1. creatorUserId 無效
   2. admin01 帳號未正確建立
   3. 資料庫連線問題
```

**解決步驟**：
1. 檢查資料庫檔案位置：
   ```
   D:\工作區\Project\Stackdose.UI.Core\ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
   ```

2. 使用 [DB Browser for SQLite](https://sqlitebrowser.org/) 開啟資料庫

3. 檢查 `Users` 表格：
   - 是否存在 `admin01` 使用者
   - `Id` 欄位是否為 1
   - `AccessLevel` 是否為 4 (Admin)
   - `IsActive` 是否為 1

---

### 錯誤 3：使用者已存在

```
========================================
??? 創建失敗！???
========================================
   錯誤訊息: 使用者 ID 'test01' 已存在
========================================

?? 錯誤分析:
   可能原因：
   1. test01 使用者已經存在
   2. 請嘗試使用不同的 UserId
```

**解決方法**：
這是正常的，表示之前已經成功創建過 test01 使用者了！

可以：
1. 刪除資料庫檔案重新測試
2. 或修改測試程式使用不同的 UserId（test02, test03 等）

---

## ?? 測試結果分析

### ? 成功的測試結果

如果看到以下輸出，代表 UserManager 正常運作：

1. ? 預設 Admin 帳號已建立
2. ? creatorUserId 正確取得
3. ? 創建使用者成功
4. ? 資料庫驗證通過

**結論**：`UserManagementService.CreateUserAsync` 方法運作正常！

---

### ? 失敗的測試結果

如果看到「找不到創建者資訊」錯誤：

1. 檢查 Debug 輸出視窗（Visual Studio）
2. 查找以下訊息：
   ```
   [UserManagementService] Default admin account created: admin01 / admin123
   ```

3. 如果沒有此訊息，表示 `EnsureDefaultAdminExists()` 沒有執行

**可能原因**：
- `UserManagementService` 建構函數中的 `EnsureDefaultAdminExists()` 被註解掉
- 資料庫檔案權限問題
- SQLite 連線錯誤

---

## ?? 手動測試（不使用測試程式）

如果測試程式無法運作，可以手動在 ModelB.Demo 中測試：

### 1. 在 App.xaml.cs 的 OnStartup 加入測試程式碼：

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // 初始化
    var _ = ComplianceContext.CurrentUser;
    var userService = new UserManagementService();

    // 測試創建使用者
    try
    {
        // 取得 admin01
        var users = await userService.GetAllUsersAsync();
        var admin = users.FirstOrDefault(u => u.UserId == "admin01");

        if (admin == null)
        {
            MessageBox.Show("找不到 admin01！", "錯誤");
            Application.Current.Shutdown();
            return;
        }

        // 創建 test01
        var result = await userService.CreateUserAsync(
            "test01",
            "Test User",
            "test123",
            AccessLevel.Operator,
            admin.Id,
            "test@example.com",
            "Test",
            "Test User"
        );

        if (result.Success)
        {
            MessageBox.Show("? 創建成功！", "成功");
        }
        else
        {
            MessageBox.Show($"? 創建失敗：{result.Message}", "錯誤");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"例外錯誤：{ex.Message}", "錯誤");
    }

    // 正常啟動
    ShowSplashScreen();
}
```

---

## ?? 資料庫檢查

### 使用 DB Browser for SQLite

1. **下載並安裝** [DB Browser for SQLite](https://sqlitebrowser.org/)

2. **開啟資料庫檔案**：
   ```
   D:\工作區\Project\Stackdose.UI.Core\ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
   ```

3. **檢查 Users 表格**：
   - 點擊「Browse Data」
   - 選擇「Users」表格
   - 檢查是否有 `admin01` 使用者

4. **執行 SQL 查詢**：
   ```sql
   -- 查看所有使用者
   SELECT * FROM Users;

   -- 查看 Admin 帳號
   SELECT * FROM Users WHERE AccessLevel = 4;

   -- 檢查 admin01
   SELECT * FROM Users WHERE UserId = 'admin01';
   ```

---

## ?? 預期結果

### 成功的資料庫狀態

執行測試後，`Users` 表格應該包含：

| Id | UserId | DisplayName | AccessLevel | IsActive | CreatedBy |
|----|--------|-------------|-------------|----------|-----------|
| 1  | admin01 | System Administrator | 4 (Admin) | 1 | System |
| 2  | test01 | Test User 01 | 1 (Operator) | 1 | System Administrator |

---

## ?? 除錯指引

### Debug 輸出視窗關鍵訊息

在 Visual Studio 的「輸出」視窗中查找：

```
[ComplianceContext] 合規引擎已啟動（批次寫入模式）
[UserManagementService] Initialized with default admin
[UserManagementService] Default admin account created: admin01 / admin123
[UserManagerTest] ========== UserManager 創建測試開始 ==========
[UserManagerTest] [1/6] 初始化 ComplianceContext...
[UserManagerTest] ? ComplianceContext 初始化成功
```

---

## ? 最終確認清單

測試完成後確認：

- [ ] 測試程式成功啟動
- [ ] admin01 帳號已建立
- [ ] test01 使用者創建成功
- [ ] 資料庫驗證通過
- [ ] 沒有錯誤訊息

如果全部打勾，表示 **UserManager 創建功能正常運作**！

---

## ?? 如果仍然失敗

請提供以下資訊：

1. **測試程式的完整輸出**（截圖或複製文字）
2. **Debug 輸出視窗的內容**（Visual Studio → 檢視 → 輸出 → 顯示輸出來源: 偵錯）
3. **資料庫檔案的狀態**（使用 DB Browser 檢查 Users 表格）
4. **錯誤訊息的完整內容**

這樣我可以更精確地找出問題所在！??
