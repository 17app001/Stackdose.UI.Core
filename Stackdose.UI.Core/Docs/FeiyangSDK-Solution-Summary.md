# FeiyangSDK 整合方案總結

## ?? 決策記錄

**日期：** 2025/01/18  
**決策：** 採用**手動設定**方式管理 FeiyangSDK DLL 複製

---

## ?? 為什麼選擇手動設定？

### ? **優點**

1. **明確控制**
   - 開發人員清楚知道每個專案的依賴關係
   - 不會有「黑魔法」讓人困惑

2. **靈活性**
   - 某些專案可能不需要 FeiyangSDK DLLs
   - 避免不必要的檔案複製

3. **問題定位容易**
   - 出錯時可快速找到原因（PostBuild 未設定）
   - 不需要追蹤複雜的建置鏈

4. **團隊協作友善**
   - 新成員容易理解（有完整文件）
   - Code Review 時清楚看到變更

### ? **自動化方案的問題**

最初嘗試在 `Stackdose.PrintHead.csproj` 加入自動複製：

```xml
<Target Name="CopyFeiyangSDKDlls" AfterTargets="Build">
  <!-- 自動複製邏輯 -->
</Target>
```

**問題：**
- MSBuild 的 ProjectReference 傳遞行為不一致
- C++ (vcxproj) 和 C# (csproj) 的 PostBuild 處理方式不同
- 增加建置系統的複雜度
- 難以預測哪些專案會執行複製

---

## ?? 最終方案

### **原則**

> **哪個專案需要 FeiyangSDK，就在該專案的 .csproj 加入 PostBuild**

### **實作**

每個使用 FeiyangSDK 的專案，在 `.csproj` 中加入：

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="if exist &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib&quot; xcopy /Y /E /I &quot;$(SolutionDir)FeiyangSDK-2.3.1\lib\*&quot; &quot;$(TargetDir)&quot;" />
</Target>
```

### **判斷標準**

專案需要設定 PostBuild 的條件（任一符合）：

1. ? 直接參考 `FeiyangWrapper.vcxproj`
2. ? 使用 `PrintHeadStatus` 控制項
3. ? 程式碼中使用 `FeiyangPrintHead` 類別
4. ? 需要連接飛揚噴頭硬體

---

## ??? 開發人員工作流程

### **新建專案時**

1. 開發期間使用 `PrintHeadStatus` → 記得加 PostBuild
2. 建置專案
3. 執行 `.\Check-FeiyangSDK.ps1` 驗證
4. 如有錯誤，參考 `FeiyangSDK-Integration-Guide.md`

### **檢查現有專案**

```powershell
# 自動掃描所有專案
.\Check-FeiyangSDK.ps1
```

輸出範例：
```
?? 專案: WpfApp1.csproj
   ? 已正確設定 FeiyangSDK DLL 複製
   ?? 輸出目錄檢查:
      ? NJCS.dll
      ? NJCSC.dll
      ? opencv_world420.dll
```

---

## ?? 文件結構

建立了以下文件：

| 文件 | 用途 | 目標讀者 |
|------|------|---------|
| `FeiyangSDK-QuickStart.md` | 30 秒快速設定指南 | 所有開發人員 |
| `FeiyangSDK-Integration-Guide.md` | 完整技術文件 | 需要深入了解的開發人員 |
| `Check-FeiyangSDK.ps1` | 自動檢查工具 | CI/CD、本地驗證 |

---

## ?? 目前專案狀態

### **已正確設定**

| 專案 | 參考關係 | PostBuild | DLLs |
|------|---------|-----------|------|
| `WpfApp1` | ? FeiyangWrapper (直接)<br>? Stackdose.UI.Core | ? 已設定 | ? 完整 |
| `Stackdose.UI.Core` | ? FeiyangWrapper (直接)<br>? Stackdose.PrintHead | ? 已設定 | ? 完整 |

### **不需要設定**

| 專案 | 原因 |
|------|------|
| `Stackdose.PrintHead` | 類別庫，不需要執行 |
| `Stackdose.Hardware` | 無 Feiyang 相關功能 |

### **需要關注**

| 專案 | 狀態 | 建議 |
|------|------|------|
| `Wpf.Demo` | ?? 缺少設定 | 如果使用 PrintHeadStatus，需加入 PostBuild |

---

## ?? 維護指南

### **定期檢查**

建議在 CI/CD 流程中加入：

```yaml
# .github/workflows/build.yml
- name: Check FeiyangSDK Integration
  run: powershell .\Check-FeiyangSDK.ps1
```

### **新成員 Onboarding**

1. 閱讀 `FeiyangSDK-QuickStart.md`
2. 執行 `Check-FeiyangSDK.ps1` 了解目前狀態
3. 參考 `WpfApp1.csproj` 的設定

### **更新 FeiyangSDK 版本**

如果 SDK 版本從 `2.3.1` 升級到 `2.4.0`：

1. 更新所有專案的 PostBuild 路徑
2. 更新文件中的路徑範例
3. 更新 `Check-FeiyangSDK.ps1` 的路徑檢查

---

## ? 驗證清單

- [x] 恢復手動 PostBuild 設定
- [x] 移除自動化複製邏輯
- [x] 建立完整文件
- [x] 建立自動檢查工具
- [x] 驗證 WpfApp1 正常運作
- [x] 驗證 Stackdose.UI.Core 正常運作
- [x] 建置測試通過

---

## ?? 聯絡資訊

如有疑問，請參考：
- ?? 快速入門：`Docs/FeiyangSDK-QuickStart.md`
- ?? 完整文件：`Docs/FeiyangSDK-Integration-Guide.md`
- ?? 檢查工具：`Check-FeiyangSDK.ps1`

---

**維護者：** Stackdose 開發團隊  
**最後更新：** 2025/01/18
