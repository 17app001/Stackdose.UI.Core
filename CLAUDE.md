# CLAUDE.md — Stackdose.UI.Core 快速上下文

> 每次對話開始時讀這份文件。讀完即可掌握全局狀況。

---

## 專案定位

**一句話：讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 稽核要求的監控介面。**

### 核心主旨

工業設備廠商的 UI 開發痛點有三：
1. **PLC 整合複雜** — 每個設備的 Modbus / FX3U 地址不同，用程式碼寫很慢
2. **FDA 合規負擔重** — 21 CFR Part 11 要求完整的操作日誌、使用者驗證、稽核追蹤
3. **介面維護成本高** — 設計師改版型需要找工程師，工程師不懂設計工具

Stackdose.UI.Core 的解法：
- **MachinePageDesigner**：類 Unity 的拖曳設計工具，設計師直接擺控制項、綁 PLC 地址，輸出 `.machinedesign.json`
- **DesignPlayer**：量產 Shell App，讀取 JSON 後直接部署，無需寫程式碼
- **DeviceFramework**：更進一步的 JSON 驅動框架，適合複雜設備邏輯
- **全架構內建** FDA 21 CFR Part 11（日誌、稽核、使用者權限）

### 使用場景

| 需求 | 解法 |
|---|---|
| 設計師快速做單頁機台監控介面 | MachinePageDesigner → DesignPlayer（單頁模式） |
| 複雜設備需要多頁面導航 | MachinePageDesigner（多頁面） → DesignPlayer（頁籤/頁面切換） |
| 工程師需要客製化設備邏輯 | DeviceFramework + JSON 設定 |
| 上線前驗證 PLC 地址正確性 | DesignRuntime（即時 PLC 連線 + 熱更新） |

### 技術定位
企業級 WPF 框架（.NET 8 Windows-only，x64）。**框架產品**，非單一應用。
Core / Templates 是基礎庫，DeviceFramework 是組裝框架，App.* 是範例，Tools.* 是開發工具。

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
| `Stackdose.App.DesignPlayer` | `./Stackdose.App.DesignPlayer/` | 可交付量產 Shell App，JSON 驅動 + PLC 連線 + 登入管控 | **開發中** |

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

### 已完成
- **MachinePageDesigner** — FreeCanvas、Snap、Z-Order、框選、鎖定、複製貼上、GroupBox、對齊分配、多頁面（v2.0 JSON）、頁籤列、Undo/Redo per page
- **DesignRuntime** — PLC 連線、模擬器、熱更新、斷線重連、**多頁面切換（pages 陣列 + 頁籤列）**

### 已完成（續）
- **DesignViewer** — 拖入 JSON 靜態預覽 + 多頁面切換
- **DesignPlayer** — 多頁面切換、真實 PLC 連線驗證（192.168.22.39:3000 ✓）、修復 StartupUri 崩潰 bug
- **SecuredButton 擴充** — writeValue / pulse / toggle / sequence 四種 commandType，PropertyPanel 完整 UI，修復 commandAddress 讀取 bug
- **AlarmViewer 設計器整合** — PropertyPanel 行內編輯器（新增/刪除/修改），存檔自動輸出 alarms.json
- **SensorViewer 設計器整合** — 同 AlarmViewer 架構，支援 AND/OR/COMPARE 模式設定
- **控制項模板庫（Template Gallery）** — 9 個內建模板（溫度/壓力/計數器/啟停/急停/閥門/總覽/日誌警報），ToolboxPanel 雙頁籤，拖曳放置，自訂模板儲存
- **多步驟指令序列（Command Sequence DSL）** — JSON 驅動 PLC 多步驟指令：write/read/wait/conditional/readWait，支援變數、條件分支、Rollback、FDA 稽核日誌

### 已知功能缺口（待補強）

> 底層架構存在，但設計師或工程師使用上有缺口

| 項目 | 狀態 | 說明 |
|---|---|---|
| **AlarmViewer** | ✅ 已補強 | MachinePageDesigner 內建行內編輯器，存檔自動輸出 alarms.json |
| **SensorViewer** | ✅ 已補強 | 同 AlarmViewer 架構，支援 AND/OR/COMPARE 模式設定，存檔自動輸出 sensors.json |
| **SecuredButton** | ✅ 已補強 | 支援 writeValue / pulse / toggle / sequence 四種 commandType |
| **控制項模板庫** | ✅ 已完成 | MachinePageDesigner 內建 Template Gallery，支援分類篩選、搜尋、拖曳放置、自訂模板 |
| **工程師複雜框架** | ✅ 已完成 | Command Sequence DSL：JSON 定義多步驟指令，DesignPlayer/DesignRuntime 均支援執行 |

**alarms.json 格式：**
```json
{ "alarms": [
  { "group": "馬達", "device": "D200", "bit": 0, "operationDescription": "馬達過載" }
] }
```

**sensors.json 格式（扁平陣列）：**
```json
[
  { "group": "粉槽狀態", "device": "D90", "bit": "2,3", "value": "0,0", "mode": "AND", "operationDescription": "粉槽_B無粉" }
]
```

**Command Sequence DSL 格式：**
```json
{
  "steps": [
    { "type": "write", "address": "D100", "value": 1 },
    { "type": "wait", "ms": 500 },
    { "type": "read", "address": "D101", "variable": "status" },
    { "type": "conditional", "variable": "status", "operator": "==", "value": 1,
      "then": [{ "type": "write", "address": "D102", "value": 1 }],
      "else": [{ "type": "write", "address": "D103", "value": 0 }] },
    { "type": "readWait", "address": "D104", "expected": 1, "timeoutMs": 5000, "pollIntervalMs": 200 }
  ],
  "rollback": [
    { "type": "write", "address": "D100", "value": 0 }
  ],
  "onError": "rollback"
}
```
步驟類型：`write`（寫入）、`read`（讀取存變數）、`wait`（延遲）、`conditional`（條件分支）、`readWait`（輪詢等待）。
`onError` 策略：`rollback`（執行回滾步驟）、`stop`（立即停止）、`continue`（忽略繼續）。

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
| `docs/kb/deviceframework-guide.md` | DeviceFramework 新人指南（設定格式、擴充順序、常見問題） |
| `docs/kb/quickstart.md` | 新 App 快速建立指南（CLI 指令說明） |
| `docs/kb/second-app-quickstart.md` | 第二個 App 整合進 Shell 的步驟 |
| `docs/kb/design-standard.md` | Core UI 設計標準 |
| `docs/kb/theme-token-standard.md` | 主題 Token 收斂規範（控制項色碼規則） |
| `docs/devlog/2026-04.md` | 2026年4月開發日誌 |
