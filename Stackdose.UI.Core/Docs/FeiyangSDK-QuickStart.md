# ?? 新專案必讀：FeiyangSDK 整合

## ?? 重要提醒

如果您的專案使用 **PrintHeadStatus 控制項** 或需要連接飛揚噴頭，**必須**手動加入 PostBuild 設定！

---

## ?? 快速設定（30 秒完成）

### 1?? 判斷是否需要

```csharp
// 您的 XAML 或程式碼中有以下任一項？
<controls:PrintHeadStatus />          // ? 需要
var printHead = new FeiyangPrintHead(); // ? 需要
// 都沒有？ ? 不需要
```

### 2?? 複製貼上到 .csproj

在您的專案檔案 `</Project>` 之前加入：

```xml
<!-- ?? 複製 FeiyangSDK 依賴的 DLLs -->
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="if exist &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib&quot; xcopy /Y /E /I &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib\*&quot; &quot;$(TargetDir)&quot;" />
</Target>
```

### 3?? 重建專案

```
Ctrl + Shift + B
```

### 4?? 驗證成功

**方法 1：自動檢查**
```powershell
.\Check-FeiyangSDK.ps1
```

**方法 2：手動檢查**
檢查 `bin\Debug\net8.0-windows\` 包含：
- ? NJCS.dll
- ? NJCSC.dll  
- ? opencv_world420.dll

---

## ??? 自動檢查工具

在解決方案根目錄執行：

```powershell
# 檢查所有專案
.\Check-FeiyangSDK.ps1

# 檢查特定專案
.\Check-FeiyangSDK.ps1 -ProjectPath "WpfApp1\WpfApp1.csproj"
```

工具會自動檢查：
- ? 專案參考是否需要 FeiyangSDK
- ? PostBuild 設定是否正確
- ? 輸出目錄是否包含必要 DLLs

---

## ?? 完整文件

詳細說明請參閱：[FeiyangSDK-Integration-Guide.md](./FeiyangSDK-Integration-Guide.md)

---

## ?? 常見錯誤

### ? 執行時出現：
```
System.DllNotFoundException: 無法載入 DLL 'NJCS.dll'
```

**原因：** 忘記加 PostBuild  
**解法：** 執行上方「快速設定」步驟 2

---

## ?? 參考範例

查看這些專案的 `.csproj` 設定：
- ? `WpfApp1/WpfApp1.csproj`
- ? `Stackdose.UI.Core/Stackdose.UI.Core.csproj`

---

## ?? 目前專案狀態

| 專案 | 需要設定 | 狀態 |
|------|---------|------|
| WpfApp1 | ? | ? 已設定 |
| Stackdose.UI.Core | ? | ? 已設定 |
| Wpf.Demo | ?? | ? 需要設定 |

> 執行 `.\Check-FeiyangSDK.ps1` 取得最新狀態

---

**請將此 README 釘選在您的書籤！** ??
