# 控制項快速參考

> Stackdose.UI.Core + Stackdose.UI.Templates 所有 WPF 元件的職責與使用摘要。
>
> **範圍：** 本文件涵蓋 UI.Core 的 26 個控件/對話框 + UI.Templates 的 16 個 Shell/Page/Control 元件。
> **準確性：** 2026-04-21 重新對齊程式碼實況；完整 DP 清單與繼承關係另見 `docs/refactor/B0-control-inventory.md`。

---

## 基類現況（✅ B1 已完成遷移）

`Stackdose.UI.Core/Controls/Base/` 下三個基類，**PlcLabel / PlcText / PlcStatusIndicator / SensorViewer / AlarmViewer 已全數遷移**。

| 類別 | 位置 | 用途 |
|---|---|---|
| `CyberControlBase` | `Controls/Base/` | `UserControl + IThemeAware`；ThemeManager WeakRef 自動登錄、`OnThemeChanged` 虛擬方法 |
| `PlcControlBase` | `Controls/Base/` | 繼承 `CyberControlBase`；統一 PlcContext 訂閱、`ValueChanged` event、`OnPlcConnected/OnPlcDataUpdated` hook |
| `CyberTabControl` | `Controls/Base/` | 繼承 `TabControl`，主題支援 |

詳見 `docs/kb/foundation-base-classes.md`。

---

## UI.Core — PLC 相關控件

| 控制項 | 說明 | 常用屬性 / 事件 |
|---|---|---|
| `PlcLabel` | PLC 數值顯示標籤，支援色彩主題、外框形狀、除數換算、數字格式（22 個 DP） | 常用：`Address`、`Label`、`ColorTheme`、`Shape`、`Divisor`、`DefaultValue`、`Format`、`Prefix`、`Suffix`；事件：`ValueChanged` |
| `PlcText` | PLC 字串或多字元數值顯示（5 個 DP） | `Address`、`Label`；事件：`ValueApplied`（⚠️ 命名與 `PlcLabel.ValueChanged` 不一致，B1 統一） |
| `PlcStatus` | **PLC 連線單例**。其他 Plc* 控件訂閱它的 `ScanUpdated` 取值（9 個 DP） | `IsConnected`、`ScanInterval`；事件：`ConnectionEstablished`、`ScanUpdated` |
| `PlcStatusIndicator` | 狀態指示燈（位元） | `DisplayAddress` |
| `PlcEventTrigger` | bit 邊緣觸發事件廣播，透過 `PlcEventContext.NotifyEventTriggered` 對外發佈 | `Address`、`TriggerOn`、`EdgeType`（6 個 DP） |
| `PlcDeviceEditor` | PLC 讀寫測試 UI（7 個 DP） | — |
| `SensorViewer` | 感測器列表（綁 `SensorContext`） | — |
| `AlarmViewer` | 警報列表（綁 `SensorContext` Alarm） | — |

> **注意：** `PlcDataGridPanel` **不在** UI.Core，而在 `Stackdose.App.DeviceFramework/Controls/`，是 DeviceFramework 的延伸控件。

---

## UI.Core — 硬體控制項（Feiyang PrintHead）

| 控制項 | 說明 |
|---|---|
| `PrintHeadController` | Feiyang PrintHead 完整控制面板（連線、電源、列印） |
| `PrintHeadPanel` | PrintHead 狀態顯示面板 |
| `PrintHeadStatus` | PrintHead 連線狀態單例（4 DP）；事件：`ConnectionEstablished`、`ConnectionLost` |

---

## UI.Core — 安全與驗證控件

| 控制項 | 說明 |
|---|---|
| `SecuredButton` | 需權限驗證的按鈕（10 DP），點擊前呼叫 `SecurityContext.CheckAccess()` |
| `LoginDialog` | 登入對話框（AD + 本機） — `Window` |
| `UserEditorDialog` | 使用者帳號編輯對話框 — `Window` |
| `GroupManagementDialog` | 群組管理對話框 — `Window` |
| `UserManagementPanel` | 帳號管理 UI（UserControl，含 `INotifyPropertyChanged`） |

---

## UI.Core — 日誌 / 製程 UI

| 控制項 | 說明 |
|---|---|
| `LiveLogViewer` | 即時日誌顯示（綁 `ComplianceContext.LiveLogs`） |
| `LogManagementPanel` | 歷史日誌查詢面板（搜尋、過濾、匯出） |
| `ProcessStatusIndicator` | 非 PLC，製程動畫狀態燈（`ProcessState`、`BatchNumber`） |

---

## UI.Core — 通用 UI / 對話框

| 控制項 | 類型 | 說明 |
|---|---|---|
| `CyberFrame` | UserControl | 樣式化容器框架（14 DP，含時鐘、使用者狀態顯示） |
| `InfoLabel` | UserControl | 靜態資訊標籤（17 DP，非即時更新） |
| `CyberMessageBox` | Window | 樣式化訊息對話框（取代原生 MessageBox） |
| `InputDialog` | Window | 單一輸入對話框 |
| `BatchInputDialog` | Window | 多欄位批次輸入對話框 |

---

## UI.Core — 進階功能（Feature/ 子目錄）

| 控制項 | 路徑 | 說明 |
|---|---|---|
| `RecipeLoader` | `Controls/Feature/Recipe/` | 配方（Recipe）載入介面（5 DP） |
| `SimulatorControlPanel` | `Controls/Feature/Simulator/` | PLC 模擬器控制面板 |

---

## UI.Core — 控件合計

| 類別 | 數量 |
|---|---|
| UserControl（含 Plc* / 硬體 / 日誌 / 通用 / 進階） | 20 |
| Window（對話框） | 6 |
| **UI.Core 總計** | **26** |

---

## UI.Templates — Shell 容器（2）

| 控件 | DP / Events | 用途 |
|---|---|---|
| `MainContainer` | 13 DP；`NavigationRequested`、`LogoutRequested`、`CloseRequested`、`MinimizeRequested`、`MachineSelectionRequested` | 完整 Shell：AppHeader + LeftNavigation + AppBottomBar + ShellContent |
| `SinglePageContainer` | 6 DP；`LogoutRequested`、`CloseRequested`、`MinimizeRequested` | 簡化 Shell：單頁面無 LeftNav |

> **使用方式：** DesignRuntime 根據 `shellMode`（FreeCanvas / SinglePage / Standard）自動選擇 Shell 策略包裝畫布。Standard + `pages[]` 時 `MainContainer` 自動接線 LeftNavigation。

---

## UI.Templates — Shell 組件（7）

| 控件 | DP | Events |
|---|---|---|
| `AppHeader` | 12 | `LogoutClicked`、`MinimizeClicked`、`CloseClicked`、`SwitchUserClicked`、`FullscreenClicked`、`MachineSelectionChanged` |
| `LeftNavigation` | 3 | `NavigationRequested` |
| `AppBottomBar` | 0 | — |
| `MachineCard` | 18 | `MachineSelected` |
| `GroupBoxBlock` | 4 | — |
| `PanelBlock` | 3 | — |
| `SystemClock` | 4 | — |

---

## UI.Templates — Pages（6）

| 頁面 | 用途 |
|---|---|
| `BasePage` | 基底頁面（其他頁繼承） |
| `MachineDetailPage` | 單機詳情 — `StartRequested`、`StopRequested`、`ResetRequested` |
| `MachineOverviewPage` | 多機概覽 — `PlcScanUpdated`、`MachineSelected` |
| `SingleDetailWorkspacePage` | 單機工作區 — `SecuredSampleButtonClicked` |
| `LogViewerPage` | 日誌檢視 |
| `UserManagementPage` | 帳號管理 |

---

## UI.Templates — Converter

| 名稱 | 說明 |
|---|---|
| `FirstCharConverter` | 顯示字串首字元（`IValueConverter`） |

---

## UI.Templates — 合計

| 類別 | 數量 |
|---|---|
| Shell 容器 | 2 |
| Shell 組件 | 7 |
| Pages | 6 |
| Converters | 1 |
| **UI.Templates 總計** | **16** |

---

## 全專案 UI 元件合計

**UI.Core 26 + UI.Templates 16 = 42 個 UI 元件**

> CLAUDE.md 的「26 個 WPF 控制項」指 UI.Core 範圍，未含 Templates。

---

## 主題 Token 規則

控制項 XAML 中**只能**使用語意 Token，不能寫 `#RRGGBB` 硬編碼色碼：

| Token 前綴 | 用途 |
|---|---|
| `Surface.*` | 背景色（`Surface.Primary`, `Surface.Secondary`, `Surface.Card`...） |
| `Text.*` | 文字色（`Text.Primary`, `Text.Secondary`, `Text.Disabled`...） |
| `Action.*` | 按鈕/操作色（`Action.Primary`, `Action.Danger`...） |
| `Border.*` | 邊框色 |
| `Status.*` | 狀態色（`Status.Success`, `Status.Warning`, `Status.Error`...） |

Dark/Light 字典必須同步維護。

---

## 延伸閱讀

- [`docs/refactor/B0-control-inventory.md`](../refactor/B0-control-inventory.md) — 所有控件完整 DP + 繼承關係 + B1 遷移優先度
- [`docs/refactor/B0-findings.md`](../refactor/B0-findings.md) — 文件與程式碼差異修正紀錄
- [`docs/kb/architecture.md`](architecture.md) — Context 系統、資料流
- [`docs/kb/theme-token-standard.md`](theme-token-standard.md) — 主題 Token 規範詳本
