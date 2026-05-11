---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
last_updated: 2026-05-11
source_of_truth: false
---

# 架構決策紀錄（ADR）

> 本文件記錄「為什麼這樣設計」與「為什麼不那樣做」。  
> **RAG 用途：** 當 AI 被問「能不能做 X」，先查這裡——有記錄的決策不需要重新討論。  
> 沒有記錄的新需求，才需要分析架構後補記。

---

## 快速查詢表

| ADR | 問題場景 | 答案 |
|---|---|---|
| ADR-001 | 能加 DI 容器嗎？ | ❌ 刻意不用，原因見下 |
| ADR-002 | 能改 IPlcManager / IPlcMonitor 簽名嗎？ | ⚠️ 極高風險，必讀 platform-contracts.md |
| ADR-003 | 能把 App 特屬邏輯放進 UI.Core 嗎？ | ❌ 架構禁令 |
| ADR-004 | 能讓 PlcLabel 直接訂閱 IPlcMonitor 嗎？ | ❌ 必須走 PlcStatus.ScanUpdated |
| ADR-005 | 能跳過 ComplianceContext 直接寫日誌嗎？ | ❌ 架構禁令，且會漏資料 |
| ADR-006 | 能改 PlcStatus 為非單例嗎？ | ❌ 所有 Plc* 控件依賴此單例 |
| ADR-007 | 能在 XAML 用 #RRGGBB 硬編碼色碼嗎？ | ❌ 架構禁令，必須用語意 Token |
| ADR-008 | BehaviorEngine 能移進 UI.Core 嗎？ | ❌ 刻意放在 ShellShared |
| ADR-009 | RuntimeControlFactory 能抽成共用基底嗎？ | ✅ 可以，但目前各 App 為副本隔離設計 |
| ADR-010 | 能不呼叫 ComplianceContext.Shutdown() 嗎？ | ❌ 5秒 buffer 資料會遺失 |
| ADR-011 | 能加自動化單元測試嗎？ | ⚠️ 有限制，WPF 控件需 UI thread |
| ADR-012 | 能改 Token 命名或移除現有 Token 嗎？ | ⚠️ 全域影響，必須同步 Dark/Light 兩份字典 |
| ADR-013 | 能在 machinedesign.json 硬編碼確認框嗎？ | ❌ 暫緩實作，且不應硬編碼在 UI JSON 中 |

---

## ADR-001：不使用 DI 容器（Static Context Manager Pattern）

**決策：** 框架採靜態 Context 存取（`PlcContext.GlobalStatus`、`SecurityContext.CurrentUser`），不引入 DI 容器。

**原因：**
- 工業設備 UI 的全域狀態（PLC 連線、使用者登入、稽核日誌）需要跨控制項樹隨時存取
- WPF Code-behind 中 DI 注入成本高（控件無法像 Service 一樣被注入）
- 靜態存取犧牲可測試性，但換取開發速度——符合此類快速交付專案的實際需求

**AI 不應做的事：**
- 不要主動建議「改用 DI 重構」
- 不要把 Context 改成 Interface + Injection 形式
- 不要把靜態 Context 呼叫包成 ViewModel 屬性綁定

**狀態：** Active（長期決策）

---

## ADR-002：跨 Repo 介面簽名不得擅改

**決策：** `IPlcManager`、`IPlcMonitor`、`IPrintHead`（定義在 `../Stackdose.Platform/`）的方法簽名需要跨 Repo 協調才能修改。

**原因：**
- 這三個介面是 Platform Repo 與 UI.Core Repo 的邊界
- 簽名任何變動會導致 UI.Core 多處編譯失敗（所有 PlcContext、PlcStatus、PrintHeadStatus 等都依賴）
- Platform Repo 可能還有其他未知消費者

**AI 不應做的事：**
- 不要直接修改這三個介面的方法簽名或參數型別
- 修改前必須讀 `docs/kb/platform-contracts.md` 確認影響範圍

**狀態：** Active（跨 Repo 約束）

---

## ADR-003：App 特屬邏輯不耦合進 UI.Core

**決策：** UI.Core 只放通用控件與 Context 服務。特定機台、特定設備、特定 App 的邏輯只能放在 `Stackdose.App.*` 層。

**原因：**
- UI.Core 是共用框架基礎庫，若混入 App 邏輯，所有 App 都會受影響
- 保持 UI.Core 穩定，讓它可以被多個 App 引用而不相互干擾

**AI 不應做的事：**
- 不要把 ModelE 特有邏輯、UbiDemo 邏輯、MyPrintApp 邏輯寫進 `Stackdose.UI.Core/`
- 不要在 `UI.Core` 裡 `using Stackdose.App.XXX`

**狀態：** Active（架構禁令）

---

## ADR-004：Plc* 控件必須透過 PlcStatus.ScanUpdated 取值，不直接訂閱 IPlcMonitor

**決策：** `PlcLabel`、`PlcText`、`PlcStatusIndicator`、`SensorViewer`、`AlarmViewer` 訂閱 `PlcStatus.ScanUpdated`，而不是直接訂閱 `IPlcMonitor.WordChanged / BitChanged`。

**原因：**
- PlcStatus 是連線單例，負責中繼 PLC 輪詢結果
- 所有控件統一走同一個事件，避免各自維護輪詢 Timer（原本 BitIndicator 有 N 個各自計時器的問題，B1/B2 修正）
- PlcStatus 負責連線狀態管理，控件不需要知道底層是哪種 PLC

**AI 不應做的事：**
- 不要在 Plc* 控件裡直接 `IPlcMonitor.WordChanged += ...`
- 不要在控件裡建立獨立的輪詢 Timer

**狀態：** Active（B1/B2 重構確立）

---

## ADR-005：日誌必須走 ComplianceContext，不得直接操作 SqliteLogger

**決策：** 所有稽核、操作、事件日誌都必須透過 `ComplianceContext.LogXxx()` 寫入，不得直接呼叫 `SqliteLogger`。

**原因：**
- ComplianceContext 統一管理 FDA 21 CFR Part 11 合規格式
- 直接操作 SqliteLogger 會跳過合規欄位（操作者、時戳格式、類別）
- Behavior Engine 的 LogAudit action 也透過 ComplianceContext，確保 JSON-driven 行為的稽核紀錄一致

**AI 不應做的事：**
- 不要 `new SqliteLogger()` 或直接呼叫 `_logger.EnqueueAsync()`
- 關閉程式前必須呼叫 `ComplianceContext.Shutdown()`，否則 5 秒 buffer 資料遺失

**狀態：** Active（FDA 合規要求）

---

## ADR-006：PlcStatus 維持連線單例設計

**決策：** `PlcStatus` 控件作為全域 PLC 連線單例存在於 XAML 樹中，其他控件訂閱它的 `ScanUpdated` 事件。

**原因：**
- 工業設備 App 對同一台 PLC 只需要一條連線
- 單例確保 PLC 連線生命週期由一個地方管理
- 所有依賴 PLC 數值的控件透過同一個事件源取值，不重複建立連線

**AI 不應做的事：**
- 不要建立多個 PlcStatus 實例
- 不要移除 XAML 中的 `<controls:PlcStatus>` 而改用程式碼動態建立

**狀態：** Active

---

## ADR-007：XAML 禁用硬編碼色碼，必須使用語意 Token

**決策：** 所有 `Controls/*.xaml` 中的顏色必須用語意 Token（`{StaticResource Surface.Bg.Card}` 等），禁止出現 `#RRGGBB`、`Color.FromRgb(...)` 等硬編碼色值。

**原因：**
- Dark/Light 主題切換完全依賴 Token 系統
- 硬編碼色碼會在切換主題時不跟隨切換，造成視覺問題（曾發生：Light 模式下控件仍顯示深色背景）

**可接受的例外：**
- RuntimeControlFactory 內建立 GroupBox 時可用 `Color.FromArgb(...)` 建立動態色彩，因為是從 `headerColor` prop 計算出來的，不是設計時固定值

**AI 不應做的事：**
- 不要在 XAML 裡寫 `Background="#1E1E2E"` 或 `Foreground="White"`
- 新增 Token 時必須同步更新 `DarkColors.xaml` 與 `LightColors.xaml` 兩個檔案

**狀態：** Active（架構禁令）

---

## ADR-008：BehaviorEngine 放在 ShellShared，不放在 UI.Core

**決策：** `BehaviorEngine` 及其 Handler 放在 `Stackdose.App.ShellShared`，而不是 `Stackdose.UI.Core`。

**原因：**
- BehaviorEngine 的 `Navigate` action 需要知道頁面導航機制（`Navigator`），這屬於 Shell 層概念
- UI.Core 是純控件庫，不應依賴 Shell 導航邏輯
- 若放在 UI.Core，會造成 UI.Core → ShellShared 的反向依賴

**影響：**
- DesignRuntime 在啟動時注入 `Navigator` 給 BehaviorEngine
- 新增 BehaviorEngine Handler 應在 ShellShared 新增，不在 UI.Core

**狀態：** Active（B4/B5 重構確立）

---

## ADR-009：RuntimeControlFactory 各 App 保有獨立副本

**決策：** `DesignRuntime`、`MyPrintApp3`、`DashboardTest1`、`ModelE` 各自有一份 `RuntimeControlFactory.cs`，不共用。

**原因（當時）：**
- 各 App 對同一控件（如 Spacer/GroupBox）可能有不同的視覺需求
- 初期快速開發，副本比繼承更直接

**已知問題：**
- 副本導致 bug 修正必須同步多處（如 headerColor 問題，2026-05-11 修正了 3 個副本）
- 新控件支援需要每個副本各自補

**未來可以做的事：**
- 抽出 `BaseRuntimeControlFactory` 共用基底（已列為 TODO，但優先度中）
- 各 App 覆寫特定 Create 方法即可

**狀態：** Technical Debt（可重構，但需協調所有 App）

---

## ADR-010：ComplianceContext.Shutdown() 是必要呼叫，不可省略

**決策：** App 關閉流程中必須呼叫 `ComplianceContext.Shutdown()`。

**原因：**
- `SqliteLogger` 採非同步批次寫入，5秒 flush 一次
- 若不呼叫 Shutdown，最後 5 秒內的日誌資料不會落盤
- FDA 21 CFR Part 11 要求完整的稽核軌跡，遺失資料是合規問題

**AI 不應做的事：**
- 不要在簡化 App 啟動流程時移除 Shutdown 呼叫
- 不要假設「程式結束時 OS 會自動 flush」

**狀態：** Active（FDA 合規要求）

---

## ADR-011：自動化測試有 WPF UI Thread 限制

**決策：** `Stackdose.UI.Core.Tests` 目前為空，不強制要求 UI 控件的自動化測試。

**原因：**
- WPF 控件必須在 STA UI Thread 上執行，標準 xUnit/NUnit 跑在 MTA Thread
- 控件初始化需要完整的 WPF Application 生命週期
- 工業設備 App 目前以手動整合測試（DesignRuntime + 實機/模擬器）驗證

**可以自動化測試的：**
- ViewModel 邏輯（不依賴 WPF 控件）
- 純 C# 的 Context 邏輯（ComplianceContext 日誌格式、SecurityContext 權限計算）
- JSON 序列化/反序列化（machinedesign.json 格式）

**不適合自動化測試的（目前）：**
- WPF 控件的視覺行為（主題切換效果）
- PLC 連線行為（需要實機或 Mock PLC）

**狀態：** Active（已知限制，可逐步改善）

---

## ADR-012：Theme Token 命名改動需同步兩份字典

**決策：** 任何 Token 新增、重命名、移除都必須同步更新 `DarkColors.xaml` 與 `LightColors.xaml`。

**原因：**
- Token 是全域 `StaticResource`，任何控件都可能引用
- 只更新一份字典會導致切換主題時 `ResourceNotFound` 例外
- 歷史上曾發生只改 Dark 忘改 Light，Light 模式下控件顏色全部失效

**Token 前綴規則：**
- `Surface.*` — 背景色
- `Text.*` — 文字色
- `Action.*` — 按鈕/操作色
- `Border.*` — 邊框色
- `Status.*` — 狀態色

**AI 不應做的事：**
- 不要只改其中一份字典
- 不要移除「看起來沒在用」的 Token（可能有控件動態引用）

**狀態：** Active

---

## ADR-013：暫緩實作 PlcConfirmationHandler 且不應整合於 UI JSON

**決策：** 延後「PLC 寫入確認框」的實作，且未來實作時，不應將確認邏輯硬編碼在 `machinedesign.json` 的 `events[].do` 中。

**原因：**
- **安全性風險：** 若確認邏輯散落在各個按鈕的 JSON 裡，容易發生遺漏或設定不一致的情況。
- **維護性：** 同一個 PLC 位址可能在多個頁面被寫入，應集中管理安全性規則。
- **優先順序：** 目前應優先穩定核心 UI 框架與 ModelE 移植，安全加固功能待架構穩定後再議。

**AI 不應做的事：**
- 不要主動在 `machinedesign.json` 裡加入 `confirmWrite` 等動作。
- 不要嘗試實作 `PlcConfirmationHandler`，除非使用者明確要求重啟該任務。

**狀態：** Postponed（待架構穩定後重新評估）

---

## 補充記錄指引

若發現新的「為什麼不那樣做」決策，請用以下格式補記：

```markdown
## ADR-XXX：[決策標題]

**決策：** [一句話說明選擇了什麼]

**原因：**
- [具體理由，越詳細越好]

**AI 不應做的事：**
- [明確列出 AI 不應自動「幫忙優化」的行為]

**狀態：** Active / Deprecated / Technical Debt
```
