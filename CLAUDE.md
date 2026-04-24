# CLAUDE.md — Stackdose.UI.Core 快速上下文

> 每次對話開始時讀這份文件。讀完即可掌握全局狀況。

---

## 現狀三行摘要

- **分支：** `feature/printhead-robustness`（未合 master）｜PrintHead 控件型別安全重構（2026-04-24）
- **主力工作：** PrintHead 整合關鍵環節，控件強化已完成，待進入實際硬體整合
- **未解問題：** Flash/Spit 邏輯仍在 PrintHeadPanel 與 PrintHeadController 各有一份（重複）；傳圖進度條尚未實作

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
- **未解問題：** 見 `docs/refactor/PROGRESS.md` 底部 ⚠️ 區塊；分支尚未合併至 master

---

## 開發狀態快速確認

> 當前進度以 `docs/refactor/PROGRESS.md` 為唯一真相。AI 接手必讀 `docs/refactor/HANDOFF.md`（含鐵律與用戶偏好）。

**常用方案 / Startup 對照**

| 工作內容 | 開啟方案 | Startup Project |
|---|---|---|
| 設計器 / 執行環境 / 預覽 | `Stackdose.Designer.sln` | `Stackdose.App.DesignRuntime` |
| 框架核心 / DeviceFramework | `Stackdose.UI.Core.sln` | `Stackdose.App.UbiDemo` |
| 專案產生器 | `Stackdose.UI.Core.sln` | `Stackdose.Tools.ProjectGeneratorUI` |
| 全局修改 / 跨多個專案 | `Stackdose.UI.Core.sln` | — |

---

## 專案定位

企業級 WPF 工業設備 UI 框架（.NET 8 Windows-only，x64），目標讓設備廠商快速建立符合 **FDA 21 CFR Part 11** 稽核要求的操作介面。
這是**框架產品**，不是單一應用。Core / Templates 是基礎庫，DeviceFramework 是組裝框架，App.* 是範例，Tools.* 是開發工具。

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
| `Stackdose.App.UbiDemo` | `./Stackdose.App.UbiDemo/` | UBI工業烤箱參考實作（已遷移至DeviceFramework架構） | 維護 |
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

## 目前主力開發方向

1. **MachinePageDesigner（自由畫布設計器）** — 已完成 FreeCanvas 模式、Snap、Z-Order、框選、鎖定、複製貼上、GroupBox、對齊分配、方向鍵微調、Dashboard PLC 欄位設定
2. **DesignRuntime** — 真實 PLC 連線執行環境，支援 FreeCanvas / SinglePage / Standard / **Dashboard** 四種 Shell 策略
3. **Shell 模式體系** — `DashboardShellStrategy`（精簡生產模式，畫布全螢幕貼合，自動連線 PLC）完成；`scripts/new-app.ps1 -Mode Dashboard` 可一鍵 scaffold
4. **DesignViewer** — 拖入 JSON 即時預覽工具

---

## AI 行為規則

### 優先順序（衝突時依此裁決）
1. **穩定性** — 不破壞現有功能
2. **正確性** — 行為符合規格
3. **最小改動** — 只動必須動的
4. **可讀性** — 清楚優於聰明
5. **重構** — 只在被要求時才做

### 每次任務回應格式
```
**摘要：** 做了什麼（一句話）
**異動檔案：** 列出所有改動的檔案
**改了什麼：** 具體變更內容
**為什麼：** 決策理由
**風險：** 可能的副作用或假設
**下一步：** 建議的後續動作
```

### 核心架構規則（勿違反）
1. 不在 `Controls/*.xaml` 寫硬編碼色碼，用語意 Token（`Surface.*`、`Text.*`、`Action.*`）
2. 不繞過 `ComplianceContext` 散落寫日誌，關閉前必須呼叫 `ComplianceContext.Shutdown()`
3. App 特屬邏輯不耦合進 `UI.Core`
4. 編譯失敗先確認 `../Stackdose.Platform/` 各專案與 `FeiyangWrapper.dll` 是否存在

---

## 知識庫與文件索引

| 文件 | 說明 |
|---|---|
| `docs/PROJECT_MAP.md` | 完整專案依賴圖（含版本與路徑） |
| `docs/kb/architecture.md` | 架構設計、Context 系統、資料流 |
| `docs/kb/designer-system.md` | MachinePageDesigner + DesignViewer + DesignRuntime |
| `docs/kb/platform-contracts.md` | Platform 跨 Repo 契約文件（危險介面清單） |
| `docs/kb/controls-reference.md` | 控制項快速參考 |
| `docs/kb/quickstart.md` | 新 App 快速建立指南（CLI 指令說明） |
| `docs/devlog/2026-04.md` | 2026年4月開發日誌 |
