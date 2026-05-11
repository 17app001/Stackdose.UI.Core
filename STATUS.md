---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
owner: Jerry
last_updated: 2026-05-11
source_of_truth: true
---

# 專案狀態

> **唯一動態狀態來源。每次任務結束後更新。**
> 靜態規則與架構見 `CLAUDE.md`；重構歷史見 `docs/refactor/PROGRESS.md`。

---

## 現況

- **日期：** 2026-05-11
- **分支：** `master`
- **上次做了什麼：** LLM Wiki 第二版強化完成（2026-05-11）
  - `docs/AI_KNOWLEDGE_TEST.md` — 全新，20 個 RAG 驗證問題 + 評分表 + 常見失敗模式
  - `docs/RAG_INDEX.md` — 加入 3 張 Mermaid 知識關係圖（AI 流程圖 / 文件關係圖 / 資料分級流程圖）
  - `docs/kb/GLOSSARY.md` — 補齊 14 個術語（HMI / PLC / ModelE / Migration / Feiyang SDK / Platform Contracts / Tolerance Merging / FDA 21 CFR Part 11 / Confidential / Local RAG only / External AI / STATUS.md / RAG_INDEX.md / 主題 Token）
  - `docs/kb/DECISION_LOG.md` — 補 ADR-014 至 ADR-020（7 個新架構決策，含 STATUS.md 唯一真相、資料分級、AI 行為規則）
  - `docs/kb/AI_REPRODUCTION_GUIDE.md` — 加入第 7 節「AI 標準回報格式」（5 段結構化輸出）
  - 12 個文件補齊資料等級 frontmatter（classification / ai_usage / last_updated）
  - `index.html` — 補 RAG_INDEX / GLOSSARY / DECISION_LOG / AI_REPRODUCTION_GUIDE / AI_KNOWLEDGE_TEST 卡片連結
- **下一步：**
    1. ⚠ rebuild MyPrintApp3 / DashboardTest1 / ModelE 確認 Spacer headerColor 修正生效
    2. **ModelE 移植 — PrintWorkflowService** — 撰寫 App 層列印工作流狀態機（見下方移植備忘）
    3. **Open WebUI 環境確認** — 確認有哪些工具後決定安裝方式
    4. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重載畫布

---

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、D512 flag 確認 |
| 2 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |
| 3 | DATA_DICTIONARY 填入 | `docs/specs/DATA_DICTIONARY.md` | Confidential，填入實際 PLC Tag |
| 4 | **ModelE 移植** | `ModelE/Services/PrintWorkflowService.cs` | 見下方移植備忘，明天繼續 |

---

## 未來規劃 (Icebox)

| 任務 | 優先度 | 備註 |
|---|---|---|
| **PlcConfirmationHandler** | 低 | 帶倒數與 Event 接軌的確認框。**決策：不整合在 machinedesign.json**，待架構穩定後再議。 |
| 自動化測試套件 | 低 | 建立 UI.Core.Tests 的基礎架構 |

---

## 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| **Spacer headerColor runtime 仍為 Primary 藍** | 高 | MyPrintApp2 有獨立 RuntimeControlFactory.cs 副本，需單獨 rebuild MyPrintApp2.csproj；代碼修正已完成 |
| **D512 PLC flag 缺失** | 中 | ModelE 傳圖前寫 D512 作為層旗標，待實機確認是否需要補 |
| JSON 熱更新 | 中 | DesignRuntime 尚未實作 |

---

## 高風險區域

| 區域 | 風險 | 注意事項 |
|---|---|---|
| `IPlcManager` / `IPlcMonitor` 介面 | 高 | 跨 Repo 依賴，簽名改動導致多處編譯失敗 |
| `ComplianceContext` / `SqliteLogger` | 高 | 稽核軌跡，關閉前必須呼叫 `Shutdown()` |
| `IPrintHead` / `FeiyangPrintHead` | 高 | C++ SDK 橋接，改動影響 PrintHead 所有功能 |
| `scripts/init-shell-app.ps1` | 中 | 改動影響所有 scaffold 產出專案，需同步更新 `MyPrintApp2` 等現有副本 |
| `DarkColors.xaml` / `LightColors.xaml` | 中 | Token 命名規則全局生效，改名或移除會破壞所有控件主題 |

---

## 測試狀態

| 項目 | 狀態 | 備註 |
|---|---|---|
| 自動化單元測試 | 無（`UI.Core.Tests` 為空） | — |
| DesignRuntime 手動測試 | 手動 | 每次改動後在 DesignRuntime 載入 JSON 確認 |
| 主題切換（Dark/Light） | 手動 | 切換後目視確認所有控件顯示正確 |
| PrintHead 實機測試 | 部分完成 | Head1 已驗證，Head2 config 路徑問題待確認 |
| Dashboard scaffold 測試 | 手動 | `new-app.ps1 -Mode Dashboard` 產出後手動驗證 |

---

## 最近 Commits

```
f90cbd6 fix: PrintHeadStatus layout — call ResetStatusDisplay before LoadConfiguration
ef05d01 fix: Light mode runtime — button text, AlarmViewer scope, PrintHeadStatus layout, Spacer template
71e63d0 fix: Light模式主題完整修正 — 控件顏色 token 化 + Designer 設計時預覽
aefd366 light模式處理未完成，PrintHeadStatus 跑版
6a456f6 fix: PrintHead disabled opacity + DesignTimeFactory Spacer solid bg
```

---

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | ✅ 完成（含容器感知、全方位 Padding） |
| Light/Dark 主題切換 | ✅ 完成（Designer + DesignRuntime + 實機 App） |
| 重構 B0–B10 | ✅ 全部完成 |

---

## ModelE 移植備忘（2026-05-11 討論）

### 架構決策
- **BehaviorEngine（JSON）** 只處理 UI 反應（按鈕→寫 PLC、值變化→改顏色）
- **複雜序列流程**（啟動列印→等 PLC 旗標→傳圖→等 D512→換圖→下一層）必須寫在 App 層 C# Service

### 要建的檔案
```
ModelE/Services/PrintWorkflowService.cs
```
狀態機：`Idle → WaitingForReady → SendingImage → WaitingForLayerFlag → NextLayer → Done`

### 介面對應
| 動作 | 使用介面 |
|---|---|
| 等待 PLC 旗標 | `PlcStatus.ScanUpdated` + `IPlcManager.GetBit()` |
| 傳圖給噴頭 | `IPrintHead.SendAsync()` |
| 等 D512 layer flag | `IPlcManager.GetWord("D512")` — 待實機確認 |
| 記錄稽核 | `ComplianceContext.LogOperation()` |
| 更新 UI 進度 | `BehaviorEventBus.RaiseControlEventFired()` |

### 已知缺口（移植前需確認）
- D512 是否確實為 layer flag（STATUS.md 已標記待確認）
- D65/D67/D69/D71 軸位址現場確認
- D2000 供墨超時暫存器疑似原始錯誤，需確認
- 軸控 UI 控件尚未實作（需要位置輸入 + 寫 PLC）

---

## 交接備註

- **給下一位 AI 接手**：依序讀 `CLAUDE.md` → 本文件 → `SECURITY_RULES.md` → `docs/RAG_INDEX.md` → `docs/kb/DECISION_LOG.md`，讀完這五份就能開始工作。有「能不能做 X」的問題查 DECISION_LOG，找不到文件查 RAG_INDEX。
- **MyPrintApp2 是獨立副本**：scaffold 修正需要額外 rebuild `MyPrintApp2.csproj`，不是 `Stackdose.UI.Core.sln`。
- **含中文的 `.ps1` 必須存 UTF-8 with BOM**，否則 PS5 zh-TW Windows 環境下中文亂碼。
- **不自動 commit/push**，等使用者明確說才執行。
