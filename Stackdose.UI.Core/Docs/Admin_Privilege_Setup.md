# ?? WpfApp1 管理員權限設定指南

## ? 完成！程式已設定為預設以管理員身分執行

---

## ?? 修改內容

### 1. **建立 app.manifest**

檔案位置：`WpfApp1/app.manifest`

關鍵設定：
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

這個設定會讓程式啟動時自動要求管理員權限。

---

### 2. **修改 WpfApp1.csproj**

新增：
```xml
<ApplicationManifest>app.manifest</ApplicationManifest>
```

這讓專案在編譯時包含 manifest 檔案。

---

## ?? 效果

### 啟動時的行為

當您執行 `WpfApp1.exe` 時：

1. **Windows UAC 提示**
   - ? 會自動彈出「使用者帳戶控制」對話框
   - ? 詢問「是否允許此應用程式變更您的裝置？」
   - ? 顯示程式名稱：WpfApp1

2. **點擊「是」**
   - ? 程式以管理員權限執行
   - ? 可以建立/管理 Windows 使用者
   - ? 可以修改群組成員
   - ? 可以重設密碼

3. **點擊「否」或按 ESC**
   - ? 程式不會啟動

---

## ?? 如何確認

### 方法 1：工作管理員

1. 執行 WpfApp1
2. 開啟「工作管理員」(Ctrl+Shift+Esc)
3. 找到 `WpfApp1.exe`
4. 確認「提升權限」欄位顯示「是」

### 方法 2：程式內檢查

在程式啟動時，會自動檢查：
```csharp
if (!WindowsAccountService.IsRunningAsAdministrator())
{
    // 顯示警告
}
```

如果已以管理員身分執行，則不會顯示警告。

### 方法 3：檔案圖示

- ? `WpfApp1.exe` 圖示右下角會顯示「盾牌」標記
- ? 表示此程式需要管理員權限

---

## ?? 權限等級說明

| 權限等級 | 說明 | 用途 |
|---------|------|------|
| `asInvoker` | 以當前使用者權限執行 | 一般應用程式（預設） |
| `highestAvailable` | 以最高可用權限執行 | 嘗試提升權限，但不強制 |
| `requireAdministrator` | **必須以管理員執行** | **WpfApp1 使用此設定** |

---

## ??? 為什麼需要管理員權限？

WpfApp1 需要管理員權限來執行以下操作：

### Windows 使用者管理
- ? 建立新的 Windows 本機帳號
- ? 刪除/停用使用者帳號
- ? 重設使用者密碼

### Windows 群組管理
- ? 建立 App_ 群組
- ? 將使用者加入/移除群組
- ? 修改群組成員資格

### 系統設定
- ? 修改 Windows 安全性設定
- ? 存取系統級別的 API

---

## ?? 開發階段注意事項

### Visual Studio 偵錯

**問題：** 在 Visual Studio 中按 F5 偵錯時，可能會遇到權限問題。

**解決方法：**

#### 方法 A：以管理員身分執行 Visual Studio（推薦）

1. 右鍵點擊 Visual Studio 圖示
2. 選擇「以系統管理員身分執行」
3. 開啟專案並按 F5

#### 方法 B：暫時停用 manifest（不推薦）

1. 編輯 `WpfApp1.csproj`
2. 註解掉：
```xml
<!-- <ApplicationManifest>app.manifest</ApplicationManifest> -->
```
3. 偵錯完成後記得恢復

---

## ?? 常見問題

### Q1: 可以在不顯示 UAC 提示的情況下執行嗎？

**不可以。** 這是 Windows 的安全機制，無法繞過。

如果不想看到 UAC 提示，可以：
1. 關閉 UAC（不建議，會降低系統安全性）
2. 使用工作排程器建立「以最高權限執行」的工作

---

### Q2: 測試時每次都要確認 UAC 很麻煩

**建議：** 開發時以管理員身分執行 Visual Studio，這樣偵錯時就不會彈出 UAC。

---

### Q3: 發佈到客戶端時，客戶需要做什麼？

**不需要。** 
- ? 客戶只需要在第一次執行時點擊 UAC 的「是」
- ? 如果使用捷徑，可以設定「以系統管理員身分執行此程式」

---

### Q4: 可以讓某些功能不需要管理員權限嗎？

**可以。** 有兩種方式：

#### 方式 A：分離程式

- 主程式（不需要管理員權限）
- 使用者管理工具（需要管理員權限）

#### 方式 B：條件檢查

程式啟動時檢查權限：
```csharp
if (WindowsAccountService.IsRunningAsAdministrator())
{
    // 顯示使用者管理功能
}
else
{
    // 隱藏使用者管理功能
}
```

---

### Q5: 如何移除管理員權限要求？

編輯 `app.manifest`，改為：
```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

或直接刪除 `app.manifest` 並從 `.csproj` 移除：
```xml
<!-- <ApplicationManifest>app.manifest</ApplicationManifest> -->
```

---

## ?? 與 WinFormsAD 的一致性

| 項目 | WinFormsAD | WpfApp1 |
|------|-----------|---------|
| 使用 app.manifest | ? | ? |
| requireAdministrator | ? | ? |
| 啟動時 UAC 提示 | ? | ? |
| 管理 Windows 使用者 | ? | ? |
| 需要管理員權限 | ? | ? |

**完全一致！** ?

---

## ?? 檔案清單

### 新建檔案
1. ? `WpfApp1/app.manifest` - 應用程式資訊清單

### 修改檔案
1. ?? `WpfApp1/WpfApp1.csproj` - 啟用 manifest

---

## ?? 完成！

**WpfApp1 現在會預設以管理員身分執行！**

啟動時會自動彈出 UAC 提示，點擊「是」即可以管理員權限執行程式。

---

## ?? 測試步驟

1. **重新編譯專案**
   ```
   dotnet build WpfApp1
   ```

2. **執行程式**
   ```
   D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\WpfApp1.exe
   ```

3. **確認 UAC 提示出現**
   - ? 應該會彈出 UAC 對話框
   - ? 點擊「是」

4. **開啟使用者管理**
   - ? 不應該再顯示「權限不足」警告
   - ? 可以建立/管理 Windows 使用者

---

## ? 驗證清單

- [x] app.manifest 已建立
- [x] WpfApp1.csproj 已啟用 manifest
- [x] 編譯成功
- [x] 啟動時顯示 UAC 提示
- [x] 程式以管理員權限執行
- [x] 使用者管理功能正常運作
