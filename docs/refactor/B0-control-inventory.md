# B0 控件實際現況盤點

> 產出時間：2026-04-21
> 來源：直接讀取 `*.xaml.cs` 檔案，不依賴任何 kb/ 文件描述
> 用途：B1 抽基類前的準確地圖；如與 kb/ 衝突，**以本份為準**

---

## 一句話摘要

**基類已寫好但 0 個控件使用**。`Stackdose.UI.Core/Controls/Base/` 下有完整實作的 `CyberControlBase` / `PlcControlBase` / `CyberTabControl`，還附 `MIGRATION_GUIDE.md`，但 24 個 Core 控件 + 17 個 Templates 控件**全部仍直接繼承 UserControl / Window**。這是個已規劃未執行的半成品重構。

---

## 1. 基類層 — `Stackdose.UI.Core/Controls/Base/`

| 檔案 | 型態 | 繼承 | 職責 |
|---|---|---|---|
| `CyberControlBase.cs` | abstract | `UserControl, IThemeAware, IDisposable` | lifecycle (Loaded/Unloaded virtual hooks)、ThemeManager 自動註冊/註銷、SafeInvoke/SafeBeginInvoke helper、DesignMode 檢測、IDisposable 清理 |
| `PlcControlBase.cs` | abstract | `CyberControlBase` | 加上 `PlcManager` DP、`TargetStatus` DP、`OnPlcConnected/OnPlcDataUpdated` virtual hooks、`GetPlcManager/IsPlcConnected` helper、自動訂閱/退訂 PlcStatus 的 `ConnectionEstablished/ScanUpdated` 事件 |
| `CyberTabControl.cs` | concrete | `TabControl` | 暫未調查用途（待 B1 時補） |
| `MIGRATION_GUIDE.md` | doc | — | 記錄了「如何遷移 PlcText / PlcLabel / PlcStatus」的步驟，但實際未執行。**文件內容編碼亂碼（BIG5/UTF-8 問題）** |
| `QUICK_REFERENCE.md` | doc | — | 基類 API 速查卡，**同樣亂碼** |

**備註**：基類檔案本身的 **程式碼是可讀的**，只有 XML 註解亂碼。

---

## 2. Core 控件繼承樹（實際）

### 2.1 繼承 UserControl 的控件（18 個）

| 控件 | 路徑 | DP 數 | Events | 附加介面 | PLC 相關 |
|---|---|---|---|---|---|
| `PlcLabel` | `Controls/` | 22 | **`ValueChanged`** | `IThemeAware` | 自己訂 PlcContext |
| `PlcText` | `Controls/` | 5 | **`ValueApplied`** ⚠️命名不一致 | — | 自己訂 PlcContext |
| `PlcStatus` | `Controls/` | 9 | **`ConnectionEstablished`、`ScanUpdated`** | `IDisposable` | PLC 連線單例（其他控件訂閱它） |
| `PlcStatusIndicator` | `Controls/` | 1 (`DisplayAddress`) | — | — | 訂 `PlcContext.GlobalStatus.ScanUpdated` |
| `PlcEventTrigger` | `Controls/` | 6 | — | — | bit edge 觸發 → `PlcEventContext.NotifyEventTriggered` |
| `PlcDeviceEditor` | `Controls/` | 7 | — | — | 讀寫測試 |
| `SensorViewer` | `Controls/` | 8 | — | — | 訂 ScanUpdated，掃 bit |
| `AlarmViewer` | `Controls/` | 6 | — | — | 訂 ScanUpdated，掃 alarm bit |
| `PrintHeadController` | `Controls/` | 1 | — | — | Feiyang PrintHead |
| `PrintHeadPanel` | `Controls/` | 2 | — | — | PrintHead 顯示 |
| `PrintHeadStatus` | `Controls/` | 4 | `ConnectionEstablished`、`ConnectionLost` | — | PrintHead 連線 |
| `ProcessStatusIndicator` | `Controls/` | 2 (`ProcessState`、`BatchNumber`) | — | — | 非 PLC，動畫狀態燈 |
| `SecuredButton` | `Controls/` | 10 | `Click` | — | 非 PLC，含 SecurityContext 驗證 |
| `CyberFrame` | `Controls/` | 14 | — | — | 內建時鐘 + 使用者狀態 |
| `LiveLogViewer` | `Controls/` | 2 | — | `IThemeAware` | 日誌 UI |
| `LogManagementPanel` | `Controls/` | 0 | `PropertyChanged` | `INotifyPropertyChanged` | 歷史日誌查詢 |
| `UserManagementPanel` | `Controls/` | 0 | `PropertyChanged` | `INotifyPropertyChanged` | 帳號管理 |
| `InfoLabel` | `Controls/` | 17 | — | — | 純靜態文字 |

### 2.2 繼承 Window 的對話框（6 個）

| 對話框 | 路徑 | 附加介面 |
|---|---|---|
| `LoginDialog` | `Controls/` | — |
| `CyberMessageBox` | `Controls/` | — |
| `InputDialog` | `Controls/` | — |
| `BatchInputDialog` | `Controls/` | — |
| `UserEditorDialog` | `Controls/` | — |
| `GroupManagementDialog` | `Controls/` | 內嵌 `GroupItem : INotifyPropertyChanged` |

### 2.3 進階功能（2 個，在 Feature/ 子目錄）

| 控件 | 路徑 | DP 數 |
|---|---|---|
| `RecipeLoader` | `Controls/Feature/Recipe/` | 5 |
| `SimulatorControlPanel` | `Controls/Feature/Simulator/` | 0 |

### 2.4 資料模型（非控件）

- `PlcValueChangedEventArgs`（`PlcLabel.xaml.cs` line 36）
- `ValueAppliedEventArgs`（`PlcText.xaml.cs` line 436）
- `AlarmItem : INotifyPropertyChanged`（`AlarmViewer.xaml.cs`）
- `NavigationItem : INotifyPropertyChanged`（`LeftNavigation.xaml.cs`）

### 2.5 關鍵 Context（`Stackdose.UI.Core/Helpers/`）

| Context | 說明 | 用途 |
|---|---|---|
| `PlcContext` | 全域 PLC 實例 + 附加屬性 | `GlobalStatus`、`GetStatus(dp)` |
| `PlcEventContext` | **PlcEventTrigger 專用**事件匯流排 | `Register/Unregister/NotifyEventTriggered` |
| `SecurityContext` | 登入/權限 | `Login`、`CheckAccess` |
| `ComplianceContext` | FDA 合規日誌 | `LogAuditTrail`、`LogOperation`、`LogEvent`、`LogPeriodicData`、`Shutdown` |
| `SensorContext` | 感測器配置 | 配置載入、警報狀態 |
| `ThemeManager` | 主題切換（在 `Services/`） | `Register(IThemeAware)` |

**架構觀察**：`PlcEventContext` 已是匯流排雛形，但只服務 `PlcEventTrigger`（bit edge）。B2 目標就是升級它。

---

## 3. Templates 層盤點 — `Stackdose.UI.Templates/`

### 3.1 Shell（2 個，策略化關鍵）

| 控件 | DP 數 | Events | 用途 |
|---|---|---|---|
| `MainContainer` | 13 | `NavigationRequested`、`LogoutRequested`、`CloseRequested`、`MinimizeRequested`、`MachineSelectionRequested` | **完整 Shell**：AppHeader + LeftNavigation + AppBottomBar + ShellContent |
| `SinglePageContainer` | 6 | `LogoutRequested`、`CloseRequested`、`MinimizeRequested` | **簡化 Shell**：單頁面無 LeftNav |

**B3 關鍵**：目前 Dashboard 模式在 DesignPlayer 自己 hardcode 視窗樣式，沒用 `MainContainer` 或 `SinglePageContainer`。要抽 `IShellStrategy` 需把 `MainContainer` 視為 "StandardShell" 實作。

### 3.2 Controls（7 個）

| 控件 | DP 數 | Events |
|---|---|---|
| `AppHeader` | 12 | `LogoutClicked`、`MinimizeClicked`、`CloseClicked`、`SwitchUserClicked`、`FullscreenClicked`、`MachineSelectionChanged`、`PropertyChanged` |
| `LeftNavigation` | 3 | `NavigationRequested`、`PropertyChanged`（內嵌 `NavigationItem`） |
| `AppBottomBar` | 0 | — |
| `MachineCard` | 18 | `MachineSelected` |
| `GroupBoxBlock` | 4 | — |
| `PanelBlock` | 3 | — |
| `SystemClock` | 4 | — |

### 3.3 Pages（6 個）

| 頁面 | DP 數 | Events | 用途 |
|---|---|---|---|
| `BasePage` | 7 | — | 基底頁面 |
| `MachineDetailPage` | 8 | `StartRequested`、`StopRequested`、`ResetRequested` | 單機詳情 |
| `MachineOverviewPage` | — | `PlcScanUpdated`、`MachineSelected` | 多機概覽 |
| `SingleDetailWorkspacePage` | — | `SecuredSampleButtonClicked` | 單機工作區 |
| `LogViewerPage` | — | — | 日誌檢視 |
| `UserManagementPage` | — | — | 帳號管理 |

### 3.4 Converters

- `FirstCharConverter : IValueConverter`

---

## 4. 統計總覽

| 類別 | 數量 | 備註 |
|---|---|---|
| Core 基類 | 3 | CyberControlBase / PlcControlBase / CyberTabControl（**已寫未用**） |
| Core 控件（UserControl） | 18 | 全部直接繼承 UserControl |
| Core 對話框（Window） | 6 | 全部直接繼承 Window |
| Core 進階功能 | 2 | Feature/Recipe/ + Feature/Simulator/ |
| Core 合計 | **26 + 3 基類** | 符合「26 個控件」宣稱 |
| Templates Shell | 2 | MainContainer + SinglePageContainer |
| Templates Controls | 7 | AppHeader 最複雜（12 DP + 7 events） |
| Templates Pages | 6 | — |
| Templates Converters | 1 | — |
| **全專案總計** | **48 個 UI 元件** | — |

---

## 5. PLC 事件資料流（實際）

```
PlcStatus（連線單例）
  └── 每個 ScanInterval 讀 PLC
        ├── 觸發 ScanUpdated 事件
        └── 訂閱者：
              ├── PlcLabel（update display, 觸發自己的 ValueChanged）
              ├── PlcText（refresh value）
              ├── PlcStatusIndicator（update light）
              ├── SensorViewer（掃 sensor bits）
              └── AlarmViewer（掃 alarm bits）

PlcEventTrigger
  └── 監聽特定 bit address
        └── 邊緣觸發時：
              └── PlcEventContext.NotifyEventTriggered(this, value)
                    └── 外部訂閱者透過 PlcEventContext.RegisterHandler 接收
```

**缺口**（B2 要補）：
1. **`PlcLabel.ValueChanged` 是孤島** — 沒進 `PlcEventContext`，外部只能 code-behind 訂閱
2. **沒有「值區間條件觸發」**（只有 bit edge）
3. **沒有統一 Subscribe API** — `PlcEventContext` 只服務 `PlcEventTrigger`

---

## 6. B1 遷移優先順序建議

依據：**PLC 事件邏輯最複雜 → 最受益於基類**。

| 優先 | 控件 | 基類 | 受益 |
|---|---|---|---|
| P1 | `PlcText` | `PlcControlBase` | 30 行訂閱邏輯可刪 |
| P1 | `PlcLabel` | `PlcControlBase` | 40 行訂閱邏輯可刪；`ValueChanged` 下放到基類 |
| P1 | `PlcStatusIndicator` | `PlcControlBase` | 20 行 |
| P1 | `AlarmViewer` | `PlcControlBase` | 25 行 |
| P1 | `SensorViewer` | `PlcControlBase` | 25 行 |
| P2 | `PlcEventTrigger` | `PlcControlBase`（但邏輯特殊，考慮不遷） | 改與 PlcEventContext 整合；B2 一起處理 |
| P2 | `PlcStatus` | 不遷（它是 PLC 連線單例，不是 consumer） | — |
| P3 | `CyberFrame` | `CyberControlBase` | Loaded/Unloaded lifecycle 可統一 |
| P3 | `LiveLogViewer` | `CyberControlBase` | IThemeAware 自動處理 |
| P3 | `SecuredButton` | `CyberControlBase` | lifecycle 可統一，但 Click event 邏輯要保留 |
| 跳過 | 6 個 Dialog 對話框 | 不遷（Window 不適用 UserControl 基類） | 未來可考慮另開 `CyberDialogBase : Window` |
| 跳過 | PrintHead 3 個 | 不遷（Feiyang 特殊邏輯，風險高） | — |

**B1 驗收底線**：P1 全部完成、P2/P3 依時間決定、跳過類標註 `B1-skipped.md` 說明。

---

## 7. Events 命名不一致清單（B1 要統一）

| 控件 | 現有事件 | 建議統一名 | 理由 |
|---|---|---|---|
| `PlcLabel` | `ValueChanged : EventHandler<PlcValueChangedEventArgs>` | ✅ 保留（即標準名） | 已是合理命名 |
| `PlcText` | `ValueApplied : EventHandler<ValueAppliedEventArgs>` | ⚠️ 改為 `ValueChanged` 或新增 `ValueChanged` 與 `ValueApplied` 並存 | ValueApplied 是「使用者按 Apply」；ValueChanged 才是「PLC 值變」 |
| `PlcStatus` | `ScanUpdated : Action<IPlcManager>` | 保留（PLC 單例專用） | 不是控件值變化 |
| `PrintHeadStatus` | `ConnectionEstablished`、`ConnectionLost` | 保留 | 不同語意 |

**決策**：B1 新增 `PlcControlBase.ValueChanged` 作為**統一控件值變化事件**（統一 `EventArgs`）；`PlcText.ValueApplied` 保留作「使用者送出」事件。

---

## 8. 備註 / 待查項

- [ ] `CyberTabControl.cs` 用途未調查（B1 再看）
- [ ] `MIGRATION_GUIDE.md` / `QUICK_REFERENCE.md` 亂碼——B8 統一修或刪
- [ ] `MachineOverviewPage` / `SingleDetailWorkspacePage` / `LogViewerPage` / `UserManagementPage` DP 具體數未盤——但未列入 B1 遷移（非 PLC 控件），視需要再補
