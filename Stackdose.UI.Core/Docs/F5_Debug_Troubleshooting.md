# ?? Visual Studio F5 偵錯問題診斷

## ? 按下 F5 後發生什麼？

### 情況 A：UAC 對話框不斷彈出

**症狀：**
- 按 F5
- UAC 對話框出現
- 點「是」後，程式啟動
- 關閉程式
- 再按 F5，UAC 又彈出

**原因：** Visual Studio 沒有以管理員身分執行

**解決方法：**

#### ? 永久解決（推薦）

1. **關閉當前的 Visual Studio**
2. **右鍵點擊 Visual Studio 圖示**
3. **選擇「以系統管理員身分執行」**
4. 重新開啟專案
5. 按 F5

? UAC 不會再彈出

#### ?? 設定為永遠以管理員執行

1. 右鍵點擊 Visual Studio 圖示
2. 選擇「內容」
3. 「相容性」標籤
4. ? 勾選「以系統管理員身分執行此程式」
5. 確定

? 以後開啟 Visual Studio 會自動要求管理員權限

---

### 情況 B：程式無法啟動（存取被拒）

**症狀：**
- 按 F5
- Visual Studio 顯示錯誤
- 或程式完全沒反應

**可能原因：**
1. Visual Studio 沒有管理員權限
2. 編譯失敗
3. 其他程序佔用 port/資源

**診斷步驟：**

#### 步驟 1：檢查 Visual Studio 權限

```powershell
# 在 PowerShell 中執行
$vsProcess = Get-Process | Where-Object { $_.ProcessName -like "*devenv*" }
if ($vsProcess) {
    $isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    Write-Host "Visual Studio 是否以管理員執行: $isAdmin"
}
```

如果顯示 `False`，請以管理員身分重新開啟 Visual Studio。

#### 步驟 2：檢查編譯錯誤

查看「錯誤清單」視窗：
- 按 `Ctrl + \, E`
- 或「檢視」→「錯誤清單」

如果有錯誤，先修正再按 F5。

#### 步驟 3：清理並重建

```
1. 「建置」→「清除方案」
2. 「建置」→「重建方案」
3. 按 F5
```

---

### 情況 C：程式啟動後立即關閉

**症狀：**
- 按 F5
- 程式視窗閃一下
- 立即關閉

**可能原因：**
1. 未處理的例外
2. 登入失敗
3. MainWindow 建立失敗

**診斷步驟：**

#### 步驟 1：檢查輸出視窗

在 Visual Studio：
1. 「檢視」→「輸出」(或 Ctrl+Alt+O)
2. 查看最後的錯誤訊息

#### 步驟 2：檢查 app_startup.log

```powershell
Get-Content "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\app_startup.log" -Tail 50
```

查找關鍵字：
- `ERROR`
- `EXCEPTION`
- `FAILED`

#### 步驟 3：啟用中斷點

在 `App.xaml.cs` 的 `OnStartup` 方法設定中斷點：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    WriteLog("OnStartup: Called"); // ← 設中斷點在這裡
    // ...
}
```

按 F5，程式會在此處暫停，逐步執行找出問題。

---

### 情況 D：Visual Studio 顯示「無法啟動程式」

**錯誤訊息：**
```
無法啟動程式 'D:\...\WpfApp1.exe'
指定的可執行檔不是此 OS 平台的有效應用程式。
```

**原因：** 編譯目標平台不符

**解決方法：**

確認 `WpfApp1.csproj` 設定：
```xml
<PlatformTarget>x64</PlatformTarget>
```

如果您的系統是 64-bit，應該設為 `x64`。

---

## ?? 快速檢查清單

### ? 確認事項

- [ ] Visual Studio 以管理員身分執行
- [ ] 專案編譯成功（沒有錯誤）
- [ ] `app.manifest` 存在且正確
- [ ] `ApplicationManifest` 在 `.csproj` 中已啟用
- [ ] 目標平台設為 `x64`
- [ ] .NET 8 SDK 已安裝

---

## ?? 測試方法

### 測試 1：直接執行 exe

不使用 Visual Studio，直接執行編譯後的 exe：

```powershell
cd "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows"
.\WpfApp1.exe
```

如果這樣可以執行，問題在 Visual Studio 設定。

---

### 測試 2：使用命令列編譯

```powershell
cd "D:\工作區\Project\Stackdose.UI.Core\WpfApp1"
dotnet build
dotnet run
```

查看是否有編譯錯誤。

---

### 測試 3：檢查 manifest 嵌入

確認 manifest 是否正確嵌入到 exe：

```powershell
# 檢查 exe 檔案
$exePath = "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\WpfApp1.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host "WpfApp1.exe 存在"
    Write-Host "檔案大小: $($fileInfo.Length) bytes"
    Write-Host "修改時間: $($fileInfo.LastWriteTime)"
} else {
    Write-Host "WpfApp1.exe 不存在！"
}
```

---

## ?? 常見問題對應表

| 症狀 | 原因 | 解決方法 |
|------|------|---------|
| UAC 不斷彈出 | VS 沒有管理員權限 | 以管理員身分執行 VS |
| 程式無法啟動 | 編譯失敗 | 檢查錯誤清單 |
| 閃退 | 未處理例外 | 檢查 app_startup.log |
| 存取被拒 | 權限不足 | 以管理員身分執行 |
| 找不到 exe | 編譯失敗 | 清理並重建方案 |

---

## ?? 詳細診斷步驟

### 當您按下 F5 時...

#### 1?? Visual Studio 應該做什麼

```
1. 編譯專案
   ↓
2. 檢查是否有錯誤
   ↓
3. 生成 WpfApp1.exe
   ↓
4. 啟動 exe（以偵錯模式）
   ↓
5. 附加偵錯器
```

#### 2?? WpfApp1.exe 應該做什麼

```
1. 檢查 manifest → 要求管理員權限
   ↓
2. 如果 VS 是管理員 → 直接啟動
   如果 VS 不是管理員 → 彈出 UAC
   ↓
3. 初始化 App.OnStartup()
   ↓
4. 顯示 LoginDialog
   ↓
5. 登入成功後顯示 MainWindow
```

---

## ?? 建議設定

### Visual Studio 設定

#### 設定 1：永遠以管理員執行

1. 右鍵 Visual Studio 圖示 → 內容
2. 相容性 → ? 以系統管理員身分執行此程式
3. 確定

#### 設定 2：啟用詳細輸出

在 Visual Studio：
1. 工具 → 選項
2. 專案和方案 → 建置並執行
3. MSBuild 專案建置輸出詳細程度 → **詳細**

---

## ?? 成功標準

按 F5 後應該看到：

1. ? 編譯成功
2. ? LoginDialog 出現
3. ? 輸入 `admin01` / `admin01admin01`
4. ? MainWindow 顯示
5. ? 標題列顯示：`Stackdose Control System - admin01 (Admin)`

---

## ?? 需要更多資訊

請告訴我：

1. **按 F5 後，看到什麼？**
   - UAC 對話框？
   - 錯誤訊息？
   - 程式視窗？
   - 完全沒反應？

2. **Visual Studio 輸出視窗顯示什麼？**
   - 按 `Ctrl+Alt+O` 查看

3. **錯誤清單有錯誤嗎？**
   - 按 `Ctrl+\, E` 查看

4. **Visual Studio 是以管理員執行嗎？**
   - 標題列顯示「(系統管理員)」

請提供這些資訊，我可以更精準地診斷問題！
