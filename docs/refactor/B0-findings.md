# B0 文件 vs 實際差異表

> 產出時間：2026-04-21
> 對照來源：[`B0-control-inventory.md`](B0-control-inventory.md)（實況）vs 以下 kb/ 文件
> 用途：B0.4 / B0.5 / B0.6 的修正依據

---

## 0. 檢查範圍

| 被檢查文件 | 路徑 | 是否存在 |
|---|---|---|
| 控件參考 | `docs/kb/controls-reference.md` | ✅ |
| 架構 | `docs/kb/architecture.md` | ✅ |
| 專案地圖 | `docs/PROJECT_MAP.md` | ✅ |
| 首頁 | `index.html` | ❌ **不存在**（HANDOFF/PLAN 寫要更新但根目錄沒這檔案） |

---

## 1. 高嚴重度差異（文件描述與實作矛盾）

| # | 來源 | 文件說 | 實際 | 誰該改 | 怎麼改 |
|---|---|---|---|---|---|
| **F1** | `controls-reference.md` §基類 | `CyberControlBase` / `PlcControlBase` 是「所有控制項基類」「PLC 控制項基類」（暗示 Plc* 系列繼承 PlcControlBase） | 基類在 `Controls/Base/` 已寫好且有 `MIGRATION_GUIDE.md`，但**沒有任何控件繼承之**。所有 Plc* 仍 `: UserControl` | B0.4 改 `controls-reference.md` | 基類表格加一欄「狀態：存在但尚未被控件繼承（B1 規劃中）」；移除「所有控制項基類」這類混淆語意 |
| **F2** | `architecture.md` §6 控制項基類體系 | ```CyberControlBase → PlcControlBase → PlcLabel / PlcText / PlcStatus / PlcStatusIndicator``` | **完全不符**。實際繼承：`PlcLabel : UserControl, IThemeAware`、`PlcText : UserControl`、`PlcStatus : UserControl`、`PlcStatusIndicator : UserControl` | B0.5 改 `architecture.md` | 整段 §6 改寫為「規劃中的基類體系（B1）」，並註記目前實況（`UserControl + IThemeAware`） |
| **F3** | `controls-reference.md` §PLC 顯示控制項 | 列出 `PlcDataGridPanel`（位置隱含在 UI.Core） | 實際在 `Stackdose.App.DeviceFramework/Controls/PlcDataGridPanel.xaml`，**不屬於 UI.Core** | B0.4 改 `controls-reference.md` | 從 UI.Core 表格移除；或另闢「DeviceFramework 延伸控件」區註明 |
| **F4** | `architecture.md` §2 Static Context 系統 | 列了 5 個 Context：PlcContext / SecurityContext / ComplianceContext / SensorContext / ThemeManager | **遺漏 `PlcEventContext`**（`Helpers/PlcEventContext.cs`，已是事件匯流排雛形，B2 要升級它） | B0.5 改 `architecture.md` | §2 新增 2.6 `PlcEventContext` 子節；標註「目前僅服務 PlcEventTrigger」 |

---

## 2. 中嚴重度差異（屬性/數量誤植）

| # | 來源 | 文件說 | 實際 | 誰該改 | 怎麼改 |
|---|---|---|---|---|---|
| **F5** | `controls-reference.md` §PLC 顯示控制項 → `PlcLabel` | 主要屬性：`Address, Label, ColorTheme, Shape, Divisor, DefaultValue`（6 個） | 實際 **22 個 DP**（含 Format、Prefix/Suffix、FontFamily、DigitCount 等） | B0.4 改 `controls-reference.md` | 註記「常用屬性」並加「完整 22 DP 見 `B0-control-inventory.md`」 |
| **F6** | `controls-reference.md` §PLC 顯示控制項 → `PlcStatus` | 主要屬性：`IsConnected, ScanInterval`（2 個） | 實際 **9 個 DP**；且事件 `ConnectionEstablished` / `ScanUpdated` 被其他控件訂閱，是 PLC 單例角色，文件未提 | B0.4 改 `controls-reference.md` | 補事件說明「此控件是 PLC 連線單例；其他 Plc* 控件訂閱它的 `ScanUpdated` 取值」 |
| **F7** | `controls-reference.md` §硬體控制項 | 僅列 `PrintHeadController` / `PrintHeadPanel` | 遺漏 **`PrintHeadStatus`**（4 DP、`ConnectionEstablished` / `ConnectionLost` 事件） | B0.4 改 `controls-reference.md` | 硬體控制項區新增 `PrintHeadStatus` 一列 |
| **F8** | `controls-reference.md` 整份 | 標題「**26 個自定義控制項**」（CLAUDE.md 也同樣數字） | 實際 **Core 26 = 18 UserControl + 6 Window + 2 Feature/**；Templates 另有 16 個（2 Shell + 7 Controls + 6 Pages + 1 Converter） | B0.4 改 `controls-reference.md` + B0.5 改 `CLAUDE.md` 專案列 | 改為「Core 26 個 + Templates 16 個 = 全專案 42 UI 元件」；或維持 26 並標註「僅 UI.Core」 |
| **F9** | `controls-reference.md` §通用 UI 控制項 | 有 `CyberFrame` 但未列屬性 | `CyberFrame` 14 DP（含時鐘、使用者狀態） | B0.4 改 `controls-reference.md` | 補主要屬性（Title、ShowClock、ShowUserStatus、ContentArea 等） |
| **F10** | `controls-reference.md` 整份 | 未列出 `LogManagementPanel` / `UserManagementPanel` 為獨立條目 | 兩者均為 `Controls/` 下的 UserControl + `INotifyPropertyChanged` | B0.4 改 `controls-reference.md` | 「日誌 UI 控制項」補 `LogManagementPanel`；新增「帳號管理 UI」或併入通用 UI 列 `UserManagementPanel` |
| **F11** | `controls-reference.md` 整份 | 未列 `PlcEventTrigger` / `PlcDeviceEditor` / `ProcessStatusIndicator` / `RecipeLoader` / `SimulatorControlPanel` | 全部實際存在 | B0.4 改 `controls-reference.md` | 依序補入：PlcEventTrigger（事件觸發器）、PlcDeviceEditor（讀寫測試）、ProcessStatusIndicator（動畫狀態燈）、Feature/ 下兩個（配方載入 / 模擬器） |

---

## 3. Templates 層缺漏（文件幾乎沒提）

| # | 來源 | 文件說 | 實際 | 誰該改 | 怎麼改 |
|---|---|---|---|---|---|
| **F12** | `controls-reference.md` 整份 | 只寫「UI.Core 控制項」，**完全未提 UI.Templates 的 16 個元件** | `MainContainer` / `SinglePageContainer` / `AppHeader` / `LeftNavigation` / `AppBottomBar` / `MachineCard` / `GroupBoxBlock` / `PanelBlock` / `SystemClock` / 6 Pages / 1 Converter | B0.4 改 `controls-reference.md` | 新增「Templates 層元件」一整章；或另開 `templates-reference.md` 放 `docs/refactor/` 待 B8 回灌 |
| **F13** | `architecture.md` §4 Shell 導航系統 | 列了 `ShellRouteCatalog` / `NavigationOrchestrator` / `IShellAppProfile` / `ShellNavigationService` | 未提 Templates Shell 的具體實作（`MainContainer` / `SinglePageContainer`）、未說 DesignPlayer Dashboard 模式目前 **不使用** Templates Shell 而是自己 hardcode | B0.5 改 `architecture.md` | §4 補子節「Shell 實作現況」：列出 2 個 Container；註記 Dashboard 模式尚未整合（B3 目標） |
| **F14** | `PROJECT_MAP.md` 依賴圖 | 列出 `Stackdose.App.DesignPlayer` 嗎？**沒列！** | `Stackdose.App.DesignPlayer` 實際存在且引用 Core + Templates + ShellShared + DeviceFramework | B0.5 改 `PROJECT_MAP.md` | 依賴圖新增 `DesignPlayer` 區塊（同 DesignRuntime 位階） |

---

## 4. 輕微差異（命名 / 排版 / 備註）

| # | 來源 | 文件說 | 實際 | 誰該改 | 怎麼改 |
|---|---|---|---|---|---|
| **F15** | 多處 | `PlcText` 有 `ValueApplied` 事件但命名與 `PlcLabel.ValueChanged` 不一致 | — | 不是文件錯，是**程式碼設計不一致**，B1 時統一處理 | 本 findings 列入「待 B1 修」清單即可 |
| **F16** | `architecture.md` §5 資料流 PLC 輪詢 | `IPlcMonitor.Start() → WordChanged / BitChanged 事件 → PlcLabel / PlcStatus 控制項訂閱` | 實際路徑是 `PlcStatus.ScanUpdated` → 其他 Plc* 控件訂閱（WordChanged / BitChanged 是 IPlcMonitor 層事件，控件並未直接接這兩個） | B0.5 改 `architecture.md` | 補一層：IPlcMonitor 事件 → `PlcStatus` 單例橋接 → `ScanUpdated` → 其他 Plc* |
| **F17** | `controls-reference.md` 整份 | 排序依「類型」（PLC/硬體/安全/日誌/通用） | 無 B1 基類歸類欄、無「P1/P2/P3 遷移優先度」 | B0.4 改 `controls-reference.md` | 可選：每個表加「B1 遷移優先度」欄，或在段末加連結到 `B0-control-inventory.md §6` |
| **F18** | `HANDOFF.md` / `PLAN.md` 提到 `index.html` | 「同步更新 index.html」 | **專案根目錄沒有 index.html**（grep 全域無此檔） | B0.6 決策 | 二擇一：(a) 移除所有 index.html 相關指示；(b) 若過去存在但被刪，記錄在 findings 並問用戶 |

---

## 5. `CLAUDE.md` 層級的校正（建議 B0.5 順便做）

| # | 來源 | 文件說 | 實際 | 誰該改 | 怎麼改 |
|---|---|---|---|---|---|
| **F19** | `CLAUDE.md` §專案與方案表 | `Stackdose.UI.Core`：**26 個 WPF 控制項** | 精準應該寫 `18 UserControl + 6 Dialog + 2 Feature` | B0.5 改 `CLAUDE.md` | 改為「26 個 WPF 元件（含 6 對話框、2 進階功能）」 |
| **F20** | `CLAUDE.md` §外部依賴 vs `PROJECT_MAP.md` | CLAUDE.md 寫 UI.Core 引用 4 個（Abstractions/Core/Hardware/PrintHead）+ Plc 歸給 DeviceFramework | **已驗**：`Stackdose.UI.Core.csproj` 確實只引用這 4 個。CLAUDE.md 與 PROJECT_MAP.md 均正確。不需改 | — | ✅ 無動作 |

---

## 6. 修正計畫矩陣（B0.4 / B0.5 / B0.6 的依據）

| 階段 | 檔案 | 須處理差異編號 |
|---|---|---|
| B0.4 | `docs/kb/controls-reference.md` | F1, F3, F5, F6, F7, F8, F9, F10, F11, F12, F17 |
| B0.5 | `docs/kb/architecture.md` | F2, F4, F13, F16 |
| B0.5 | `docs/PROJECT_MAP.md` | F14 |
| B0.5 | `CLAUDE.md`（根目錄） | F8, F19 |
| B0.6 | `CURRENT_FOCUS.md` | 寫「重構中，指向 docs/refactor/」 |
| B0.6 | `docs/devlog/2026-04.md` | 附 4/21 重構啟動條目 |
| B0.6 | `index.html` | **F18：不存在，暫不處理**；若用戶要建新的另案 |
| B1 待辦 | 程式碼層 | F15（事件命名）、F1/F2 的實質遷移 |

---

## 7. 不修的東西（刻意不動）

- `MIGRATION_GUIDE.md` / `QUICK_REFERENCE.md` 亂碼：B8 統一處理
- `DeviceFramework-Guide.md` 亂碼：B8 / 或延到未來
- `DynamicDevicePage.xaml` 硬編碼色碼：已在 `architecture.md §7 已知技術債`，不屬 B0 範圍
- DeviceFramework / DesignPlayer 其他未驗證的細節：超出 B0（B0 只盤點 UI.Core + Templates）

---

## 8. 驗收標準（B0.4–B0.6 完成才勾）

- [ ] `controls-reference.md` 所有 F1/F3/F5–F12/F17 都處理
- [ ] `architecture.md` §2 補 PlcEventContext、§6 基類體系改寫、§4 補 Shell 實作現況、§5 PLC 輪詢路徑修正
- [ ] `PROJECT_MAP.md` 補 DesignPlayer
- [ ] `CLAUDE.md` 專案描述對齊
- [ ] `CURRENT_FOCUS.md` 指向 `docs/refactor/`
- [ ] `devlog/2026-04.md` 有 4/21 重構啟動條目
- [ ] 本檔 §7 不修項目列表保持不變（避免誤動）
