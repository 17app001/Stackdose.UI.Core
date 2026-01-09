# 登入畫面啟動流程說明

## ? 已完成的修改

### 1?? **LoginDialog.xaml.cs**
- ? 整合 AD 驗證支援
- ? 改進錯誤訊息顯示
- ? 取消登入時自動關閉應用程式

### 2?? **App.xaml.cs**
- ? 移除 `QuickLogin()`
- ? 在 `OnStartup` 中顯示 `LoginDialog`
- ? 登入成功才允許進入主視窗
- ? 登入失敗/取消則關閉應用程式
- ? 新增 `OnExit` 處理日誌刷新

---

## ?? 啟動流程

```
應用程式啟動
    ↓
初始化資料庫（建立預設 Admin 帳號）
    ↓
顯示 LoginDialog
    ↓
    ├─ 使用者輸入帳號密碼
    │   ↓
    │   嘗試 AD 驗證
    │   ├─ ? AD 驗證成功 → 檢查本地資料庫權限 → 登入成功
    │   └─ ? AD 驗證失敗 → 本地密碼驗證 → 成功/失敗
    │
    ├─ ? 登入成功 → 顯示 MainWindow
    │
    └─ ? 登入失敗/取消 → 關閉應用程式
```

---

## ?? 預設帳號

系統會自動建立預設管理員帳號：

| 欄位 | 值 |
|------|---|
| **User ID** | `Admin` |
| **Password** | `admin123` |
| **Access Level** | Admin (最高權限) |
| **Display Name** | System Administrator |

---

## ?? 測試方式

### **方式 1：使用預設帳號**
1. 執行應用程式
2. 在 LoginDialog 輸入：
   - User ID: `Admin`
   - Password: `admin123`
3. 點擊「Login」或按 Enter
4. 登入成功 → 進入主視窗

### **方式 2：使用 Windows AD 帳號（如 jerry）**

#### 前提條件：
- 該 AD 使用者必須先在「使用者管理」中建立

#### 步驟：
1. 使用 Admin 帳號登入
2. 進入「User Management」介面
3. 點擊「Add User」→ 選擇「From AD」
4. 選擇 AD 使用者（如 `jerry`）
5. 給予權限（如 Operator）
6. 登出並重新啟動
7. 使用 `jerry` 的 Windows 密碼登入

---

## ?? 自訂登入預設值

如果您想在開發時快速測試，可以修改 `LoginDialog.xaml`：

```xaml
<!-- 在 TextBox 中設定預設值 -->
<TextBox x:Name="UserIdTextBox"
         ...
         Text="Admin"/>  <!-- ?? 預設帳號 -->
```

---

## ??? 安全性說明

### ? **生產環境**
- ? 移除 XAML 中的預設 `Text="Admin"`
- ? 強制使用者每次都輸入帳號密碼
- ? 啟用自動登出功能（預設 15 分鐘）

### ? **開發環境**
- ? 可保留預設帳號方便測試
- ? 或使用 `QuickLogin()` 跳過登入（僅限 DEBUG 模式）

---

## ?? 進階設定

### **停用 AD 驗證（僅使用本地密碼）**

在 `App.xaml.cs` 的 `OnStartup` 中新增：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // 停用 AD 驗證
    SecurityContext.EnableAdAuthentication = false;
    
    // ... 其他初始化代碼
}
```

### **開發模式快速登入（跳過 LoginDialog）**

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    #if DEBUG
    // ?? 開發模式：直接使用 Admin 登入
    SecurityContext.QuickLogin(AccessLevel.Admin);
    System.Diagnostics.Debug.WriteLine("[App] DEBUG MODE: QuickLogin as Admin");
    #else
    // ?? 生產模式：顯示登入畫面
    bool loginSuccess = LoginDialog.ShowLoginDialog();
    if (!loginSuccess)
    {
        this.Shutdown();
        return;
    }
    #endif
    
    base.OnStartup(e);
}
```

### **自訂自動登出時間**

```csharp
// 在 App.xaml.cs 中設定
protected override void OnStartup(StartupEventArgs e)
{
    // 設定自動登出時間為 30 分鐘
    SecurityContext.AutoLogoutMinutes = 30;
    
    // 或停用自動登出
    SecurityContext.EnableAutoLogout = false;
    
    // ... 其他初始化代碼
}
```

---

## ?? 疑難排解

### ? 問題 1：無法登入（密碼正確但一直失敗）

**檢查項目：**
1. 確認資料庫是否初始化
   ```
   檢查是否有 StackDoseData.db 檔案
   位置：應用程式執行目錄
   ```

2. 確認 Admin 帳號是否存在
   ```csharp
   // 在 Debug 輸出視窗查看
   [UserManagementService] Default Admin created successfully!
   ```

3. 重新建立資料庫
   ```
   刪除 StackDoseData.db 檔案
   重新啟動應用程式
   ```

---

### ? 問題 2：AD 使用者驗證成功但無法登入

**原因：**
- 該 AD 使用者尚未在本地資料庫建立

**解決方式：**
1. 使用 Admin 帳號登入
2. 進入「User Management」
3. 從 AD 建立該使用者並給予權限

---

### ? 問題 3：取消登入後應用程式不會關閉

**檢查：**
- 確認 `LoginDialog.xaml.cs` 中 `CancelButton_Click` 有呼叫 `Application.Current.Shutdown()`

```csharp
private void CancelButton_Click(object? sender, RoutedEventArgs? e)
{
    LoginSuccessful = false;
    this.DialogResult = false;
    Application.Current.Shutdown(); // ?? 必須有這行
}
```

---

## ?? 審計軌跡

所有登入相關操作都會記錄到 `AuditTrails` 資料表：

| 事件 | 記錄內容 |
|------|----------|
| 登入成功 | `Login from {MachineName} via {AD/Local}` |
| 登入失敗 | `Failed (User not found / Wrong password / Account inactive)` |
| 登入取消 | `Application startup cancelled (Login failed or cancelled)` |
| 登出 | `Logout: {DisplayName} (Manual/Auto-Logout)` |

---

## ? 完成檢查清單

- [x] LoginDialog 整合 AD 驗證
- [x] App.xaml.cs 移除 QuickLogin
- [x] App.xaml.cs 新增 LoginDialog 顯示邏輯
- [x] 登入失敗/取消時關閉應用程式
- [x] 登入成功記錄到審計軌跡
- [x] 建置測試通過
- [x] 使用說明文件完成

---

## ?? 完成！

現在您的應用程式會在啟動時先顯示登入畫面，只有登入成功才能進入主視窗。

**預設帳號：**
- User ID: `Admin`
- Password: `admin123`

祝您使用愉快！??
