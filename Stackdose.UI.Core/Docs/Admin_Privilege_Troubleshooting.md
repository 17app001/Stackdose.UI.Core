# ? 「應用程式無法執行」問題解決

## ?? 問題原因

當您看到「應用程式無法執行」或「存取被拒」時，這是**正常的**！

原因：WpfApp1 現在要求管理員權限，因此：
- ? 無法從普通 PowerShell 直接執行
- ? 無法雙擊執行（會彈出 UAC）
- ? 需要透過正確的方式啟動

---

## ? 正確的執行方式

### 方法 1：檔案總管（推薦）

1. 開啟檔案總管
2. 導航到：
   ```
   D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows
   ```
3. **雙擊 `WpfApp1.exe`**
4. **UAC 對話框會彈出**：
   ```
   使用者帳戶控制
   
   是否允許此應用程式變更您的裝置？
   
   WpfApp1
   經過驗證的發行者: 不明
   
   [是]  [否]
   ```
5. **點擊「是」**
6. ? 程式以管理員權限啟動

---

### 方法 2：PowerShell（管理員模式）

#### 步驟 A：以管理員身分開啟 PowerShell

1. 按 `Windows + X`
2. 選擇「Windows PowerShell (系統管理員)」或「終端機 (系統管理員)」

#### 步驟 B：執行程式

```powershell
cd "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows"
.\WpfApp1.exe
```

? 程式會直接啟動（不會再彈 UAC）

---

### 方法 3：Visual Studio 偵錯

#### 步驟 A：以管理員身分執行 Visual Studio

1. 右鍵點擊 Visual Studio 圖示
2. 選擇「以系統管理員身分執行」

#### 步驟 B：按 F5 偵錯

? 程式會直接啟動（不會彈 UAC）

---

## ?? 如何確認程式需要管理員權限

### 方法 A：檔案圖示

查看 `WpfApp1.exe` 的圖示：
- ? 右下角有「??? 盾牌」標記 → 需要管理員權限
- ? 沒有盾牌標記 → 不需要管理員權限

### 方法 B：檔案屬性

1. 右鍵點擊 `WpfApp1.exe`
2. 選擇「內容」
3. 「相容性」標籤
4. 確認：**沒有**勾選「以系統管理員身分執行此程式」

（因為 manifest 已經內嵌，所以不需要勾選此項）

---

## ? 常見錯誤

### 錯誤 1：從普通 PowerShell 執行

```powershell
PS> .\WpfApp1.exe
? 'WpfApp1.exe' 程式無法執行: 存取被拒。
```

**原因：** PowerShell 沒有管理員權限

**解決：** 使用「方法 2」以管理員身分開啟 PowerShell

---

### 錯誤 2：Visual Studio 偵錯時彈出 UAC

```
每次按 F5 都會彈出 UAC 對話框
```

**原因：** Visual Studio 沒有以管理員身分執行

**解決：** 關閉 Visual Studio，以管理員身分重新開啟

---

### 錯誤 3：程式啟動後立即關閉

**可能原因：**
1. 點擊 UAC 的「否」→ 程式不會啟動
2. 程式啟動時發生例外 → 檢查 `app_startup.log`

**檢查日誌：**
```powershell
Get-Content "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\app_startup.log" -Tail 50
```

---

## ?? 測試步驟

### 步驟 1：確認 manifest 已嵌入

執行以下命令檢查：

```powershell
# 檢查 exe 是否包含 manifest
Get-Content "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\WpfApp1.exe.config" -ErrorAction SilentlyContinue

# 檢查 app.manifest 檔案
Get-Content "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\app.manifest"
```

應該看到：
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

---

### 步驟 2：雙擊執行

1. 開啟檔案總管
2. 導航到輸出目錄
3. **雙擊 `WpfApp1.exe`**
4. **應該會彈出 UAC 對話框**
5. **點擊「是」**
6. ? 程式啟動

---

### 步驟 3：確認權限

程式啟動後：

1. 開啟「工作管理員」(Ctrl+Shift+Esc)
2. 「詳細資料」標籤
3. 找到 `WpfApp1.exe`
4. 確認「提升權限」= **是**

---

## ?? 如果不想要管理員權限

如果您不希望每次都要 UAC 提示，可以暫時移除管理員權限要求：

### 方法 A：修改 manifest（暫時）

編輯 `WpfApp1/app.manifest`：

```xml
<!-- 改為 asInvoker（不要求管理員權限） -->
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

### 方法 B：移除 manifest 參考（暫時）

編輯 `WpfApp1.csproj`：

```xml
<!-- 註解掉 -->
<!-- <ApplicationManifest>app.manifest</ApplicationManifest> -->
```

?? **注意：** 移除管理員權限後，「使用者管理」功能將無法使用！

---

## ?? 總結

| 執行方式 | 需要管理員 PowerShell | 會彈 UAC | 可以執行 |
|---------|---------------------|---------|---------|
| 雙擊 exe | ? 否 | ? 是 | ? 是 |
| 普通 PowerShell | ? 否 | ? 否 | ? 否（存取被拒） |
| 管理員 PowerShell | ? 是 | ? 否 | ? 是 |
| Visual Studio (普通) | ? 否 | ? 是 | ? 是 |
| Visual Studio (管理員) | ? 是 | ? 否 | ? 是 |

---

## ? 建議做法

### 開發階段
- ? 以管理員身分執行 Visual Studio
- ? 按 F5 偵錯（不會彈 UAC）

### 測試階段
- ? 雙擊 exe 執行
- ? 點擊 UAC 的「是」

### 部署階段
- ? 建立桌面捷徑
- ? 設定捷徑屬性：「以系統管理員身分執行此程式」（可選）

---

## ?? 這不是錯誤！

**「應用程式無法執行」或「存取被拒」是正常的行為！**

這表示 app.manifest 已經正確嵌入，程式正確要求管理員權限。

只需要用正確的方式啟動（雙擊 exe 或使用管理員 PowerShell）即可。
