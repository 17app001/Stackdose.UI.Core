# ? UAC 沒有彈出的解決方法

## ?? 問題診斷

您按 F5 後 UAC 沒有彈出，這表示 `app.manifest` 沒有正確嵌入到 exe 中。

---

## ? 解決步驟

### 步驟 1：在 Visual Studio 中重新載入專案

1. 在 Visual Studio 方案總管中
2. 右鍵點擊「WpfApp1」專案
3. 選擇「卸載專案」
4. 再次右鍵點擊「WpfApp1」
5. 選擇「重新載入專案」

---

### 步驟 2：確認 app.manifest 在專案中

1. 在方案總管中展開「WpfApp1」專案
2. 確認看到 `app.manifest` 檔案
3. 如果沒看到：
   - 右鍵專案 → 新增 → 現有項目
   - 選擇 `app.manifest`
   - 確定

---

### 步驟 3：清理並重建

在 Visual Studio 中：

1. **建置 → 清除方案**
2. **建置 → 重建方案**
3. 等待編譯完成

---

### 步驟 4：確認設定

開啟 `WpfApp1.csproj`（在 Visual Studio 中雙擊專案檔），確認包含：

```xml
<PropertyGroup>
  <ApplicationManifest>app.manifest</ApplicationManifest>
</PropertyGroup>

<ItemGroup>
  <None Include="app.manifest" />
</ItemGroup>
```

---

### 步驟 5：再次按 F5

1. 確保所有檔案都已儲存
2. 按 F5 偵錯
3. **UAC 應該會彈出**

---

## ?? 驗證 Manifest 是否嵌入

### 方法 1：使用工具檢查

下載並使用 Resource Hacker 或類似工具：
```
https://www.angusj.com/resourcehacker/
```

開啟 `WpfApp1.exe`，查看是否有 Manifest 資源。

---

### 方法 2：執行測試

直接執行編譯後的 exe：

```powershell
cd "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows"
.\WpfApp1.exe
```

**預期結果：**
- ? UAC 對話框應該彈出
- ? 點「是」後程式啟動

**如果沒有彈出 UAC：**
- ? Manifest 沒有正確嵌入

---

## ??? 替代方案：手動設定權限

如果 manifest 還是無法生效，可以暫時使用這個方法：

### 方法 A：捷徑屬性

1. 右鍵點擊 `WpfApp1.exe` → 建立捷徑
2. 右鍵點擊捷徑 → 內容
3. 「捷徑」標籤 → 「進階」
4. ? 勾選「以系統管理員身分執行」
5. 確定

? 雙擊捷徑會彈出 UAC

---

### 方法 B：檔案相容性設定

1. 右鍵點擊 `WpfApp1.exe` → 內容
2. 「相容性」標籤
3. ? 勾選「以系統管理員身分執行此程式」
4. 確定

? 雙擊 exe 會彈出 UAC

?? **注意：** 這只是暫時方案，最好還是讓 manifest 正確嵌入。

---

## ?? 進階診斷

### 檢查編譯輸出

在 Visual Studio：

1. **工具 → 選項**
2. **專案和方案 → 建置並執行**
3. MSBuild 專案建置輸出詳細程度 → **詳細**
4. 確定
5. 重新建置

查看輸出視窗，搜尋關鍵字：
- `manifest`
- `app.manifest`
- `MT.exe`（Manifest Tool）

應該看到類似：
```
Embedding manifest...
MT.exe: Successfully generated manifest
```

---

### 檢查 obj 目錄

```powershell
Get-ChildItem "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\obj\Debug\net8.0-windows" | Where-Object { $_.Name -like "*manifest*" }
```

應該看到：
- `WpfApp1.exe.manifest`
- 或類似的檔案

如果沒有，表示 manifest 沒有被處理。

---

## ?? 最簡單的驗證方法

### 快速測試

1. 編譯完成後
2. 直接雙擊 exe
3. 看是否彈出 UAC

**如果彈出 UAC：**
? Manifest 正確嵌入

**如果沒彈出：**
? Manifest 沒有正確嵌入，請重新執行上述步驟

---

## ?? 如果還是不行

### 檢查清單

- [ ] `app.manifest` 檔案存在於 `WpfApp1` 目錄
- [ ] `WpfApp1.csproj` 包含 `<ApplicationManifest>app.manifest</ApplicationManifest>`
- [ ] Visual Studio 已重新載入專案
- [ ] 已清理並重建方案
- [ ] 編譯輸出視窗沒有 manifest 相關錯誤
- [ ] 已儲存所有檔案

### 終極解決方案

如果上述都無效，嘗試：

1. **關閉 Visual Studio**
2. **刪除 `obj` 和 `bin` 目錄**
```powershell
Remove-Item "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin" -Recurse -Force
Remove-Item "D:\工作區\Project\Stackdose.UI.Core\WpfApp1\obj" -Recurse -Force
```
3. **重新開啟 Visual Studio**
4. **清理方案**
5. **重建方案**
6. **按 F5**

---

## ?? 預期行為

### ? 正確的行為（Manifest 已嵌入）

```
按 F5
  ↓
編譯成功
  ↓
UAC 對話框彈出
  ↓
點「是」
  ↓
程式以管理員權限啟動
  ↓
LoginDialog 出現
```

### ? 錯誤的行為（Manifest 未嵌入）

```
按 F5
  ↓
編譯成功
  ↓
程式直接啟動（沒有 UAC）
  ↓
使用者管理功能無法使用
```

---

## ?? 成功確認

當您看到：
1. ? 按 F5 後 UAC 彈出
2. ? exe 圖示有盾牌標記
3. ? 工作管理員顯示「提升權限 = 是」
4. ? 使用者管理功能正常運作

表示 manifest 已正確嵌入！
