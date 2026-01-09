# Windows 密碼登入快速指南

## ? 最新修改

### 1?? **自動填入 Windows 使用者名稱**
- ? 登入畫面會自動填入當前 Windows 使用者（如 `jerry`）
- ? Focus 自動移到密碼輸入框
- ? 只需輸入 Windows 密碼即可

### 2?? **完整的日誌記錄**
- ? 所有啟動流程都記錄到檔案：`app_startup.log`
- ? 位置：應用程式執行目錄（bin\Debug\net8.0-windows\）
- ? 即使程式自動關閉，日誌檔案仍會保留

### 3?? **錯誤處理**
- ? 攔截所有未處理的例外
- ? 顯示 MessageBox 錯誤訊息
- ? 詳細資訊記錄到日誌檔案

---

## ?? 測試步驟

### **步驟 1：執行應用程式**
1. 按 **F5** 啟動
2. 登入畫面會自動顯示

### **步驟 2：檢查登入畫面**
- 「User ID」欄位應該自動填入您的 Windows 使用者名稱（如 `jerry`）
- 游標應該在「Password」欄位
- 如果沒有自動填入，會顯示 `Admin`

### **步驟 3：輸入密碼**

#### **如果使用者名稱是 Windows 使用者（如 jerry）**
1. 輸入您的 **Windows 登入密碼**
2. 按 Enter 或點擊 Login

#### **如果使用者名稱是 Admin**
1. 輸入 `admin123`（預設密碼）
2. 按 Enter 或點擊 Login

### **步驟 4：檢查結果**

#### ? **成功情況**
- MainWindow 應該正常顯示
- 標題顯示使用者名稱和權限

#### ? **失敗情況（程式自動關閉）**
1. 開啟 `app_startup.log` 檔案
2. 位置：
   ```
   D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\app_startup.log
   ```
3. 查看最後幾行，找出錯誤訊息

---

## ?? 如何查看日誌檔案

### **方式 1：使用檔案總管**
1. 開啟專案目錄
2. 導航到：`WpfApp1\bin\Debug\net8.0-windows\`
3. 找到 `app_startup.log` 並用記事本開啟

### **方式 2：使用 Visual Studio**
1. 在 Visual Studio 中右鍵點擊 `WpfApp1` 專案
2. 選擇「在檔案總管中開啟資料夾」
3. 進入 `bin\Debug\net8.0-windows\`
4. 開啟 `app_startup.log`

### **日誌範例（正常情況）**
```
========================================
[09:15:30.123] Application starting at 2025-01-10 09:15:30
========================================
[09:15:30.234] DEBUG: PLC Simulator enabled
[09:15:30.345] OnStartup: Called
[09:15:30.456] OnStartup: base.OnStartup called
[09:15:30.567] Initializing UserManagementService...
[09:15:30.678] UserManagementService initialized
[09:15:30.789] Initializing ComplianceContext...
[09:15:30.890] ComplianceContext initialized
[09:15:30.901] Showing login dialog...
[09:15:35.123] Login dialog closed - Success: True
[09:15:35.234] Login successful: jerry (Operator)
[09:15:35.345] Creating MainWindow...
[09:15:35.456] MainWindow created, calling Show()...
[09:15:35.567] MainWindow.Show() completed successfully!
========================================
[09:15:35.678] Application startup COMPLETED
========================================
```

### **日誌範例（錯誤情況）**
```
[09:15:35.456] Creating MainWindow...
[09:15:35.567] FATAL ERROR in OnStartup: Cannot find resource named 'Cyber.Bg.Panel'
[09:15:35.678] Exception Type: System.Windows.ResourceReferenceKeyNotFoundException
[09:15:35.789] Stack Trace: at System.Windows...
========================================
```

---

## ? 常見問題

### **問題 1：顯示「帳號未在系統中註冊」**

**原因：**
- Windows 使用者（如 `jerry`）尚未在系統中建立

**解決方式：**
1. 改為輸入 `Admin` / `admin123` 登入
2. 進入「User Management」
3. 點擊「Add User」
4. 選擇「From AD」→ 選擇 `jerry` → 給予權限（如 Operator）
5. 登出並重新登入，使用 Windows 密碼

---

### **問題 2：程式自動關閉，沒有錯誤訊息**

**檢查步驟：**
1. 開啟 `app_startup.log`
2. 找到最後一行日誌
3. 回報最後 10 行內容

**可能原因：**
- MainWindow 建構函數發生例外
- 資源檔案（Colors.xaml）載入失敗
- CyberFrame 元件初始化錯誤

---

### **問題 3：登入畫面沒有自動填入使用者名稱**

**可能原因：**
- 無法取得 Windows 使用者名稱

**檢查：**
1. 開啟 `app_startup.log`
2. 查看是否有 `Failed to get Windows user` 訊息

**Workaround：**
- 手動輸入使用者名稱

---

### **問題 4：輸入 Windows 密碼後顯示「密碼錯誤」**

**可能原因：**
1. Windows 密碼輸入錯誤
2. AD 驗證服務不可用
3. 本地帳號沒有設定備用密碼

**解決方式：**
1. 確認 Windows 密碼正確
2. 如果 AD 不可用，建議先用 `Admin` / `admin123` 登入
3. 在系統中建立該使用者並設定備用密碼

---

## ?? 建議的使用流程

### **第一次使用**
1. 用 `Admin` / `admin123` 登入
2. 進入「User Management」
3. 從 AD 建立您的 Windows 使用者（如 `jerry`）
4. 給予適當權限（Operator / Supervisor）
5. 登出

### **第二次之後**
1. 登入畫面自動填入您的 Windows 使用者名稱
2. 輸入 Windows 密碼
3. 按 Enter 登入

---

## ?? 除錯清單

如果程式仍然自動關閉，請提供以下資訊：

- [ ] `app_startup.log` 的完整內容（最後 20 行）
- [ ] 是否有 MessageBox 錯誤訊息（截圖）
- [ ] 使用的帳號是 `Admin` 還是 Windows 使用者
- [ ] Visual Studio 輸出視窗的內容（如果有）

---

## ? 測試確認

請執行應用程式並回報：

1. **登入畫面是否正確顯示？**
   - [ ] Yes
   - [ ] No

2. **使用者名稱是否自動填入？**
   - [ ] Yes（填入：_______）
   - [ ] No

3. **輸入密碼後按 Enter 的結果？**
   - [ ] MainWindow 正常顯示
   - [ ] 顯示錯誤訊息（內容：_______）
   - [ ] 程式自動關閉

4. **如果程式關閉，`app_startup.log` 最後一行是？**
   - _______________________________

---

預設測試帳號：
- **Admin**: `Admin` / `admin123`
- **Windows 使用者**: `jerry` / (您的 Windows 密碼)

請執行測試並回報結果！??
