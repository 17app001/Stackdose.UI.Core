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
- **上次做了什麼：** 修正 PrintHeadStatus 高度跑版根因（2026-05-11）
  - `PrintHeadStatus.xaml.cs` — `ResetStatusDisplay()` 移到 `LoadConfiguration()` 之前，確保 config 找不到時 VoltagesPanel 仍有佔位資料，防止 Row1=* 異常膨脹
- **下一步：**
    1. ⚠ 確認 MyPrintApp2 單獨重建後 Spacer header 灰色是否正確（需 rebuild MyPrintApp2.csproj，不是 Stackdose.UI.Core.sln）
    2. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框
    3. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重載畫布

---

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `UI.Core/Shell/Handlers` | 帶倒數與 Event 接軌的確認框 |
| 2 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、D512 flag 確認 |
| 3 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |

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

## 交接備註

- **給下一位 AI 接手**：先讀 `CLAUDE.md`，再讀本文件，再讀 `SECURITY_RULES.md`。
- **MyPrintApp2 是獨立副本**：scaffold 修正需要額外 rebuild `MyPrintApp2.csproj`，不是 `Stackdose.UI.Core.sln`。
- **含中文的 `.ps1` 必須存 UTF-8 with BOM**，否則 PS5 zh-TW Windows 環境下中文亂碼。
- **不自動 commit/push**，等使用者明確說才執行。
