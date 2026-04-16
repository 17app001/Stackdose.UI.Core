# CLAUDE.md — Stackdose.UI.Core 快速上下文

> 每次對話開始時讀這份文件。讀完即可掌握全局狀況。
> 目前開發焦點與 Sprint 狀態：見 `CURRENT_FOCUS.md`
> 完整功能完成歷史：見 `docs/STATUS.md`

---

## 專案定位

**一句話：讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 稽核要求的監控介面。**

核心解法：
- **MachinePageDesigner** → 拖曳設計，輸出 `.machinedesign.json`
- **DesignPlayer** → 讀 JSON 直接部署，零程式碼
- **DeviceFramework** → JSON 驅動框架，複雜設備邏輯
- **全架構內建** FDA 21 CFR Part 11（日誌、稽核、使用者權限）

技術定位：企業級 WPF 框架（.NET 8 Windows-only，x64）。Core/Templates 是基礎庫，DeviceFramework 是組裝框架，App.* 是應用，Tools.* 是開發工具。

---

## 方案與專案

| 專案 | 說明 | 狀態 |
|---|---|---|
| `Stackdose.UI.Core` | 26 個 WPF 控制項、Context 系統、SQLiteLogger | 穩定 |
| `Stackdose.UI.Templates` | AppHeader / LeftNav / BottomBar Shell 布局 | 穩定 |
| `Stackdose.App.ShellShared` | 多 App 共用 Shell 服務層 | 穩定 |
| `Stackdose.App.DeviceFramework` | JSON 驅動設備 App 組裝框架 | 穩定 |
| `Stackdose.App.UbiDemo` | UBI 烤箱完整範例（DeviceFramework 最佳實踐） | 維護 |
| `Stackdose.App.DesignRuntime` | 開發驗證：PLC + JSON + 熱更新 | ✅ 完整 |
| `Stackdose.App.DesignPlayer` | 量產交付：JSON + PLC + 登入管控 | ✅ 完整 |
| `Stackdose.Tools.MachinePageDesigner` | 自由畫布拖曳設計器，輸出 .machinedesign.json | ✅ 完整 |
| `Stackdose.Tools.DesignViewer` | 拖入 JSON 即時預覽 | ✅ 完整 |
| `Stackdose.Tools.ProjectGenerator` | CLI 一鍵建立新設備 App | 穩定 |
| `Stackdose.Tools.ProjectGeneratorUI` | GUI 版專案產生器 | 穩定 |
| `Stackdose.UI.Core.Tests` | xUnit 測試（SqliteLogger） | 穩定 |

方案對照：
- 設計器 / 量產 App → `Stackdose.Designer.sln`
- 框架核心 / 全局修改 → `Stackdose.UI.Core.sln`

---

## 外部依賴（跨 Repo）

### Stackdose.Platform（`../Stackdose.Platform/`）

| 專案 | 引用方 | 說明 |
|---|---|---|
| `Stackdose.Abstractions` | UI.Core、DeviceFramework | `IPlcManager`、`IPlcClient`、`IPlcMonitor` — 所有介面定義 |
| `Stackdose.Core` | UI.Core | enums、MachineState、Context 工具 |
| `Stackdose.Hardware` | UI.Core | PLC 連線實作（FX3U） |
| `Stackdose.PrintHead` | UI.Core | Feiyang PrintHead 控制 |
| `Stackdose.Plc` | DeviceFramework | PLC 輪詢 |

> ⚠️ **危險介面**：改動 `IPlcManager`、`IPlcMonitor` 簽名會導致 UI.Core 多處編譯失敗。詳見 `docs/kb/platform-contracts.md`。

### FeiyangWrapper（`../../Sdk/FeiyangWrapper/`）
C++ 原生 DLL，csproj 條件 `Exists(...)` 引用，兩者都沒有則不報錯但功能缺失。

---

## 核心架構規則（勿違反）

1. 不在 `Controls/*.xaml` 寫硬編碼色碼 → 用語意 Token（`Surface.*`、`Text.*`、`Action.*`）
2. 不繞過 `ComplianceContext` 散落寫日誌 → 關閉前必須呼叫 `ComplianceContext.Shutdown()`
3. App 特屬邏輯不耦合進 `UI.Core`
4. 編譯失敗先確認 `../Stackdose.Platform/` 各專案與 `FeiyangWrapper.dll` 是否存在
5. 新增 async 非 UI 方法時加 `ConfigureAwait(false)`

---

## 知識庫索引

| 文件 | 說明 |
|---|---|
| `CURRENT_FOCUS.md` | 目前 Sprint 焦點與下一步方向 |
| `docs/STATUS.md` | 完整功能完成歷史 + JSON 格式參考 |
| `docs/PROJECT_MAP.md` | 完整專案依賴圖 |
| `docs/kb/architecture.md` | 架構設計、Context 系統、資料流 |
| `docs/kb/designer-system.md` | MachinePageDesigner + DesignViewer + DesignRuntime |
| `docs/kb/platform-contracts.md` | Platform 跨 Repo 契約（危險介面清單） |
| `docs/kb/controls-reference.md` | 控制項快速參考（26 個） |
| `docs/kb/deviceframework-guide.md` | DeviceFramework 新人指南 |
| `docs/kb/designer-quickstart.md` | 設計師 Quick Start（角色分工 + 完整流程） |
| `docs/kb/theme-token-standard.md` | 主題 Token 收斂規範 |
| `docs/devlog/2026-04.md` | 2026 年 4 月開發日誌 |
