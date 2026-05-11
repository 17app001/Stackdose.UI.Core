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

## ADR-014：核心控制項修改之「先問後動」準則

**決策：** 凡涉及 `Stackdose.UI.Core\Controls` 目錄下的現有控制項修改、屬性增減、或是新增任何控制項，AI 必須先行提問並獲得明確授權。

**原因：**
- **相容性風險：** `UI.Core` 是多專案共用的基石，隨意修改屬性會導致其他相依專案（如其他 App）編譯失敗。
- **架構純潔性：** 為了保持「JSON 驅動」的主旨，應避免為了單一 App 的特殊需求而污染全域控制項。
- **團隊協作：** 確保 UI 控件的演進符合專案整體風格與技術規範。

**AI 應做的事：**
- 修改控制項前，先列出變更影響範圍與理由。
- 提供 XAML/C# 的修改預覽供審核。

**狀態：** Active (Mandatory)

---

## ADR-014：STATUS.md 是唯一動態狀態來源

**決策：** 專案現況（進行中任務、未解問題、下一步）只由 `STATUS.md` 記錄，任何其他文件（devlog、PROGRESS、HANDOFF、index.html）不得取代其角色。

**原因：**
- 多份文件記錄狀態會造成不一致，AI 無法判斷哪份最新
- STATUS.md 每次任務後強制更新，是唯一有時效保證的文件
- RAG 系統若從 devlog 抽取「現況」很容易讀到過期資訊

**AI 不應做的事：**
- 不要用 devlog 的最新條目回答「現在狀態是什麼」
- 不要用 PROGRESS.md 判斷任務完成度
- 若 STATUS.md 與其他文件衝突，以 STATUS.md 為準

**狀態：** Active（文件系統設計決策）

---

## ADR-015：Migration / eval 文件只能 Local RAG

**決策：** `docs/ModelE_Migration_Brief.md` 和 `docs/eval/modele-vs-framework.md` 標示為 Confidential，只能在本機 RAG 系統使用，不可提供給外部 AI。

**原因：**
- 這兩份文件含設備移植策略、WinForms 控制邏輯、設備型號等商業機密
- 若提交給外部 AI（Claude API / ChatGPT），等同對雲端服務揭露競爭優勢與設備細節
- PLC 位址與製程參數若外洩，可能造成安全風險

**AI 不應做的事：**
- 不要把 ModelE 或 Migration 相關問題直接提交給外部 AI
- 若需要外部 AI 協助分析，只能提供去識別化摘要（使用 `docs/specs/DATA_DICTIONARY.md` 底部的模板）

**狀態：** Active（資安政策）

---

## ADR-016：AI 修改前必須列出影響範圍

**決策：** AI 在修改任何程式碼前，必須先列出：需求理解、影響檔案清單、風險區域、驗證方式。

**原因：**
- 工業設備 UI 的控件修改往往有跨 App 副本影響（ADR-009）
- 跨 Repo 介面修改影響 Platform（ADR-002）
- 提前列出影響範圍讓使用者能在執行前發現盲點，防止造成意外破壞

**AI 不應做的事：**
- 不要在列出影響分析前直接開始修改程式碼
- 不要假設「只改了一個地方，其他都沒影響」

**狀態：** Active（AI 行為規則）

---

## ADR-017：高風險區域修改需要人工確認

**決策：** 涉及 `IPlcManager / IPlcMonitor / IPrintHead`、`ComplianceContext`、`PlcStatus`、scaffold 腳本的修改，AI 必須在執行前獲得使用者明確確認。

**原因：**
- 這些區域的錯誤可能導致：稽核日誌遺失（FDA 違規）、PLC 連線異常（設備停機）、所有 App 編譯失敗
- 修復成本遠高於「先確認後執行」的一次對話

**高風險區域清單：**
- `IPlcManager / IPlcMonitor / IPrintHead` — 跨 Repo 契約
- `ComplianceContext / SqliteLogger` — 稽核軌跡
- `IPrintHead / FeiyangPrintHead` — C++ SDK 橋接
- `scripts/init-shell-app.ps1` — 影響所有 scaffold 產出
- `DarkColors.xaml / LightColors.xaml` — 全域主題

**AI 不應做的事：**
- 不要在未獲確認的情況下修改高風險區域
- 不要把高風險修改與一般修改混在同一個 commit

**狀態：** Active

---

## ADR-018：index.html 只作為文件導覽首頁

**決策：** `index.html` 定位為靜態文件導覽頁，不承擔動態狀態管理功能，不作為 RAG 主知識來源，不存放機密資料。

**原因：**
- HTML 文件不易被 RAG 系統正確分段索引
- 狀態資訊若寫入 HTML，每次更新需要同時改 HTML 與 STATUS.md，容易造成不一致
- index.html 可能被靜態伺服器公開，不應含機密資訊

**規則：**
- index.html 的狀態摘要必須標注「以 STATUS.md 為準」
- index.html 可連結到各文件，但文件內容不應複製到 HTML 中

**狀態：** Active

---

## ADR-019：外部 AI 不可直接讀取完整機密文件

**決策：** 外部 AI（Claude API / ChatGPT / Gemini）只能接收去識別化摘要，不可直接接收 Confidential 等級文件的完整內容。

**原因：**
- 雲端 AI 服務的輸入可能被用於模型訓練或日誌記錄（各服務政策不同，風險不確定）
- Confidential 文件含商業機密、設備細節、客戶資料，外洩風險不可接受
- 去識別化摘要能保留分析所需的結構，同時移除可辨識資訊

**去識別化原則：**
- 替換 PLC 位址（D100 → REG_TEMP）
- 替換設備型號（ModelE → DEVICE_A）
- 移除客戶名稱與地址
- 保留功能邏輯描述

**狀態：** Active（資安政策）

---

## ADR-020：devlog / PROGRESS / HANDOFF 只作為歷史紀錄

**決策：** `docs/devlog/*.md`、`docs/refactor/PROGRESS.md`、`docs/refactor/HANDOFF.md` 是歷史紀錄文件，不反映現況。

**原因：**
- devlog 是日記格式，隨著時間累積，最新的條目不一定代表現況（可能有後續修正）
- PROGRESS.md 記錄 B0–B10 重構歷史，所有任務已完成，不再更新
- HANDOFF.md（2026-04-24）已明確標示過期

**AI 不應做的事：**
- 不要根據 devlog 最新條目判斷「現在進行中的任務」
- 不要把 PROGRESS.md 的完成狀態當作「可以修改那個模組」的依據
- 若 devlog 與 STATUS.md 衝突，以 STATUS.md 為準

**狀態：** Active

---

## ADR-021：找不到答案時 AI 的標準回答方式

**決策：** 當 AI 查遍 RAG_INDEX、DECISION_LOG、GLOSSARY 仍找不到明確答案時，必須依以下格式回應，不可沉默、不可捏造、不可直接跳過。

**標準回答語句範本：**

```
找不到對應的 ADR 或文件依據。

我的推斷（基於 [architecture.md / GLOSSARY.md / 現有程式碼]）：
[推斷內容，1–3 句]

建議：
- 若此推斷正確，可補記到 docs/kb/DECISION_LOG.md（ADR-XXX）
- 若需要確認，請提供更多背景或指向相關文件
```

**原因：**
- 沒有明確依據的回答容易讓 AI 自行「優化」架構決策，造成破壞
- 明確說出「找不到依據」讓使用者知道這是推斷，而非事實
- 推斷後建議補記 ADR，能逐步完善知識庫

**AI 不應做的事：**
- 不要在找不到依據時直接給出確定性回答（「可以這樣做」）
- 不要跳過問題或回答「我不知道」而不附推斷
- 不要用 devlog 或 PROGRESS 的片段替代正式 ADR

**狀態：** Active（AI 行為規則）

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
