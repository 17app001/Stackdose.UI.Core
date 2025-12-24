# FeiyangSDK 整合指南

## ?? 目的
本文件說明如何在新專案中正確整合 FeiyangSDK，確保執行時能找到所需的 DLL。

---

## ?? 何時需要加入 PostBuild 複製？

### ? **需要加入（以下任一條件符合）：**
1. 專案直接參考 `FeiyangWrapper.vcxproj`
2. 專案使用 `PrintHeadStatus` 控制項
3. 專案使用 `FeiyangPrintHead` 類別
4. 專案需要連接飛揚噴頭硬體

### ? **不需要加入：**
- 只使用 PlcStatus、RecipeLoader 等其他控制項
- 純測試/Demo 專案且不涉及噴頭功能

---

## ?? 操作步驟

### **步驟 1：檢查專案參考**

確認您的 `.csproj` 檔案中是否有以下參考之一：

```xml
<!-- 直接參考 FeiyangWrapper -->
<ProjectReference Include="..\..\Sdk\FeiyangWrapper\FeiyangWrapper\FeiyangWrapper.vcxproj" />

<!-- 或間接透過 Stackdose.PrintHead 參考 -->
<ProjectReference Include="..\..\Stackdose.Platform\Stackdose.PrintHead\Stackdose.PrintHead.csproj" />

<!-- 或間接透過 Stackdose.UI.Core 參考 -->
<ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
```

### **步驟 2：加入 PostBuild Target**

在您的 `.csproj` 檔案 `</Project>` 標籤**之前**加入：

```xml
<!-- ?? 複製 FeiyangSDK 依賴的 DLLs -->
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <!-- 只在 FeiyangSDK 資料夾存在時才複製 -->
  <Exec Command="if exist &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib&quot; xcopy /Y /E /I &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib\*&quot; &quot;$(TargetDir)&quot;" />
</Target>
```

### **步驟 3：驗證路徑**

確認 FeiyangSDK 路徑正確：
```
$(SolutionDir) = D:\工作區\Project\Solution\Stackdose.Solution\
FeiyangSDK-2.3.1\lib\ 應包含:
  ├─ NJCS.dll
  ├─ NJCS.lib
  ├─ NJCSC.dll
  ├─ NJCSC.lib
  └─ opencv_world420.dll
```

### **步驟 4：重建專案**

```powershell
# 清理
dotnet clean

# 重建
dotnet build
```

### **步驟 5：檢查輸出目錄**

確認 `bin\Debug\net8.0-windows\` 包含：
- ? `NJCS.dll`
- ? `NJCSC.dll`
- ? `opencv_world420.dll`

---

## ?? 完整範例

### **範例 1：WPF 應用程式專案**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <!-- 使用 Stackdose.UI.Core (內含 PrintHeadStatus) -->
    <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
  </ItemGroup>

  <!-- ? 必須加入：複製 FeiyangSDK DLLs -->
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="if exist &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib&quot; xcopy /Y /E /I &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib\*&quot; &quot;$(TargetDir)&quot;" />
  </Target>

</Project>
```

### **範例 2：類別庫專案**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 直接使用 FeiyangPrintHead -->
    <ProjectReference Include="..\..\Stackdose.Platform\Stackdose.PrintHead\Stackdose.PrintHead.csproj" />
  </ItemGroup>

  <!-- ? 必須加入：複製 FeiyangSDK DLLs -->
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="if exist &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib&quot; xcopy /Y /E /I &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib\*&quot; &quot;$(TargetDir)&quot;" />
  </Target>

</Project>
```

---

## ?? 故障排除

### **問題 1：執行時找不到 NJCS.dll**

**症狀：**
```
System.DllNotFoundException: 無法載入 DLL 'NJCS.dll': 找不到指定的模組。
```

**解決方法：**
1. 檢查 `bin\Debug\net8.0-windows\` 是否有 `NJCS.dll`
2. 確認 `.csproj` 已加入 PostBuild Target
3. 重新建置專案

### **問題 2：xcopy 失敗**

**症狀：**
```
建置錯誤: xcopy 傳回 exit code 4
```

**解決方法：**
1. 確認 `D:\工作區\Project\Solution\Stackdose.Solution\FeiyangSDK-2.3.1\lib` 存在
2. 檢查路徑中沒有多餘空格
3. 使用 `if exist` 條件避免路徑不存在時報錯

### **問題 3：平台架構不符警告**

**症狀：**
```
warning MSB3270: 處理器架構 "MSIL" 與 "AMD64" 不相符
```

**解決方法：**
這是正常警告，不影響執行。若要消除警告：
1. 開啟「組態管理員」
2. 將專案平台改為 `x64`
3. 重建專案

---

## ?? 相關專案狀態

| 專案 | 是否需要 PostBuild | 目前狀態 |
|------|------------------|---------|
| `WpfApp1` | ? 需要 | ? 已設定 |
| `Stackdose.UI.Core` | ? 需要 | ? 已設定 |
| `Wpf.Demo` | ? 不需要 | ? 未設定（正確） |
| `Stackdose.PrintHead` | ? 不需要 | ? 未設定（正確） |

---

## ? 檢查清單

建立新專案時，請依此檢查：

- [ ] 專案是否使用 `PrintHeadStatus` 控制項？
- [ ] 專案是否參考 `FeiyangWrapper` 或 `Stackdose.PrintHead`？
- [ ] 已在 `.csproj` 加入 PostBuild Target？
- [ ] 建置後 `bin` 目錄包含 `NJCS.dll`、`NJCSC.dll`、`opencv_world420.dll`？
- [ ] 執行程式沒有 `DllNotFoundException` 錯誤？

---

## ?? 支援

如有問題，請檢查：
1. 本文件的「故障排除」章節
2. 參考 `WpfApp1.csproj` 的設定
3. 查看建置輸出視窗的 PostBuild 訊息

---

**最後更新：** 2025/01/18  
**維護者：** Stackdose 開發團隊
