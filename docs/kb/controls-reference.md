# 控制項快速參考

> Stackdose.UI.Core 所有自定義 WPF 控制項的職責與使用摘要。

---

## 基類

| 類別 | 位置 | 說明 |
|---|---|---|
| `CyberControlBase` | `Controls/` | 所有控制項基類，處理主題、WeakReference 登錄 |
| `PlcControlBase` | `Controls/` | PLC 控制項基類，處理 `PlcContext` 附加屬性綁定與 `ScanUpdated` 訂閱 |

---

## PLC 顯示控制項

| 控制項 | 說明 | 主要屬性 |
|---|---|---|
| `PlcLabel` | PLC 數值顯示標籤，支援色彩主題、外框形狀、除數換算 | `Address`, `Label`, `ColorTheme`, `Shape`, `Divisor`, `DefaultValue` |
| `PlcText` | PLC 文字顯示（字串型數值） | `Address`, `Label` |
| `PlcStatus` | PLC 連線狀態顯示元件（含自動連線能力） | `IpAddress`, `Port`, `AutoConnect`, `IsGlobal`, `ScanInterval` |
| `PlcStatusIndicator` | 狀態指示燈（位元 ON/OFF） | `Address`, `TrueColor`, `FalseColor` |
| `PlcEventTrigger` | PLC 位元觸發事件（監聽地址變化觸發 C# 事件） | `Address`, `EventName`, `EventTriggered` |
| `PlcDeviceEditor` | PLC 裝置位址編輯器（讀寫測試用） | — |
| `PlcDataGridPanel` | 多筆 Labels 批次顯示面板（抽出自 DynamicDevicePage） | `Labels`（`DeviceLabelViewModel` 集合） |
| `ProcessStatusIndicator` | 製程狀態指示燈（多狀態顯示） | `State`, `StateColors` |

---

## 硬體控制項

| 控制項 | 說明 |
|---|---|
| `PrintHeadController` | Feiyang PrintHead 完整控制面板（連線、電源、列印） |
| `PrintHeadPanel` | PrintHead 狀態顯示面板 |
| `SensorViewer` | 感測器列表（綁定 `SensorContext`） |
| `AlarmViewer` | 警報列表（綁定 `SensorContext` Alarm） |

---

## 安全與驗證控制項

| 控制項 | 說明 |
|---|---|
| `SecuredButton` | 需權限驗證的按鈕，點擊前呼叫 `SecurityContext.CheckAccess()` |
| `LoginDialog` | 登入對話框（支援 AD 與本機帳號） |
| `UserEditorDialog` | 使用者帳號編輯對話框 |
| `UserManagementPanel` | 使用者帳號管理面板（新增/編輯/刪除） |
| `GroupManagementDialog` | 使用者群組管理對話框 |

---

## 日誌 UI 控制項

| 控制項 | 說明 |
|---|---|
| `LiveLogViewer` | 即時日誌顯示（綁定 `ComplianceContext.LiveLogs`） |
| `LogManagementPanel` | 歷史日誌查詢面板（搜尋、過濾、匯出） |

---

## 通用 UI 控制項

| 控制項 | 說明 |
|---|---|
| `CyberMessageBox` | 樣式化訊息對話框（取代原生 MessageBox） |
| `CyberFrame` | 樣式化容器框架 |
| `InputDialog` | 單一輸入對話框 |
| `BatchInputDialog` | 多欄位批次輸入對話框 |
| `InfoLabel` | 非PLC資訊顯示標籤（靜態文字，非即時更新） |

## 進階功能控制項

| 控制項 | 說明 |
|---|---|
| `RecipeLoader` | 配方載入控制項（`Controls/Feature/Recipe/`） |
| `SimulatorControlPanel` | PLC 模擬器控制面板（`Controls/Feature/Simulator/`，開發測試用） |

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
