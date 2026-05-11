---
classification: Confidential
ai_usage: Local RAG only (Open WebUI) — DO NOT send to Claude API / ChatGPT / external AI
last_updated: 2026-05-11
source_of_truth: false
---

# PLC 資料字典（Data Dictionary）

> **⚠️ CONFIDENTIAL — Local RAG Only**  
> 本文件含有設備 PLC 位址、旗標定義，屬 L3 機密資料。  
> 絕對不可提供給外部 AI（Claude API、ChatGPT、Gemini API）。  
> 外部 AI 協助分析邏輯時，只能提供去識別化版本（欄位保留，位址替換為 `D???` / `M???`）。

---

## 資料分級提醒

| 等級 | 說明 | 適用工具 |
|---|---|---|
| L1 Public | 通用 WPF / C# 技術問題 | 任意 AI |
| L2 Internal | 框架架構、控件邏輯 | Claude CLI / Gemini CLI |
| **L3 Confidential** | **PLC Tag、設備 IP、移植策略** | **僅 Local RAG（Open WebUI）** |

本文件為 **L3 Confidential**。

---

## 使用說明

- 本字典以設備/機型分區段記錄 PLC 位址對應
- 每個 Tag 記錄：位址、資料型別、讀寫方向、說明、對應框架元件
- 若需要向外部 AI 描述邏輯，使用去識別化格式（見底部模板）

---

## Word 暫存器（D 區）

> 此區段記錄 D 位址的用途定義。

| 位址 | 型別 | 讀/寫 | 說明 | 對應控件 |
|---|---|---|---|---|
| *(填入實際 Tag)* | | | | |

---

## Bit 旗標（M 區）

> 此區段記錄 M 位址的用途定義。

| 位址 | 型別 | 讀/寫 | 說明 | 對應控件 |
|---|---|---|---|---|
| *(填入實際 Tag)* | | | | |

---

## 警報位址（Alarm Bit）

> 此區段記錄警報觸發條件對應的 PLC 位址。

| 警報 ID | Bit 位址 | 觸發條件 | 嚴重度 | 顯示訊息 |
|---|---|---|---|---|
| *(填入實際 Alarm)* | | | | |

---

## 感測器位址（Sensor Word）

> 此區段記錄感測器數值的 D 位址及換算規則。

| 感測器名稱 | 位址 | 原始範圍 | 換算公式 | 單位 | 上限 | 下限 |
|---|---|---|---|---|---|---|
| *(填入實際感測器)* | | | | | | |

---

## 指令位址（Command Bit / Word）

> 此區段記錄 SecuredButton 等控件寫入的 PLC 位址。

| 指令名稱 | 位址 | 寫入值 | 說明 | 需要權限等級 |
|---|---|---|---|---|
| *(填入實際指令)* | | | | |

---

## 去識別化模板（給外部 AI 用）

若需要外部 AI 協助分析某段邏輯，使用以下格式描述（不含實際位址）：

```
情境：某個 Word 暫存器（命名為 PROC_TEMP）儲存製程溫度，
      範圍 0–4000，對應 0–400.0°C（除以 10）。
      超過 3500（350°C）時需觸發警報。

問題：如何在 Behavior Engine events[] 中設定此條件？
```

**不可以這樣描述（含真實位址）：**
```
❌ D100 超過 3500 時觸發 M50 = 1
```

---

## 設備分區

> 依設備/機型建立子區段。以下為區段模板：

### 設備：[機型名稱]

**PLC 型號：** [填入]  
**IP 位址：** [填入，Confidential]  
**Port：** [填入，Confidential]  
**輪詢間隔：** [填入 ms]

*(填入該機型的 D / M / Alarm / Sensor / Command 對應表)*

---

## 維護紀錄

| 日期 | 修改者 | 變更說明 |
|---|---|---|
| 2026-05-11 | Jerry | 建立字典骨架 |
