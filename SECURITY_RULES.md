---
classification: Internal
ai_usage: Local RAG allowed / Claude CLI allowed (non-sensitive sections only)
owner: Jerry
last_updated: 2026-05-11
source_of_truth: true
---

# SECURITY_RULES.md — AI 開發資安規則

> 適用所有使用 AI 工具協助此專案開發的場景。
> 違反本規則的操作必須人工確認後才能執行。

---

## 一、禁止提供給外部 AI 的資料

| 類別 | 具體例子 |
|---|---|
| 憑證與金鑰 | API Key、Token、密碼、DB Connection String、OAuth Secret |
| 環境設定 | `.env`、`appsettings.Production.json`、內部網域、VPN 設定 |
| 設備敏感資料 | 真實 PLC IP / Port、真實機台控制演算法、製程參數、藥粉配比 |
| 客戶資料 | 客戶名稱、客戶專案原始碼、現場部署設定、可識別客戶的任何資料 |
| 商業機密 | 合約、報價、財務資料、核心演算法、3D 列印控制邏輯 |
| 完整日誌 | 含真實設備狀態或客戶操作紀錄的 log 檔 |

---

## 二、可提供給外部 AI 的資料

| 類別 | 具體例子 |
|---|---|
| 通用開發問題 | WPF 排版、XAML 語法、C# 語法、.NET 8 API 用法 |
| 已去識別化問題 | 移除 IP、客戶名、設備參數後的錯誤訊息 |
| 架構討論 | 抽象架構設計、介面設計、設計模式討論 |
| 測試寫法 | 單元測試、整合測試框架用法（不含真實資料） |
| 範例程式碼 | 不含機密邏輯的示範程式碼 |

---

## 三、Claude / Gemini CLI 操作規則

1. **不得讀取或要求使用者提供 secrets**（`.env`、`appsettings.Production.json` 等）。
2. **不得要求完整客戶專案資料**，應要求使用者提供去識別化摘要。
3. **修改前先說明影響範圍**，讓使用者確認再執行。
4. **修改後列出變更檔案與驗證方式**。
5. **以下操作必須人工確認後才執行：**
   - 涉及 PLC 通訊邏輯（`IPlcManager`、`IPlcClient`、`IPlcMonitor`）
   - 涉及 PrintHead 控制（`IPrintHead`、`FeiyangPrintHead`）
   - 涉及 ComplianceContext / SqliteLogger（稽核軌跡）
   - 涉及安全性與登入驗證邏輯
   - 資料庫 migration 或 schema 變更
6. **不得自動執行以下操作（需使用者明確指示）：**
   - `git push --force`
   - 刪除檔案或分支
   - 覆蓋 `appsettings` 或 `Config/` 下的設定檔
   - 部署到生產環境

---

## 四、高風險介面（改動前必須讀 platform-contracts.md）

| 介面 | 風險 | 原因 |
|---|---|---|
| `IPlcManager` | 高 | 簽名改動導致 UI.Core 多處編譯失敗 |
| `IPlcMonitor` | 高 | 跨 repo 依賴，改動影響 DeviceFramework |
| `IPrintHead` | 高 | Feiyang SDK 整合，改動影響 PrintHeadController/Status |
| `ComplianceContext.Shutdown()` | 中 | 關閉前必須呼叫，否則日誌遺失 |
| `SqliteLogger` | 中 | 非同步批次寫入，改動可能導致 race condition |

詳見 `docs/kb/platform-contracts.md`。

---

## 五、Open WebUI / Private RAG 使用規則

- 放入 RAG 的文件必須先確認不含任何 Level 3 機密資料（參見 `AI_USAGE_POLICY.md`）。
- 可放入 RAG：`CLAUDE.md`、`docs/kb/*.md`、`docs/devlog/*.md`（去識別化後）。
- 不可放入 RAG：完整客戶設定、真實 PLC IP、製程參數、任何含客戶識別資訊的 JSON。
