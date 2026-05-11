---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
last_updated: 2026-05-11
source_of_truth: false
---

# 技術術語表（Glossary）

> 本文件定義 Stackdose.UI.Core 框架的專有名詞，並標明每個術語的**能力邊界**。  
> **RAG 用途：** 當 AI 被問「X 能做什麼」或「X 是什麼」，先查這裡。  
> 每個條目的「❌ 不能」欄位是防止 AI 越界的關鍵。

---

## 快速索引

| 分類 | 術語 |
|---|---|
| 控件基類 | [CyberControlBase](#cybercontrolbase)、[PlcControlBase](#plccontrolbase) |
| 靜態 Context | [PlcContext](#plccontext)、[SecurityContext](#securitycontext)、[ComplianceContext](#compliancecontext)、[SensorContext](#sensorcontext)、[PlcEventContext](#plceventcontext) |
| 核心控件 | [PlcStatus](#plcstatus)、[PlcLabel](#plclabel)、[PlcText](#plctext)、[PlcStatusIndicator](#plcstatusindicator)、[SensorViewer](#sensorviewer)、[AlarmViewer](#alarmviewer)、[SecuredButton](#securedbutton)、[Spacer](#spacer) |
| 系統服務 | [ThemeManager](#thememanager)、[SqliteLogger](#sqlitelogger)、[BehaviorEventBus](#behavioreventbus) |
| Behavior Engine | [BehaviorEngine](#behaviorengine)、[BehaviorHandler](#behaviorhandler) |
| 設計器系統 | [MachinePageDesigner](#machinepagedesigner)、[DesignViewer](#designviewer)、[DesignRuntime](#designruntime)、[FreeCanvas](#freecanvas) |
| Shell 系統 | [IShellStrategy](#ishellstrategy)、[RuntimeControlFactory](#runtimecontrolfactory)、[DesignTimeControlFactory](#designtimecontrolfactory) |
| 平台介面 | [IPlcManager](#iplcmanager)、[IPlcMonitor](#iplcmonitor)、[IPrintHead](#iprinthead) |
| 資料格式 | [machinedesign.json](#machinedesignjson)、[DesignerItemDefinition](#designeritemdefinition)、[PageDefinition](#pagedefinition) |

---

## 控件基類

### CyberControlBase

**位置：** `Stackdose.UI.Core/Controls/Base/CyberControlBase.cs`

**是什麼：** 所有 UI.Core WPF 控件的頂層基類，繼承自 `UserControl`。

**能做：**
- 自動向 `ThemeManager` 以 `WeakReference` 登錄，主題切換時收到 `OnThemeChanged(ResourceDictionary)` 回呼
- 管理 `Loaded` / `Unloaded` 生命週期勾子

**❌ 不能：**
- 不含任何 PLC 資料訂閱邏輯（那是 `PlcControlBase` 的職責）
- 不可直接存取 `PlcContext`（應交給子類決定）

---

### PlcControlBase

**位置：** `Stackdose.UI.Core/Controls/Base/PlcControlBase.cs`

**是什麼：** 所有需要顯示 PLC 資料的控件的基類，繼承自 `CyberControlBase`。

**能做：**
- 統一訂閱 `PlcContext.GlobalStatus` → `ScanUpdated` 事件，子類無需手動訂閱
- 提供 `Address` DP（共用 PLC 位址屬性）
- 子類覆寫 `OnPlcConnected(IPlcManager)` / `OnPlcDataUpdated(IPlcManager)` 處理資料
- `RaiseValueChanged(...)` 同時更新 `CurrentValue` 並觸發 `ValueChanged` 事件
- 每次 `ValueChanged` 自動呼叫 `PlcEventContext.PublishControlValueChanged()`，供 BehaviorEngine 接收

**❌ 不能：**
- 不可直接訂閱 `IPlcMonitor.WordChanged / BitChanged`（ADR-004 禁令）
- 不可在控件內建立獨立輪詢 Timer

---

## 靜態 Context

### PlcContext

**位置：** `Stackdose.UI.Core/Helpers/PlcContext.cs`

**是什麼：** 全域 PLC 實例管理器，靜態存取，框架採 Context Manager Pattern（非 DI）。

**能做：**
- `PlcContext.GlobalStatus` 取得全域 `IPlcManager` 實例
- `PlcManager` WPF 附加屬性讓子控件在控件樹中取得 Manager，無需知道父層
- App 啟動時設定一次，後續所有控件自動取用

**❌ 不能：**
- 不可改成 Interface + Injection 形式（ADR-001）
- 不可把靜態呼叫包成 ViewModel 屬性綁定

---

### SecurityContext

**位置：** `Stackdose.UI.Core/Helpers/SecurityContext.cs`

**是什麼：** 使用者登入/登出、權限檢查、自動登出計時的靜態管理器。

**能做：**
- `Login(username, password)` — AD 或本機 SQLite 驗證
- `CheckAccess(requiredLevel)` — 檢查當前使用者是否有足夠權限
- `StartAutoLogoutTimer()` — 閒置自動登出
- 存取等級：`Guest < Operator < Instructor < Supervisor < Admin < SuperAdmin`

**❌ 不能：**
- 不可繞過 SecurityContext 直接操作使用者資料
- `CheckAccess` 必須在每個寫入操作前呼叫，不能假設已通過驗證

---

### ComplianceContext

**位置：** `Stackdose.UI.Core/Helpers/ComplianceContext.cs`

**是什麼：** FDA 21 CFR Part 11 合規日誌的統一入口，底層由 `SqliteLogger` 非同步落盤。

**能做：**
- `LogAuditTrail(action, detail)` — 稽核軌跡（強制記錄）
- `LogOperation(action, detail)` — 操作記錄
- `LogEvent(eventName, detail)` — 系統事件
- `LogPerfodicData(data)` — 週期性製程數據
- `Shutdown()` — 強制 flush 佇列，App 關閉前必須呼叫

**❌ 不能：**
- 不可直接 `new SqliteLogger()` 或呼叫 `_logger.EnqueueAsync()`（ADR-005）
- 不可省略 `Shutdown()`，否則最後 5 秒資料遺失（ADR-010）

---

### SensorContext

**位置：** `Stackdose.UI.Core/Helpers/SensorContext.cs`

**是什麼：** 感測器配置載入與警報狀態追蹤的靜態管理器。

**能做：**
- 載入感測器設定（上下限、單位、警報等級）
- 供 `SensorViewer` 查詢當前警報狀態

**❌ 不能：**
- 不直接連 PLC，感測器數值仍由 `PlcStatus.ScanUpdated` 推送

---

### PlcEventContext

**位置：** `Stackdose.UI.Core/Helpers/PlcEventContext.cs`

**是什麼：** 控件事件統一匯流排，BehaviorEngine 的事件來源。

**能做：**
- `ControlValueChanged` 靜態事件 — BehaviorEngine 訂閱此事件取得所有控件值變化
- `PublishControlValueChanged(element, args)` — 由 `PlcControlBase` 內部呼叫，一般不需手動觸發
- bit edge 觸發（`PlcEventTrigger` 使用，向後相容）

**❌ 不能：**
- App 層不應直接訂閱 `ControlValueChanged`，應透過 BehaviorEngine 的 events[] JSON 設定行為

---

## 核心控件

### PlcStatus

**位置：** `Stackdose.UI.Core/Controls/PlcStatus.xaml.cs`

**是什麼：** PLC 連線單例控件，放在 XAML 樹頂層，其他所有 Plc* 控件透過它的 `ScanUpdated` 事件取值。

**能做：**
- 橋接 `IPlcMonitor.WordChanged / BitChanged` → 觸發 `PlcStatus.ScanUpdated`
- 管理 PLC 連線生命週期

**❌ 不能：**
- 不可建立多個 `PlcStatus` 實例（ADR-006）
- 不可移除 XAML 中的 `<controls:PlcStatus>` 改用程式碼動態建立

---

### PlcLabel

**位置：** `Stackdose.UI.Core/Controls/PlcLabel.xaml.cs`

**是什麼：** 顯示 PLC Word 暫存器數值的唯讀標籤控件，繼承自 `PlcControlBase`。

**能做：**
- 設定 `Address`（D暫存器位址）、`Label`、`Unit`、`ColorTheme`
- 支援 `events[]` 觸發 BehaviorEngine 動作

**❌ 不能：**
- 不直接寫入 PLC（唯讀）
- 不自行訂閱 `IPlcMonitor`

---

### PlcText

**位置：** `Stackdose.UI.Core/Controls/PlcText.xaml.cs`

**是什麼：** 可輸入數值並寫入 PLC 的文字輸入控件，繼承自 `PlcControlBase`。

**能做：**
- 讀取與寫入 PLC Word 位址
- 輸入驗證（上下限）

---

### PlcStatusIndicator

**位置：** `Stackdose.UI.Core/Controls/PlcStatusIndicator.xaml.cs`

**是什麼：** 顯示 PLC Bit 開關狀態的指示燈控件，繼承自 `PlcControlBase`。

**能做：**
- 設定 ON/OFF 對應的顏色與文字
- 支援 `events[]` 觸發行為

---

### SensorViewer

**位置：** `Stackdose.UI.Core/Controls/SensorViewer.xaml.cs`

**是什麼：** 帶警報門檻視覺化的感測器顯示控件，繼承自 `PlcControlBase`。

---

### AlarmViewer

**位置：** `Stackdose.UI.Core/Controls/AlarmViewer.xaml.cs`

**是什麼：** 顯示警報清單的控件，訂閱 `PlcStatus.ScanUpdated`，繼承自 `PlcControlBase`。

**❌ 不能：**
- 不可跨 Page 共用同一個 `AlarmViewer` 實例（B5 已確認作用域為 Page 層）

---

### SecuredButton

**位置：** `Stackdose.UI.Core/Controls/SecuredButton.xaml.cs`

**是什麼：** 帶權限檢查的按鈕，點擊前會呼叫 `SecurityContext.CheckAccess()`，不繼承 `PlcControlBase`。

**能做：**
- 設定 `RequiredLevel` 屬性控制最低存取等級
- 點擊時透過 `BehaviorEventBus.RaiseControlEventFired(id, "clicked", 0)` 廣播事件供 BehaviorEngine 處理

**❌ 不能：**
- 不直接讀取 PLC 位址（不是 PLC 資料控件）

---

### Spacer

**位置：** 定義在 `machinedesign.json` 的 `type: "Spacer"`，由各 App 的 `RuntimeControlFactory` 渲染為 GroupBox

**是什麼：** 設計器中的分組容器控件，可設定標題顏色、顯示/隱藏 Header。

**能做：**
- `headerColor` prop：`"Normal"`（灰）、`"Primary"`（藍）、`"Success"`（綠）、`"Warning"`（橙）、`"Error"`（紅）
- `showTitle` prop：是否顯示 Header 標題列
- 背景使用 `Surface.Bg.Card` 語意 Token，跟隨主題切換

**❌ 不能：**
- 不可在 XAML 中硬編碼顏色（ADR-007）
- `headerColor` 預設值必須是 `"Normal"`（灰），不能是 `"Primary"`（藍），否則所有 Spacer 全部顯示藍色

---

## 系統服務

### ThemeManager

**位置：** `Stackdose.UI.Core/Services/ThemeManager.cs`

**是什麼：** 動態主題切換管理器（Dark/Light），維護 `WeakReference<IThemeAware>` 登錄表。

**能做：**
- `ThemeManager.Switch(ThemeMode.Dark / Light)` — 切換全域主題
- 自動通知所有已登錄的 `CyberControlBase` 子類

**❌ 不能：**
- 不可繞過 Token 系統直接在 XAML 寫 `#RRGGBB`（ADR-007）
- 新增 Token 時必須同步 `DarkColors.xaml` + `LightColors.xaml` 兩份（ADR-012）

---

### SqliteLogger

**位置：** `Stackdose.UI.Core/Services/SqliteLogger.cs`

**是什麼：** 非同步批次 SQLite 寫入器，5 秒 flush 一次，是 `ComplianceContext` 的底層。

**❌ 不能：**
- 不可直接呼叫（ADR-005），必須透過 `ComplianceContext`

---

### BehaviorEventBus

**位置：** `Stackdose.UI.Core/Helpers/BehaviorEventBus.cs`

**是什麼：** `SecuredButton` 點擊事件到 BehaviorEngine 的橋接靜態類別。

**能做：**
- `RaiseControlEventFired(controlId, "clicked", value)` — 由 SecuredButton 呼叫，BehaviorEngine 接收

**❌ 不能：**
- 不可取代 `PlcEventContext.ControlValueChanged`，兩者用途不同

---

## Behavior Engine

### BehaviorEngine

**位置：** `Stackdose.App.ShellShared/Services/BehaviorEngine.cs`（**不在 UI.Core**）

**是什麼：** JSON 驅動的觸發反應引擎，讓設計師不寫 C# 就能設定控件行為。

**能做：**
- `BindDocument(PageDefinition)` — 載入頁面的 events[] 設定
- 訂閱 `PlcEventContext.ControlValueChanged` 收取所有控件事件
- 評估 `when` 條件，依序執行 `do[]` 動作
- `Navigate` action 需要 `Navigator` 注入（Standard 多頁模式）

**❌ 不能：**
- 不可移進 `UI.Core`（ADR-008，BehaviorEngine 依賴 Shell 的 Navigator，若放在 UI.Core 會造成反向依賴）

---

### BehaviorHandler

**是什麼：** BehaviorEngine 的 action 執行單元，目前 6 個內建 Handler：

| Handler | action 名稱 | 說明 |
|---|---|---|
| SetPropHandler | `setProp` | 反射修改目標控件屬性 |
| WritePlcHandler | `writePlc` | 寫入 PLC 位址 |
| LogAuditHandler | `logAudit` | 寫 ComplianceContext 稽核日誌 |
| ShowDialogHandler | `showDialog` | 顯示 CyberMessageBox |
| NavigateHandler | `navigate` | 切換頁面（需 Navigator 注入） |
| SetStatusHandler | `setStatus` | 設定語意狀態 |

**❌ 不能：**
- 新增 Handler 應在 `ShellShared` 加，不在 `UI.Core`（ADR-008）

---

## 設計器系統

### MachinePageDesigner

**位置：** `Stackdose.Tools.MachinePageDesigner/`  
**方案：** `Stackdose.Designer.sln`

**是什麼：** 自由畫布拖曳頁面設計器，輸出 `.machinedesign.json`。

**能做：**
- 拖曳、Snap 對齊、框選、鎖定、複製貼上、Z-Order、GroupBox、Undo/Redo
- 設定每個控件的 `props`、`events[]`
- 輸出 `PageDefinition` JSON

**❌ 不能：**
- 設計器不連接真實 PLC，顯示的是 `props.defaultValue`，不是即時值
- 不可把 App 特屬邏輯寫進設計器（ADR-003）

---

### DesignViewer

**位置：** `Stackdose.Tools.DesignViewer/`

**是什麼：** 拖入 `.machinedesign.json` 即時渲染預覽工具，不連接 PLC。

**能做：**
- 渲染 JSON 定義的控件佈局
- 使用 `DesignTimeControlFactory` 建立控件（設計時版本）

---

### DesignRuntime

**位置：** `Stackdose.App.DesignRuntime/`

**是什麼：** 連接真實 PLC 並載入 JSON 的執行環境，同時也是框架功能驗證用的主力測試 App。

**能做：**
- 載入 `.machinedesign.json` 並連接真實 PLC 顯示即時值
- 使用 `RuntimeControlFactory`（DesignRuntime 版）建立控件
- 注入 `Navigator` 給 BehaviorEngine 支援 `Navigate` action

**❌ 不能：**
- 不可把 App 特屬業務邏輯放進 DesignRuntime（ADR-003）

---

### FreeCanvas

**是什麼：** MachinePageDesigner v2.0 的設計模式，允許控件被放置在畫布的任意座標（x, y），取代舊的 Zone 制固定格位。

**JSON 標識：** `"shellMode": "FreeCanvas"`

---

## Shell 系統

### IShellStrategy

**位置：** `Stackdose.App.ShellShared/Services/IShellStrategy.cs`

**是什麼：** Shell 容器策略介面，DesignRuntime 根據 JSON 的 `shellMode` 自動選擇實作。

| 實作 | shellMode | 說明 |
|---|---|---|
| `FreeCanvasShellStrategy` | `"FreeCanvas"` | 裸畫布，DesignRuntime 預設 |
| `SinglePageShellStrategy` | `"SinglePage"` | 單頁，`SinglePageContainer` |
| `StandardShellStrategy` | `"Standard"` | 多頁，`MainContainer` + LeftNav |

---

### RuntimeControlFactory

**位置：** 各 App 各有一份副本（DesignRuntime / MyPrintApp3 / DashboardTest1 / ModelE）

**是什麼：** JSON `type` 字串 → WPF 控件實例的工廠，Runtime（連接 PLC）版本。

**能做：**
- `Create(DesignerItemDefinition)` → 對應的 WPF 控件
- `CreateGroupBox(def, body)` → Spacer 容器，支援 `headerColor` 和 `showTitle` prop

**⚠️ 注意：**
- 各 App 為獨立副本，bug 修正需同步所有副本（ADR-009 技術債）
- `headerColor` 預設值必須是 `"Normal"`，不能是 `"Primary"`

**❌ 不能：**
- 不可把這個工廠改成單一共用版本放進 UI.Core（未完成的重構，需協調所有 App）

---

### DesignTimeControlFactory

**位置：** `Stackdose.Tools.MachinePageDesigner/` 或 `DesignViewer/`

**是什麼：** JSON `type` 字串 → WPF 控件實例的工廠，DesignTime（不連接 PLC）版本。顯示 `defaultValue`，外觀模擬。

**能做：**
- 提供設計器用的靜態預覽控件

**❌ 不能：**
- 不讀取 PLC 位址的即時值

---

## 平台介面

> ⚠️ 以下三個介面定義在 `../Stackdose.Platform/Stackdose.Abstractions/`，為跨 Repo 危險介面。

### IPlcManager

**是什麼：** PLC 管理器頂層介面，統一讀寫、連線管理。

**能做：**
- `GetWord(address)` / `GetBit(address)` — 同步讀值
- `WriteAsync(address, value)` — 非同步寫入
- 取得 `IPlcMonitor` 子介面

**❌ 不能：**
- 不可直接修改簽名（ADR-002）
- 修改前必須讀 `docs/kb/platform-contracts.md`

---

### IPlcMonitor

**是什麼：** PLC 輪詢監控介面，定期 BatchRead，觸發 `WordChanged / BitChanged` 事件。

**❌ 不能：**
- Plc* 控件不可直接訂閱此介面的事件（ADR-004），必須透過 `PlcStatus.ScanUpdated`
- 不可修改簽名（ADR-002）

---

### IPrintHead

**是什麼：** 噴頭控制介面，橋接 Feiyang C++ SDK（`FeiyangWrapper.dll`）。

**❌ 不能：**
- 不可修改簽名（ADR-002）
- C++ SDK 路徑：`../../Sdk/FeiyangWrapper/`，缺失時功能沉默失效（不報錯）

---

## 資料格式

### machinedesign.json

**副檔名：** `.machinedesign.json`

**是什麼：** MachinePageDesigner 輸出的頁面設計檔，由 DesignViewer / DesignRuntime 載入執行。

**結構：**
```
{
  "version": "2.0",
  "shellMode": "FreeCanvas" | "SinglePage" | "Standard",
  "meta": { "title": "...", "machineId": "..." },
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "canvasItems": [ DesignerItemDefinition, ... ],
  "pages": [ PageDefinition, ... ]   ← Standard 模式才有
}
```

---

### DesignerItemDefinition

**是什麼：** `canvasItems[]` 中每個控件的完整定義：

```json
{
  "id": "a1b2c3d4",
  "type": "PlcLabel",
  "x": 100, "y": 200,
  "width": 120, "height": 80,
  "order": 0,
  "locked": false,
  "props": { "label": "溫度", "address": "D100", ... },
  "events": [ BehaviorEvent, ... ]
}
```

---

### PageDefinition

**是什麼：** Standard 多頁模式下 `pages[]` 中每個頁面的定義，包含 `pageId`、`title`、`canvasItems[]`（等同 DesignerItemDefinition）。

**用途：** BehaviorEngine 的 `BindDocument(PageDefinition)` 參數。

---

## 主題 Token 前綴規則

| 前綴 | 語意 | 範例 |
|---|---|---|
| `Surface.*` | 背景色 | `Surface.Bg.Card`、`Surface.Bg.Panel` |
| `Text.*` | 文字色 | `Text.Primary`、`Text.Muted` |
| `Action.*` | 按鈕/操作色 | `Action.Primary`、`Action.Danger` |
| `Border.*` | 邊框色 | `Border.Default`、`Border.Focus` |
| `Status.*` | 狀態色 | `Status.Error`、`Status.Warning`、`Status.Success` |

**使用規則：**
- XAML 裡一律用 `{StaticResource Token.Name}`，禁止 `#RRGGBB`（ADR-007）
- 新增或改名 Token 必須同步 `DarkColors.xaml` + `LightColors.xaml`（ADR-012）
