# CLAUDE.md — Stackdose.UI.Core 快速上下文

> 每次對話開始時讀這份文件。讀完即可掌握全局狀況。

---

## 專案定位

企業級 WPF 工業設備 UI 框架（.NET 8 Windows-only，x64），目標讓設備廠商快速建立符合 **FDA 21 CFR Part 11** 稽核要求的操作介面。
這是**框架產品**，不是單一應用。Core / Templates 是基礎庫，DeviceFramework 是組裝框架，App.* 是範例，Tools.* 是開發工具。

---

## 方案專案清單

### 核心庫（穩定）
| 專案 | 路徑 | 說明 |
|---|---|---|
| `Stackdose.UI.Core` | `./Stackdose.UI.Core/` | 26個自定義WPF控制項、Context管理、SQLiteLogger |
| `Stackdose.UI.Templates` | `./Stackdose.UI.Templates/` | Shell布局：AppHeader、LeftNavigation、AppBottomBar |
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

1. **MachinePageDesigner（自由畫布設計器）** — 已完成 FreeCanvas 模式、Snap、Z-Order、框選、鎖定、複製貼上、GroupBox、對齊分配
2. **DesignRuntime** — 真實 PLC 連線執行環境，有未提交變更（`MainWindow.xaml` / `MainWindow.xaml.cs`）
3. **DesignViewer** — 拖入 JSON 即時預覽工具

---

## 核心架構規則（勿違反）

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
| `docs/kb/controls-reference.md` | 控制項快速參考（26個，含 PLC/安全/日誌/通用/進階） |
| `docs/kb/quickstart.md` | 新 App 快速建立指南（CLI 指令說明） |
| `docs/kb/second-app-quickstart.md` | 第二個 App 整合進 Shell 的步驟 |
| `docs/kb/design-standard.md` | Core UI 設計標準 |
| `docs/kb/theme-token-standard.md` | 主題 Token 收斂規範（控制項色碼規則） |
| `docs/devlog/2026-04.md` | 2026年4月開發日誌 |
