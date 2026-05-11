---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
last_updated: 2026-05-11
source_of_truth: false
---

# AI 驗證指南（無實機環境）

> 本文件解答：AI 在沒有真實 PLC 或噴頭硬體的情況下，如何驗證修改是否正確？  
> **重要前提：** 本框架的最終驗證需要 DesignRuntime + 實機。本文件只說明可以在開發環境中完成的驗證步驟。

---

## 1. 驗證環境對照

| 驗證目標 | 可用工具 | 是否需要硬體 |
|---|---|---|
| 控件外觀、主題切換 | DesignRuntime（Mock 模式） | ❌ 不需要 |
| JSON 解析、控件渲染 | DesignViewer | ❌ 不需要 |
| PLC 資料顯示 | DesignRuntime（需真實 PLC） | ✅ 需要 |
| 稽核日誌格式 | `UI.Core.Tests`（C# 單元測試） | ❌ 不需要 |
| SecurityContext 權限計算 | `UI.Core.Tests`（C# 單元測試） | ❌ 不需要 |
| 噴頭控制 | DesignRuntime + FeiyangWrapper.dll + 實機 | ✅ 需要 |
| Behavior Engine 觸發 | DesignRuntime（Mock PLC 可模擬 bit 切換） | 部分可模擬 |

---

## 2. DesignRuntime 啟動模式

### 2.1 無 PLC 啟動（UI 驗證）

DesignRuntime 在找不到 PLC 連線時會進入「離線模式」：
- 所有 `PlcLabel` / `PlcText` / `PlcStatusIndicator` 顯示 `---` 或初始值
- 主題切換、佈局、控件外觀仍可正常驗證
- BehaviorEngine 不會觸發（因為沒有 `ScanUpdated` 事件）

**啟動步驟：**
1. 開啟 `Stackdose.Designer.sln`
2. Startup Project 設為 `Stackdose.App.DesignRuntime`
3. 直接 F5 執行（不需要 PLC 連線設定）
4. 拖入 `.machinedesign.json` 驗證渲染

### 2.2 有 PLC 啟動（完整驗證）

需要：
- 實體 PLC 或 PLC 模擬器（GX Works 模擬功能）
- `Config/Machine*.config.json` 中正確的 PLC IP / Port
- `Stackdose.Platform` Repo 編譯正確

---

## 3. 可以驗證的項目（無硬體）

### 3.1 主題 Token 修改驗證

**修改了什麼：** 新增 / 改動 `DarkColors.xaml` 或 `LightColors.xaml` 的 Token

**驗證步驟：**
1. 開啟 DesignRuntime，載入任意 `.machinedesign.json`
2. 點擊主題切換按鈕（Dark ↔ Light）
3. 確認控件顏色正確跟隨切換
4. 確認兩個模式都沒有出現 `{StaticResource Token}` 原始文字（代表 Token 找不到）

**常見錯誤：**
- 只改了 `DarkColors.xaml` 忘改 `LightColors.xaml` → Light 模式下控件顯示空白或例外

---

### 3.2 RuntimeControlFactory 修改驗證

**修改了什麼：** 新增或修改控件工廠中的 `Create*` 方法

**驗證步驟：**
1. 在 MachinePageDesigner 建立包含該控件的頁面，儲存為 JSON
2. 開啟 DesignViewer，拖入 JSON，確認控件正確渲染（設計時外觀）
3. 開啟 DesignRuntime，載入 JSON，確認控件執行時外觀（離線模式）
4. 切換 Dark/Light 主題，確認顏色 Token 正確

**Spacer / GroupBox 特定驗證：**
- 在設計器建立 Spacer，設定不同 `headerColor`（Normal / Primary / Success / Warning / Error）
- 確認 DesignRuntime 中各 Spacer 顏色不同（若全部同色 → `headerColor` prop 讀取有誤）

---

### 3.3 JSON Schema 修改驗證

**修改了什麼：** `DesignerItemDefinition`、`PageDefinition` 的序列化邏輯

**驗證步驟：**
1. 用 MachinePageDesigner 建立包含新 prop 的控件，儲存 JSON
2. 用文字編輯器確認 JSON 輸出包含正確的 prop 欄位
3. 用 DesignRuntime 載入 JSON，確認 prop 被正確讀取

---

### 3.4 Behavior Engine 修改驗證（部分可驗證）

**修改了什麼：** `events[]` JSON schema、新 Handler、BehaviorEngine Dispatch 邏輯

**可驗證（無 PLC）：**
- `setProp` action — DesignRuntime 離線時仍可觸發，透過 BehaviorEventBus（SecuredButton 點擊）
- `showDialog` action — 彈出對話框不需要 PLC
- `logAudit` action — 寫 SQLite log，不需要 PLC；可查 `logs/` 目錄確認

**需要 PLC 才能驗證：**
- `writePlc` action — 需要真實 PLC 回應
- `valueChanged` on 事件觸發 — 需要 PLC 數值實際變化

---

### 3.5 ComplianceContext / SqliteLogger 驗證

**驗證方式：** `UI.Core.Tests` 單元測試（不需要 WPF）

```csharp
// 可以自動化測試的範例
[Fact]
public void LogAuditTrail_WritesCorrectFormat()
{
    ComplianceContext.Initialize(":memory:");  // 使用記憶體 SQLite
    ComplianceContext.LogAuditTrail("LOGIN", "user: admin");
    var entries = ComplianceContext.GetRecentEntries(1);
    Assert.Contains("LOGIN", entries[0].Action);
}
```

---

### 3.6 SecurityContext 權限計算驗證

**驗證方式：** `UI.Core.Tests` 單元測試

```csharp
[Fact]
public void CheckAccess_OperatorCannotAccessSupervisorFeature()
{
    SecurityContext.SetCurrentUser(new User { Level = AccessLevel.Operator });
    bool result = SecurityContext.CheckAccess(AccessLevel.Supervisor);
    Assert.False(result);
}
```

---

## 4. 不適合自動化的驗證（需手動）

| 項目 | 原因 | 替代方案 |
|---|---|---|
| WPF 控件視覺外觀 | WPF 需要 STA UI Thread，xUnit 跑在 MTA | DesignRuntime 手動目視 |
| 主題切換視覺效果 | 渲染結果需目視確認 | DesignRuntime 手動切換 |
| PLC 連線與斷線行為 | 需要實體硬體或模擬器 | 實機測試 |
| PrintHead 初始化流程 | 需要 FeiyangWrapper.dll + 實機 | 實機測試 |
| 多控件 BehaviorEngine 聯動 | 依賴 PLC 值變化序列 | 實機測試 |

---

## 5. 編譯環境確認（每次修改後）

執行以下任一方式確認修改不破壞編譯：

```powershell
# 方法 1：編譯整個方案
dotnet build Stackdose.UI.Core.sln

# 方法 2：只編譯 DesignRuntime（最快速驗證）
dotnet build Stackdose.App.DesignRuntime/Stackdose.App.DesignRuntime.csproj
```

**⚠️ 跨 Repo 依賴：** 若修改了 `Stackdose.Platform` 的任何介面，需先確認 `../Stackdose.Platform/` 也能編譯成功，否則本 Repo 的錯誤訊息可能會誤導。

---

## 6. 各 App RuntimeControlFactory 修改的驗證

由於 4 個 App 各有獨立副本（ADR-009），修改任一個都需要個別驗證：

| App | Startup Project | 驗證步驟 |
|---|---|---|
| DesignRuntime | `Stackdose.App.DesignRuntime` | F5 執行，載入 JSON |
| MyPrintApp3 | `MyPrintApp3` | F5 執行（需 PrintHead 硬體） |
| DashboardTest1 | `Stackdose.App.DashboardTest1` | F5 執行 |
| ModelE | `ModelE` | F5 執行（需 ModelE 硬體） |

若只改了 DesignRuntime 的 factory，其他 App 的 factory 仍為舊版本——這是已知的副本技術債（ADR-009）。

---

## 7. AI 修改後的標準檢查清單

每次修改完成後，AI 應確認以下項目（可在回應中附上這個清單）：

- [ ] 編譯通過（`dotnet build` 無錯誤）
- [ ] 沒有引入硬編碼色碼（`#RRGGBB` / `Color.FromRgb` 在非預期位置）
- [ ] 新增 Token 時兩份字典（Dark/Light）都有同步
- [ ] 沒有直接呼叫 `SqliteLogger`（應走 ComplianceContext）
- [ ] RuntimeControlFactory 副本是否需要同步（若改了 Spacer/GroupBox 邏輯）
- [ ] 沒有修改 `IPlcManager / IPlcMonitor / IPrintHead` 簽名
- [ ] `headerColor` prop 的預設值是 `"Normal"` 不是 `"Primary"`
