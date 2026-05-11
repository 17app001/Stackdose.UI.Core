---
classification: Internal
ai_usage: Claude CLI allowed / Local RAG allowed
last_updated: 2026-05-11
source_of_truth: false
---

# RAG 知識庫測試集

> 用來驗證目前知識庫是否有效回答常見問題。  
> AI 回答後，對照「預期依據文件」欄位自評是否引用正確來源。  
> **目的：** 找出知識庫覆蓋缺口，不是考試。

---

## 測試問題集

| 編號 | 問題 | 預期依據文件 | 預期答案摘要 |
|---|---|---|---|
| Q01 | 目前專案最新狀態是什麼？ | `STATUS.md` | 讀 STATUS.md 的「現況」段落，給出日期、分支、上次做了什麼、下一步 |
| Q02 | 下一步是什麼？ | `STATUS.md` | STATUS.md 的「下一步」三項（rebuild MyPrintApp / Open WebUI 確認 / JSON 熱更新） |
| Q03 | AI 開始開發前必讀哪些文件？ | `docs/RAG_INDEX.md`、`CLAUDE.md` | CLAUDE.md → STATUS.md → SECURITY_RULES.md → RAG_INDEX.md → DECISION_LOG.md 共 5 份 |
| Q04 | STATUS.md 與 PROGRESS.md 衝突時以誰為準？ | `docs/RAG_INDEX.md`（場景 1）、`docs/kb/DECISION_LOG.md`（ADR-014、ADR-020） | 以 STATUS.md 為準，PROGRESS.md 是 B0–B10 歷史紀錄 |
| Q05 | 哪些資料不能提供給外部 AI？ | `SECURITY_RULES.md`、`AI_USAGE_POLICY.md`、`docs/kb/GLOSSARY.md`（Confidential） | PLC 真實位址、ModelE 移植策略、eval JSON、PrintHead 控制邏輯、客戶資料、製程參數、Production 設定 |
| Q06 | 哪些文件只能 Local RAG？ | `AI_USAGE_POLICY.md`、`docs/RAG_INDEX.md`（場景 10、11） | DATA_DICTIONARY.md、ModelE_Migration_Brief.md、eval/modele-vs-framework.md |
| Q07 | 修改 HMI Designer，應該先看哪些文件？ | `docs/RAG_INDEX.md`（場景 5） | STATUS.md → PROJECT_MAP.md → designer-system.md → controls-reference.md → behavior-system.md → foundation-base-classes.md |
| Q08 | 要查 PLC / ModelE，應該注意什麼？ | `docs/RAG_INDEX.md`（場景 10、11）、`SECURITY_RULES.md` | Confidential 等級，Local RAG only，不可提供給外部 AI，需去識別化才能請外部 AI 協助 |
| Q09 | 沒有實機 PLC 時如何驗證修改？ | `docs/kb/AI_REPRODUCTION_GUIDE.md` | DesignRuntime 離線模式（UI 驗證）、DesignViewer（JSON 渲染）、dotnet build（編譯）、UI.Core.Tests（C# 邏輯） |
| Q10 | index.html 是不是狀態來源？ | `docs/kb/DECISION_LOG.md`（ADR-018）、`docs/RAG_INDEX.md`（場景 1） | 不是。index.html 是文件導覽首頁，狀態唯一來源是 STATUS.md |
| Q11 | 哪些文件可能已過期？ | `CLAUDE.md`（知識庫索引表）、`docs/RAG_INDEX.md`（場景 8） | docs/refactor/HANDOFF.md（2026-04-24 已過期）、docs/MANAGER_BRIEF.md（2026-04-15 已過期）、refactor/* 歷史文件 |
| Q12 | 哪些操作需要人工確認？ | `docs/kb/DECISION_LOG.md`（ADR-017）、`STATUS.md`（高風險區域） | IPlcManager / IPlcMonitor / IPrintHead 修改、ComplianceContext 修改、scaffold 腳本修改、DarkColors.xaml 改名 |
| Q13 | 哪些文件適合放入 Open WebUI Knowledge？ | `AI_USAGE_POLICY.md`、`docs/RAG_INDEX.md` | Internal 等級全部可用；Confidential 只能在 Local RAG；Public 可任意 |
| Q14 | 哪些文件適合 Claude CLI？ | `AI_USAGE_POLICY.md` | Internal 等級文件（CLAUDE.md、STATUS.md、docs/kb/*.md 等） |
| Q15 | 哪些文件不適合 Claude CLI？ | `AI_USAGE_POLICY.md`、`SECURITY_RULES.md` | Confidential 等級文件（DATA_DICTIONARY.md、ModelE_Migration_Brief.md、eval/*.md） |
| Q16 | AI 找不到答案時，應該怎麼回答？ | `docs/kb/DECISION_LOG.md`（ADR-021） | 說「找不到對應的 ADR 或文件依據」→ 附推斷內容與來源 → 建議補記 DECISION_LOG；不可沉默、不可捏造 |
| Q17 | 如何把本地 RAG 結果轉成去識別化 Claude 任務？ | `docs/kb/DECISION_LOG.md`（ADR-019）、`docs/specs/DATA_DICTIONARY.md` | 使用 DATA_DICTIONARY.md 底部的去識別化模板，替換 PLC 位址、設備型號，移除客戶資訊後才提交外部 AI |
| Q18 | 哪些術語容易混淆？ | `docs/kb/GLOSSARY.md` | Designer（工具）vs DesignRuntime（執行環境）；PlcControlBase（訂閱基類）vs PlcStatus（連線單例）；BehaviorEngine（in ShellShared）vs BehaviorEventBus（橋接）；RuntimeControlFactory（各 App 獨立副本）|
| Q19 | 哪些架構決策 AI 不應擅自推翻？ | `docs/kb/DECISION_LOG.md` | ADR-001（不用 DI）、ADR-002（不改 Platform 介面）、ADR-003（不耦合 App 邏輯進 UI.Core）、ADR-007（禁硬編碼色碼）、ADR-008（BehaviorEngine 在 ShellShared） |
| Q20 | 修改後要更新哪些文件？ | `CLAUDE.md`（每次任務回應格式）、`STATUS.md` | 必須更新 STATUS.md；若涉及架構決策，補記 DECISION_LOG；若影響術語定義，補記 GLOSSARY |

---

## 評分表

> 執行測試時填入，用來追蹤知識庫改善進度。

| 編號 | 問題 | 預期依據文件 | AI 回答是否正確 | 問題原因 | 改善方式 |
|---|---|---|---|---|---|
| Q01 | 目前專案最新狀態是什麼？ | `STATUS.md` | — | — | — |
| Q02 | 下一步是什麼？ | `STATUS.md` | — | — | — |
| Q03 | AI 開始開發前必讀哪些文件？ | `RAG_INDEX.md`、`CLAUDE.md` | — | — | — |
| Q04 | STATUS.md 與 PROGRESS.md 衝突時以誰為準？ | `DECISION_LOG.md` ADR-014/020 | — | — | — |
| Q05 | 哪些資料不能提供給外部 AI？ | `SECURITY_RULES.md`、`GLOSSARY.md` | — | — | — |
| Q06 | 哪些文件只能 Local RAG？ | `AI_USAGE_POLICY.md`、`RAG_INDEX.md` | — | — | — |
| Q07 | 修改 HMI Designer 先看哪些文件？ | `RAG_INDEX.md`（場景 5） | — | — | — |
| Q08 | 查 PLC / ModelE 應注意什麼？ | `RAG_INDEX.md`（場景 10/11）、`SECURITY_RULES.md` | — | — | — |
| Q09 | 沒有實機 PLC 時如何驗證？ | `AI_REPRODUCTION_GUIDE.md` | — | — | — |
| Q10 | index.html 是不是狀態來源？ | `DECISION_LOG.md` ADR-018 | — | — | — |
| Q11 | 哪些文件可能已過期？ | `CLAUDE.md`、`RAG_INDEX.md`（場景 8） | — | — | — |
| Q12 | 哪些操作需要人工確認？ | `DECISION_LOG.md` ADR-017、`STATUS.md` | — | — | — |
| Q13 | 哪些文件適合 Open WebUI Knowledge？ | `AI_USAGE_POLICY.md` | — | — | — |
| Q14 | 哪些文件適合 Claude CLI？ | `AI_USAGE_POLICY.md` | — | — | — |
| Q15 | 哪些文件不適合 Claude CLI？ | `AI_USAGE_POLICY.md`、`SECURITY_RULES.md` | — | — | — |
| Q16 | AI 找不到答案時怎麼回答？ | `DECISION_LOG.md`（ADR-021） | — | — | — |
| Q17 | 如何做去識別化 Claude 任務？ | `DECISION_LOG.md` ADR-019、`DATA_DICTIONARY.md` | — | — | — |
| Q18 | 哪些術語容易混淆？ | `GLOSSARY.md` | — | — | — |
| Q19 | 哪些架構決策 AI 不應推翻？ | `DECISION_LOG.md` | — | — | — |
| Q20 | 修改後要更新哪些文件？ | `CLAUDE.md`、`STATUS.md` | — | — | — |

---

## 使用說明

1. 選一個 RAG 工具（Open WebUI / Claude CLI）
2. 上傳本專案的 Internal 知識庫文件
3. 逐題提問，記錄 AI 實際回答
4. 對照「預期依據文件」欄位，判斷 AI 是否引用正確來源
5. 填入「AI 回答是否正確」欄，若不正確記錄問題原因與改善方式
6. 問題集中在某類文件 → 檢查該文件的 RAG 分段是否合理

---

## 已知常見失敗模式

| 失敗模式 | 根因 | 修復方式 |
|---|---|---|
| AI 用 PROGRESS.md 回答「現在狀態」 | PROGRESS.md 檔名含「progress」讓 AI 以為是現況 | 確保 STATUS.md 在知識庫中優先度高，PROGRESS.md 加明顯「歷史紀錄」標示 |
| AI 把 ModelE 問題提交給外部 AI | 未先讀 SECURITY_RULES.md | 強制 AI 開始每個任務前先讀安全規則 |
| AI 修改 IPlcManager 簽名不確認 | 未讀 platform-contracts.md | DECISION_LOG ADR-002 加強說明，RAG_INDEX 場景補充 |
| AI 直接回答「現在有 X 功能」而未確認 | 知識庫文件過期，AI 未核對 STATUS.md | 每次問「現況」類問題時，強制先查 STATUS.md |
