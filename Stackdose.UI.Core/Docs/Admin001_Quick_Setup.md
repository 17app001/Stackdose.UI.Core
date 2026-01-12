# ?? 快速設定 admin001 帳號並登入 WpfApp1

## ?? 重要提醒

**WpfApp1 使用純 Windows AD 驗證**，這表示：
- ? 需要在 Windows 中建立 `admin001` 本機帳號
- ? 需要將 `admin001` 加入 `App_Admins` 群組
- ? 不需要在資料庫中建立帳號

---

## ?? 快速設定步驟

### 1?? 開啟 PowerShell（管理員模式）

右鍵「開始」→「Windows PowerShell (系統管理員)」

---

### 2?? 建立 admin001 使用者

```powershell
# 建立使用者 admin001，密碼為 admin001admin001
net user admin001 admin001admin001 /add

# 設定密碼永不過期（選用）
wmic useraccount where "name='admin001'" set PasswordExpires=false

# 確認建立成功
net user admin001
```

**預期輸出：**
```
命令執行成功。

使用者名稱                admin001
全名
註解
使用者的註解
國家/地區代碼            000 (系統預設值)
...
```

---

### 3?? 建立 App_Admins 群組（如果還沒有）

```powershell
# 建立群組
net localgroup App_Admins /add

# 確認建立成功
net localgroup App_Admins
```

**預期輸出：**
```
別名     App_Admins
註解

成員
------------------------------------------------------------------------
命令執行成功。
```

---

### 4?? 將 admin001 加入 App_Admins 群組

```powershell
# 加入群組
net localgroup App_Admins admin001 /add

# 確認成功
net localgroup App_Admins
```

**預期輸出：**
```
別名     App_Admins
註解

成員
------------------------------------------------------------------------
admin001
命令執行成功。
```

---

### 5?? 測試登入 WpfApp1

1. **啟動 WpfApp1.exe**
2. **登入對話框應該已預填**：
   - 使用者：`admin001`（DEBUG 模式自動填入）
   - 密碼：（請手動輸入）`admin001admin001`
3. **按 Enter 或點擊「登入」**
4. **確認登入成功**

---

## ? 成功登入後應該看到

- ? 主視窗開啟
- ? 左上角顯示：`admin001 (Admin)` 或 `System Administrator (Admin)`
- ? 日誌顯示：
  ```
  [OK] Login Success: admin001 (Admin) via Windows AD
  ```

---

## ?? 如果登入失敗

### 檢查清單：

#### 1. 確認 Windows 帳號存在
```powershell
net user admin001
```

#### 2. 確認群組成員正確
```powershell
net localgroup App_Admins
```
應該看到 `admin001` 在成員清單中。

#### 3. 測試密碼是否正確
```powershell
# 嘗試以 admin001 身份執行命令
runas /user:admin001 cmd
```
輸入密碼 `admin001admin001`，如果能開啟 cmd，表示密碼正確。

#### 4. 查看登入日誌
開啟檔案：
```
D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\login_debug.log
```

搜尋關鍵字：
- `?` → 登入成功
- `?` → 登入失敗（查看原因）

#### 5. 檢查 Visual Studio Debug 輸出
在「輸出」視窗中搜尋：
```
[SecurityContext] Login START: admin001
[SecurityContext] ? AD Authentication SUCCESS: admin001
[SecurityContext] ? AccessLevel determined: Admin
[SecurityContext] ? Login COMPLETED
```

---

## ??? 常見錯誤

### ? 錯誤 1：密碼錯誤
```
? AD Authentication FAILED: admin001
   Error: Invalid credentials
```

**解決方法**：
```powershell
# 重設密碼
net user admin001 admin001admin001
```

---

### ? 錯誤 2：不屬於 App_Admins 群組
```
? Login Failed: User is not in any App_ group
   User groups: BUILTIN\Users
```

**解決方法**：
```powershell
# 加入群組
net localgroup App_Admins admin001 /add
```

---

### ? 錯誤 3：AD 驗證超時
```
? AD Authentication Error: The operation has timed out
```

**解決方法**：
1. 確認是本機帳號（不是網域帳號）
2. 檢查 `SecurityContext.UseLocalMachineOnly = true`

---

## ?? 完整設定確認

執行以下命令，確認設定正確：

```powershell
# 1. 確認使用者存在
net user admin001
Write-Host "? User admin001 exists" -ForegroundColor Green

# 2. 確認群組存在
net localgroup App_Admins
Write-Host "? Group App_Admins exists" -ForegroundColor Green

# 3. 確認成員關係
$members = net localgroup App_Admins | Select-String "admin001"
if ($members) {
    Write-Host "? admin001 is member of App_Admins" -ForegroundColor Green
} else {
    Write-Host "? admin001 is NOT member of App_Admins" -ForegroundColor Red
}
```

---

## ?? 一鍵設定腳本（全自動）

複製以下腳本，貼到 PowerShell（管理員模式）：

```powershell
# ======================================
# WpfApp1 admin001 快速設定腳本
# ======================================

Write-Host "開始設定 admin001 帳號..." -ForegroundColor Cyan

# 1. 建立使用者
Write-Host "`n[1/3] 建立使用者 admin001..." -ForegroundColor Yellow
net user admin001 admin001admin001 /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? 使用者 admin001 已存在或建立成功" -ForegroundColor Green
} else {
    Write-Host "? 建立使用者失敗" -ForegroundColor Red
    exit
}

# 2. 設定密碼永不過期
Write-Host "`n[2/3] 設定密碼永不過期..." -ForegroundColor Yellow
wmic useraccount where "name='admin001'" set PasswordExpires=false 2>$null
Write-Host "? 密碼設定完成" -ForegroundColor Green

# 3. 建立群組（如果不存在）
Write-Host "`n[3/3] 建立 App_Admins 群組..." -ForegroundColor Yellow
net localgroup App_Admins /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? 群組 App_Admins 已存在或建立成功" -ForegroundColor Green
}

# 4. 加入群組
Write-Host "`n[4/3] 將 admin001 加入 App_Admins..." -ForegroundColor Yellow
net localgroup App_Admins admin001 /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? admin001 已加入 App_Admins" -ForegroundColor Green
}

# 5. 確認設定
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "設定完成！請使用以下帳號登入：" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "使用者名稱：admin001" -ForegroundColor White
Write-Host "密碼：admin001admin001" -ForegroundColor White
Write-Host "權限等級：Admin (L4)" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

# 6. 顯示群組成員
Write-Host "`n目前 App_Admins 群組成員：" -ForegroundColor Yellow
net localgroup App_Admins

Write-Host "`n? 現在可以啟動 WpfApp1 並登入了！" -ForegroundColor Green
```

---

## ?? 完成！

設定完成後：
1. **啟動 WpfApp1**
2. **使用者欄位應該已預填 `admin001`**（DEBUG 模式）
3. **輸入密碼 `admin001admin001`**
4. **按 Enter 登入**
5. **享受 Admin 權限！** ??

---

## ?? 備註

- **RELEASE 模式**：會自動填入當前 Windows 使用者，而非 admin001
- **密碼安全**：建議正式環境使用更強的密碼
- **多使用者**：可以建立多個 Windows 帳號並加入不同群組，測試權限控制
