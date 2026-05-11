---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
owner: Jerry
last_updated: 2026-05-11
source_of_truth: false
---

# CLAUDE.md — Stackdose.UI.Core AI 開發入口

> 每次對話開始時讀這份文件，再讀 `STATUS.md`。
> **目前最新狀態以 `STATUS.md` 為唯一真相。**
> `docs/refactor/PROGRESS.md` 僅作為重構階段（B0–B10）的歷史紀錄，不再更新。

---

## 文件閱讀順序

> 新 AI 接手時的最小必讀集合（依序）：

| 順序 | 文件 | 目的 | 必讀？ |
|---|---|---|---|
| 1 | `CLAUDE.md`（本文件） | 架構背景、規則、索引 | ★ 必讀 |
| 2 | `STATUS.md` | 今日最新狀態、進行中、未解問題 | ★ 必讀 |
| 3 | `SECURITY_RULES.md` | AI 使用資安限制、哪些不能動 | ★ 必讀 |
| 4 | `docs/RAG_INDEX.md` | 場景→文件導航地圖，找任何答案先看這裡 | ★ 必讀 |
| 5 | `docs/kb/DECISION_LOG.md` | 「能/不能做 X」架構決策速查（ADR） | ★ 必讀 |
| 6 | `docs/kb/GLOSSARY.md` | 術語定義 + 每個術語的能力邊界 | 按需查閱 |
| 7 | `docs/kb/architecture.md` | 深入架構細節（Context 系統、資料流） | 按需查閱 |
| 8 | `docs/kb/designer-system.md` | 設計器 / DesignRuntime 細節 | 按需查閱 |

**原則：** 讀完前 5 份就能開始工作。後 3 份在被問到特定問題時再查。

---

## 專案定位

企業級 WPF 工業設備 UI 框架（.NET 8 Windows-only，x64），目標讓設備廠商快速建立符合 **FDA 21 CFR Part 11** 稽核要求的操作介面。
這是**框架產品**，不是單一應用。Core / Templates 是基礎庫，DeviceFramework 是組裝框架，App.* 是範例，Tools.* 是開發工具。

---

## 常用指令

### 1. 建立新設備 App (Scaffolding)
```powershell
# 建立 Dashboard 模式專案 (生產環境推薦，無邊框全螢幕)
.\scripts\new-app.ps1 -AppName "MyProject" -Mode Dashboard

# 建立 Standard 模式專案 (多頁導航，含側邊欄)
.\scripts\new-app.ps1 -AppName "MyProject" -Mode Standard
```

### 2. 編譯與執行
```powershell
# 編譯整個方案
dotnet build Stackdose.UI.Core.sln

# 執行設計器
# 開啟 Stackdose.Designer.sln 並執行 MachinePageDesigner 專案
```

### 3. 常用方案 / Startup 對照

| 工作內容 | 開啟方案 | Startup Project |
|---|---|---|
| 設計器 / 執行環境 / 預覽 | `Stackdose.Designer.sln` | `Stackdose.App.DesignRuntime` |
| 框架核心 / DeviceFramework | `Stackdose.UI.Core.sln` | `Stackdose.App.UbiDemo` |
| 專案產生器 | `Stackdose.UI.Core.sln` | `Stackdose.Tools.ProjectGeneratorUI` |
| 全局修改 / 跨多個專案 | `Stackdose.UI.Core.sln` | — |

---

## 方案專案清單

### 核心庫（穩定）
| 專案 | 路徑 | 說明 |
|---|---|---|
| `Stackdose.UI.Core` | `./Stackdose.UI.Core/` | 26 個 WPF 元件（20 UserControl + 6 Window，含 2 個 Feature/ 進階）、Context 系統、SQLiteLogger |
| `Stackdose.UI.Templates` | `./Stackdose.UI.Templates/` | 16 個 Shell/Page 元件（MainContainer / SinglePageContainer、AppHeader / LeftNav / BottomBar、6 Pages） |
| `Stackdose.App.ShellShared` | `./Stackdose.App.ShellShared/` | 多App共用Shell服務層 |

### 框架與應用
| 專案 | 路徑 | 說明 | 狀態 |
|---|---|---|---|
| `Stackdose.App.DeviceFramework` | `./Stackdose.App.DeviceFramework/` | JSON驅動設備App組裝框架 | 穩定 |
| `Stackdose.App.UbiDemo` | `./Stackdose.App.UbiDemo/` | UBI工業烤箱參考實作 | 維護 |
| `Stackdose.App.DesignRuntime` | `./Stackdose.App.DesignRuntime/` | 真實PLC連線 + JSON載入執行專案 | **開發中** |

### 工具
| 專案 | 路徑 | 說明 | 狀態 |
|---|---|---|---|
| `Stackdose.Tools.MachinePageDesigner` | `./Stackdose.Tools.MachinePageDesigner/` | 自由畫布拖曳頁面設計器（輸出 .machinedesign.json） | **主力開發** |
| `Stackdose.Tools.DesignViewer` | `./Stackdose.Tools.DesignViewer/` | 拖入 JSON 即時預覽畫布 | **開發中** |
| `Stackdose.Tools.ProjectGenerator` | `./Stackdose.Tools.ProjectGenerator/` | CLI 一鍵建立新設備 App | 穩定 |
| `Stackdose.Tools.ProjectGeneratorUI` | `./Stackdose.Tools.ProjectGeneratorUI/` | GUI 版本專案產生器（WPF） | 穩定 |

### 測試
| 專案 | 路徑 |
|---|---|
| `Stackdose.UI.Core.Tests` | `./Stackdose.UI.Core.Tests/` |

---

## 外部依賴（跨 Repo）

### Stackdose.Platform（原始碼 ProjectReference）
**路徑：`../Stackdose.Platform/`**（與本 Repo 同層目錄）

| Platform 專案 | UI.Core 引用方 | 說明 |
|---|---|---|
| `Stackdose.Abstractions` | UI.Core、DeviceFramework | `IPlcManager`、`IPlcClient`、`IPlcMonitor`、`IPrintHead`、`ILogService` — 所有介面定義 |
| `Stackdose.Core` | UI.Core | enums、MachineState、Context 基礎工具 |
| `Stackdose.Hardware` | UI.Core | PLC 連線實作（FX3U 系列） |
| `Stackdose.PrintHead` | UI.Core | Feiyang PrintHead 控制實作 |
| `Stackdose.Plc` | DeviceFramework | PLC 輪詢實作 |

> **跨 Repo 危險介面**：改動 `IPlcManager`、`IPlcMonitor` 簽名會直接導致 UI.Core 多處編譯失敗。
> 詳見 `docs/kb/platform-contracts.md`。

### FeiyangWrapper（C++ 原生 DLL，條件引用）
**路徑：`../../Sdk/FeiyangWrapper/`**

- Debug：`FeiyangWrapper/x64/Debug/FeiyangWrapper.dll`
- Release：`FeiyangWrapper/x64/Release/FeiyangWrapper.dll`
- SDK 依賴：`../../Sdk/FeiyangSDK-2.3.1/lib/`
- 引用方式：csproj 條件 `Exists(...)` 判斷，Debug 優先，兩者都沒有則不引用（不報錯，但功能缺失）

---

## AI 行為規則

### 優先順序（衝突時依此裁決）
1. **穩定性** — 不破壞現有功能
2. **正確性** — 行為符合規格
3. **最小改動** — 只動必須動的
4. **可讀性** — 清楚優於聰明
5. **重構** — 只在被要求時才做

### 核心開發準則 (Mandatory)
1.  **先問後動 (Ask Before Act)：** 涉及 `Stackdose.UI.Core\Controls` 下的**現有控制項修改**或**新增自定義控制項**，必須先提供設計提案並徵得使用者同意。嚴禁擅自更動核心屬性、XAML 結構或公用介面。
2.  **不隨便 commit/push：** 除非使用者明確要求，否則不執行提交與推送。
3.  **保持解耦：** 優先使用 JSON 驅動邏輯，避免在核心控制項中寫入特定 App 的業務邏輯。

### 核心架構規則（絕對禁止）
1. 不在 `Controls/*.xaml` 寫硬編碼色碼，用語意 Token（`Surface.*`、`Text.*`、`Action.*`）
2. 不繞過 `ComplianceContext` 散落寫日誌，關閉前必須呼叫 `ComplianceContext.Shutdown()`
3. App 特屬邏輯不耦合進 `UI.Core`
4. 編譯失敗先確認 `../Stackdose.Platform/` 各專案與 `FeiyangWrapper.dll` 是否存在
5. 改動 `IPlcManager` / `IPlcMonitor` / `IPrintHead` 簽名前必須讀 `docs/kb/platform-contracts.md`

### 資安規則
詳見 `SECURITY_RULES.md`。AI 工具分級使用規則見 `AI_USAGE_POLICY.md`。

---

## 知識庫與文件索引

| 文件 | classification | 說明 |
|---|---|---|
| `STATUS.md` | Internal | **唯一動態狀態來源**，每次任務後更新 |
| `SECURITY_RULES.md` | Internal | AI 使用資安限制、高風險操作清單 |
| `AI_USAGE_POLICY.md` | Internal | AI 工具分級使用政策 |
| `docs/RAG_INDEX.md` | Internal | **RAG 導航地圖** — 場景→文件映射，AI 找路必讀 |
| `docs/kb/DECISION_LOG.md` | Internal | 架構決策紀錄（ADR）— 能/不能做 X 的根據 |
| `docs/kb/GLOSSARY.md` | Internal | 技術術語表 — 專有名詞定義與能力邊界 |
| `docs/kb/AI_REPRODUCTION_GUIDE.md` | Internal | AI 驗證指南 — 無實機環境下可驗證的項目與步驟 |
| `docs/specs/DATA_DICTIONARY.md` | **Confidential** | PLC Tag 字典 — Local RAG only，不可外傳 |
| `docs/PROJECT_MAP.md` | Internal | 完整專案依賴圖（含版本與路徑） |
| `docs/kb/architecture.md` | Internal | 架構設計、Context 系統、資料流 |
| `docs/kb/designer-system.md` | Internal | MachinePageDesigner + DesignViewer + DesignRuntime |
| `docs/kb/platform-contracts.md` | Internal | Platform 跨 Repo 契約文件（危險介面清單） |
| `docs/kb/controls-reference.md` | Internal | 控制項快速參考 |
| `docs/kb/quickstart.md` | Internal | 新 App 快速建立指南（CLI 指令說明） |
| `docs/devlog/2026-04.md` | Internal | 2026年4月開發日誌 |
| `docs/devlog/2026-05.md` | Internal | 2026年5月開發日誌 |
| `docs/eval/modele-vs-framework.md` | Internal | ModelE WinForms vs WPF框架功能對照與差距分析 |
| `docs/refactor/PROGRESS.md` | Internal | B0–B10 重構歷史紀錄（已完成，不再更新） |
| `docs/refactor/HANDOFF.md` | Internal | ⚠️ 歷史交接文件（2026-04-24），已過期 |
| `docs/MANAGER_BRIEF.md` | Internal | ⚠️ 已過期（2026-04-15），參考用 |
