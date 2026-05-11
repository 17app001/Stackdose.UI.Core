---
classification: Internal
ai_usage: Local RAG allowed / Claude CLI allowed
owner: Jerry
last_updated: 2026-05-11
source_of_truth: true
---

# AI_USAGE_POLICY.md — AI 工具使用政策

> 依資料敏感度分三級，規範哪類資料可以使用哪些 AI 工具。
> 詳細禁止事項見 `SECURITY_RULES.md`。

---

## Level 1：通用開發 / 公開技術問題

**範例**
- WPF / XAML UI 排版問題
- C# 語法與 .NET 8 API
- 單元測試框架用法
- Regex、LINQ 寫法
- 通用架構設計討論
- 無敏感資訊的錯誤訊息

**可用工具**
- GitHub Copilot、Cursor、Claude CLI、Gemini CLI
- ChatGPT、Claude.ai、Gemini
- 任何公開 AI 服務

**限制**
- 不可貼 API Key、密碼、Token、真實 IP、連線字串。
- 不可貼完整客戶設定檔。

---

## Level 2：一般內部邏輯 / 可抽象化業務邏輯

**範例**
- 抽象化後的 PLC 通訊流程（已隱去 IP）
- 資料庫結構設計、CRUD 架構討論
- 框架架構設計、介面設計
- 報表生成邏輯（已隱去客戶資料）
- 已去識別化的 SQL 效能問題
- UI 控制項的業務邏輯（已隱去設備參數）

**可用工具**
- Claude API / OpenAI API（透過公司帳號）
- ChatGPT Team / Enterprise
- Azure OpenAI（公司核准帳號）
- Claude CLI / Gemini CLI（僅使用去識別化摘要）
- Open WebUI + Local RAG（去識別化後）

**限制**
- 必須先去識別化：移除公司名稱、客戶資料、真實 IP/Port、設備序號、製程參數。
- 確認 AI 服務商不使用輸入資料訓練模型（或啟用 Opt-out）。
- 建議使用公司核准的帳號，避免個人帳號混入商業資料。

---

## Level 3：核心機密 / 高敏感資料

**範例**
- 製程參數、藥粉配比
- 3D 列印控制演算法
- 真實 PLC IP、真實機台控制流程
- 客戶個資、合約、報價
- 公司核心 IP / 演算法
- 任何可識別客戶、現場環境的資料

**可用工具（僅限地端）**
- Ollama（本機）
- Open WebUI + Local RAG（公司內網）
- 地端部署的 DeepSeek / Qwen / Llama

**限制**
- **絕對不可送往外部 AI 服務**（包含 Claude、GPT、Gemini 等任何雲端服務）。
- 地端部署必須在防火牆內，不得連外網。
- 需要存取權限控管 + 操作稽核記錄。
- 資料不得離開公司網路。

---

## 快速判斷表

| 資料類型 | Level | 可用工具 |
|---|---|---|
| WPF 排版問題 | L1 | 任何 AI |
| 通用 C# 錯誤 | L1 | 任何 AI |
| 抽象架構設計 | L1~L2 | 任何 AI（無敏感資訊則 L1） |
| 去識別化業務邏輯 | L2 | Claude/GPT API、Claude CLI |
| 框架 JSON schema | L2 | Claude/GPT API（去識別化後） |
| 真實 PLC IP / 設備參數 | L3 | 地端 AI 僅 |
| 客戶專案設定 | L3 | 地端 AI 僅 |
| 製程參數 / 核心演算法 | L3 | 地端 AI 僅 |

---

## 此專案的慣用工具對應

| 工作類型 | 建議工具 |
|---|---|
| 框架開發（UI.Core、Templates） | Claude CLI（L1~L2） |
| 設計器功能開發 | Claude CLI（L1~L2） |
| PLC 通訊邏輯 | Claude CLI + 去識別化（L2），或地端 |
| 客戶專案客製化 | 地端 AI（L3） |
| 製程參數調整 | 地端 AI（L3） |
| 知識庫建立（RAG） | 去識別化後放入 Open WebUI |
