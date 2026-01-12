# ?? 快速設定 admin01 帳號並登入 WpfApp1

## ?? 重要提醒

**WpfApp1 使用純 Windows AD 驗證**，這表示：
- ? 需要在 Windows 中建立 `admin01` 本機帳號
- ? 需要將 `admin01` 加入 `App_Admins` 群組
- ? 不需要在資料庫中建立帳號
- ? **所有使用者資訊來自 Windows AD，非本地資料庫**

---

## ?? 快速設定步驟

### 1?? 開啟 PowerShell（管理員模式）

右鍵「開始」→「Windows PowerShell (系統管理員)」

---

### 2?? 建立 admin01 使用者

```powershell
# 建立使用者 admin01，密碼為 admin01admin01
net user admin01 admin01admin01 /add

# 設定密碼永不過期（選用）
wmic useraccount where "name='admin01'" set PasswordExpires=false

# 確認建立成功
net user admin01
```

**預期輸出：**
```
命令執行成功。

使用者名稱                admin01
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

### 4?? 將 admin01 加入 App_Admins 群組

```powershell
# 加入群組
net localgroup App_Admins admin01 /add

# 確認成功
net localgroup App_Admins
```

**預期輸出：**
```
別名     App_Admins
註解

成員
------------------------------------------------------------------------
admin01
命令執行成功。
```

---

### 5?? 測試登入 WpfApp1

1. **啟動 WpfApp1.exe**
2. **登入對話框應該已預填**：
   - 使用者：`admin01`（DEBUG 模式自動填入）
   - 密碼：（請手動輸入）`admin01admin01`
3. **按 Enter 或點擊「登入」**
4. **確認登入成功**

---

## ? 成功登入後應該看到

- ? 主視窗開啟
- ? 左上角顯示：`admin01 (Admin)` 或類似資訊
- ? 日誌顯示：
  ```
  [OK] Login Success: admin01 (Admin) via Windows AD
  ```
- ? **使用者資訊完全來自 Windows AD（DisplayName、Email、Groups 等）**

---

## ?? 確認使用者來源是 Windows AD

### 查看登入日誌

開啟檔案：
```
D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\login_debug.log
```

應該看到：
```
========================================
Login START: admin01
Mode: Pure Windows AD (No Database Check)
========================================
? AD Authentication SUCCESS: admin01
   DisplayName: (來自 Windows AD)
   Permission Level: Admin
   Groups: App_Admins, BUILTIN\Users, ...
? UserAccount created from AD:
   UserId: admin01
   DisplayName: (來自 Windows AD)
   AccessLevel: Admin
   Email: (來自 Windows AD)
? Login COMPLETED in XXXms
   Auth Method: Windows AD
   AccessLevel: Admin
========================================
```

**關鍵字**：
- ? `Mode: Pure Windows AD (No Database Check)` - 純 AD 驗證
- ? `UserAccount created from AD` - 從 AD 建立使用者
- ? `Auth Method: Windows AD` - 驗證方法

---

## ?? 如果登入失敗

### 檢查清單：

#### 1. 確認 Windows 帳號存在
```powershell
net user admin01
```

#### 2. 確認群組成員正確
```powershell
net localgroup App_Admins
```
應該看到 `admin01` 在成員清單中。

#### 3. 測試密碼是否正確
```powershell
# 嘗試以 admin01 身份執行命令
runas /user:admin01 cmd
```
輸入密碼 `admin01admin01`，如果能開啟 cmd，表示密碼正確。

#### 4. 查看登入日誌
開啟檔案並搜尋關鍵字：
- `?` → 登入成功
- `?` → 登入失敗（查看原因）

#### 5. 檢查 Visual Studio Debug 輸出
在「輸出」視窗中搜尋：
```
[SecurityContext] Login START: admin01
[SecurityContext] ? AD Authentication SUCCESS: admin01
[SecurityContext] ? AccessLevel determined: Admin
[SecurityContext] ? Login COMPLETED
```

---

## ?? 一鍵設定腳本（全自動）

複製以下腳本，貼到 PowerShell（管理員模式）：

```powershell
# ======================================
# WpfApp1 admin01 快速設定腳本
# ======================================

Write-Host "開始設定 admin01 帳號..." -ForegroundColor Cyan

# 1. 建立使用者
Write-Host "`n[1/4] 建立使用者 admin01..." -ForegroundColor Yellow
net user admin01 admin01admin01 /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? 使用者 admin01 已存在或建立成功" -ForegroundColor Green
} else {
    Write-Host "? 建立使用者失敗" -ForegroundColor Red
    exit
}

# 2. 設定密碼永不過期
Write-Host "`n[2/4] 設定密碼永不過期..." -ForegroundColor Yellow
wmic useraccount where "name='admin01'" set PasswordExpires=false 2>$null
Write-Host "? 密碼設定完成" -ForegroundColor Green

# 3. 建立群組（如果不存在）
Write-Host "`n[3/4] 建立 App_Admins 群組..." -ForegroundColor Yellow
net localgroup App_Admins /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? 群組 App_Admins 已存在或建立成功" -ForegroundColor Green
}

# 4. 加入群組
Write-Host "`n[4/4] 將 admin01 加入 App_Admins..." -ForegroundColor Yellow
net localgroup App_Admins admin01 /add 2>$null
if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) {
    Write-Host "? admin01 已加入 App_Admins" -ForegroundColor Green
}

# 5. 確認設定
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "設定完成！請使用以下帳號登入：" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "使用者名稱：admin01" -ForegroundColor White
Write-Host "密碼：admin01admin01" -ForegroundColor White
Write-Host "權限等級：Admin (L4)" -ForegroundColor White
Write-Host "資料來源：Windows AD (非本地資料庫)" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

# 6. 顯示群組成員
Write-Host "`n目前 App_Admins 群組成員：" -ForegroundColor Yellow
net localgroup App_Admins

Write-Host "`n? 現在可以啟動 WpfApp1 並登入了！" -ForegroundColor Green
Write-Host "   所有使用者資訊將來自 Windows AD，非本地資料庫" -ForegroundColor Yellow
```

---

## ?? 使用者資訊來源確認

| 資訊欄位 | 來源 | 說明 |
|---------|------|------|
| `UserId` | ? Windows AD | 使用者名稱 |
| `DisplayName` | ? Windows AD | 完整名稱 |
| `Email` | ? Windows AD | 電子郵件 |
| `AccessLevel` | ? Windows AD Groups | 從 App_ 群組判斷 |
| `Department` | ? Windows AD Groups | 群組清單 |
| `Remarks` | ? Windows AD | 記錄?組資訊 |
| `CreatedBy` | ? `"Windows AD"` | 標記來源 |
| ? 資料庫 Users 表 | ? **不使用** | 僅用於 Audit Trail |

---

## ?? 完成！

設定完成後：
1. **啟動 WpfApp1**
2. **使用者欄位應該已預填 `admin01`**（DEBUG 模式）
3. **輸入密碼 `admin01admin01`**
4. **按 Enter 登入**
5. **所有使用者資訊來自 Windows AD！** ??

---

## ?? 重要提醒

- ? **所有使用者資訊來自 Windows AD**
- ? **資料庫不再儲存使用者帳號**
- ? **資料庫僅用於 Audit Trail 和 Data Logs**
- ? **與 WinFormsAD 完全一致**
