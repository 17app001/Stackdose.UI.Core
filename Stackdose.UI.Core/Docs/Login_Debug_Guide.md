# 登入畫面問題偵錯指南

## ?? 問題：登入後程式卡住或自動關閉

### 已修復的問題

#### 1?? **CancelButton_Click 導致應用程式關閉**
**問題：**
- 原本的 `CancelButton_Click` 中有 `Application.Current.Shutdown()`
- 這會導致取消登入時整個應用程式關閉（正確）
- 但也可能影響其他流程

**修復：**
```csharp
private void CancelButton_Click(object? sender, RoutedEventArgs? e)
{
    LoginSuccessful = false;
    this.DialogResult = false;
    this.Close(); // 只關閉對話框，讓 App.xaml.cs 處理後續
}
```

#### 2?? **新增完整的 Debug 日誌**
現在可以在「輸出」視窗（Debug）中看到完整的執行流程：

```
========================================
[App.OnStartup] Application starting...
========================================
[App.OnStartup] Initializing UserManagementService...
[App.OnStartup] UserManagementService initialized successfully
[App.OnStartup] Initializing ComplianceContext...
[App.OnStartup] Showing login dialog...
[LoginDialog.ShowLoginDialog] Creating dialog...
[LoginDialog] Constructor called
[LoginDialog] Window loaded
[LoginDialog] LoginButton_Click called
[LoginDialog] Login successful for: System Administrator (Admin)
[LoginDialog] Setting DialogResult = true
[LoginDialog] Closing dialog
[App.OnStartup] Login successful!
[App.OnStartup] Creating MainWindow...
[App.OnStartup] MainWindow shown successfully
========================================
```

---

## ?? 偵錯步驟

### **步驟 1：開啟輸出視窗**
1. 在 Visual Studio 中按 **Ctrl + W, O**
2. 或選擇「檢視 → 輸出」
3. 確認下拉選單選擇「顯示輸出來源：偵錯」

### **步驟 2：執行應用程式（Debug 模式）**
1. 按 **F5** 或點擊「開始偵錯」
2. 觀察輸出視窗的日誌

### **步驟 3：分析日誌**

#### ? **正常流程（登入成功）**
```
[App.OnStartup] Application starting...
[App.OnStartup] Showing login dialog...
[LoginDialog] LoginButton_Click called
[LoginDialog] Login successful for: Admin (Admin)
[LoginDialog] Setting DialogResult = true
[LoginDialog] Closing dialog
[App.OnStartup] Login successful!
[App.OnStartup] Creating MainWindow...
[App.OnStartup] MainWindow shown successfully
```

#### ? **異常流程 1：登入失敗**
```
[LoginDialog] LoginButton_Click called
[LoginDialog] Calling SecurityContext.Login...
[LoginDialog] Login result: False
[LoginDialog] Login failed
[LoginDialog] Show Error: 登入失敗 Login Failed...
```
**原因：** 帳號密碼錯誤

#### ? **異常流程 2：取消登入**
```
[LoginDialog] CancelButton_Click called
[LoginDialog] User cancelled login, closing dialog
[App.OnStartup] Login cancelled or failed. Shutting down...
[App.OnStartup] Calling Shutdown()...
[App.OnExit] Application shutting down...
```
**原因：** 使用者點擊 Cancel 按鈕

#### ? **異常流程 3：登入成功但 MainWindow 未顯示**
```
[App.OnStartup] Login successful!
[App.OnStartup] Creating MainWindow...
[App.OnStartup] FATAL ERROR: ...
```
**可能原因：**
- MainWindow 建構函數發生錯誤
- XAML 載入失敗
- 資源檔案遺失

---

## ?? 常見問題排除

### ? 問題 1：登入後程式立即關閉

**檢查：**
1. 查看輸出視窗是否有 `[App.OnStartup] FATAL ERROR`
2. 檢查 MainWindow.xaml 是否有語法錯誤
3. 檢查主題資源是否正確載入

**解決方式：**
```csharp
// 在 App.xaml.cs 的 OnStartup 中已經加入 try-catch
// 任何錯誤都會顯示 MessageBox
```

---

### ? 問題 2：登入畫面卡住不動

**檢查：**
1. 查看輸出視窗是否有錯誤訊息
2. 檢查資料庫初始化是否成功
3. 檢查 SecurityContext.Login 是否有例外

**可能原因：**
- 資料庫檔案鎖定
- AD 驗證超時
- 密碼驗證邏輯錯誤

**測試方式：**
```csharp
// 在 LoginButton_Click 中設定中斷點
// 逐步執行觀察哪一行卡住
```

---

### ? 問題 3：輸出視窗沒有任何日誌

**原因：**
- 可能是 Release 模式執行（DEBUG 符號未定義）

**解決方式：**
1. 確認建置設定為 **Debug**
2. 在 Visual Studio 中：
   - 工具列選擇「Debug」
   - 或右鍵專案 → 屬性 → 建置 → 設定 → Debug

---

### ? 問題 4：Admin 帳號密碼錯誤

**預設帳號：**
- User ID: `Admin`
- Password: `admin123`

**重置資料庫：**
1. 關閉應用程式
2. 刪除 `StackDoseData.db` 檔案（位於執行目錄）
3. 重新啟動應用程式（會自動重建）

---

## ?? 測試流程

### **測試 1：正常登入**
1. 執行應用程式
2. 輸入 Admin / admin123
3. 按 Enter 或點擊 Login
4. 應該看到 MainWindow

### **測試 2：錯誤密碼**
1. 執行應用程式
2. 輸入 Admin / wrong_password
3. 應該看到錯誤訊息
4. 清空密碼，重新輸入正確密碼
5. 應該可以登入

### **測試 3：取消登入**
1. 執行應用程式
2. 點擊 Cancel 按鈕
3. 應用程式應該關閉

---

## ?? 效能監控

### **正常啟動時間**
```
[App.OnStartup] Application starting... (0ms)
[App.OnStartup] Showing login dialog... (100-300ms)
[LoginDialog] Login successful... (50-200ms)
[App.OnStartup] MainWindow shown successfully (100-500ms)
```
**總時間：約 250-1000ms**

### **異常情況**
如果超過 5 秒還沒看到 MainWindow：
1. 檢查輸出視窗最後一行日誌
2. 可能卡在某個初始化步驟
3. 中斷執行，查看呼叫堆疊

---

## ?? 開發模式快速登入

如果想在開發時跳過登入畫面：

```csharp
// 在 App.xaml.cs 的 OnStartup 中
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    #if DEBUG
    // ?? 開發模式：直接使用 Admin 登入
    SecurityContext.QuickLogin(AccessLevel.Admin);
    System.Diagnostics.Debug.WriteLine("[App] DEBUG MODE: QuickLogin as Admin");
    
    var mainWindow = new MainWindow();
    mainWindow.Show();
    return; // 跳過登入對話框
    #endif

    // ... 正常登入流程
}
```

---

## ? 確認清單

- [x] CancelButton 不會意外關閉應用程式
- [x] 加入完整 Debug 日誌
- [x] 加入錯誤處理和 MessageBox 顯示
- [x] 登入成功後正確建立 MainWindow
- [x] 建置測試通過

---

## ?? 下一步

1. **執行應用程式（F5）**
2. **開啟輸出視窗（Ctrl + W, O）**
3. **觀察日誌流程**
4. **如果有問題，截圖輸出視窗回報**

預設帳號：
- User ID: `Admin`
- Password: `admin123`

祝您偵錯順利！??
