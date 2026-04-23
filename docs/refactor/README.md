# 基礎層 + Behavior 重構 — 總覽

> 分支：`refactor/foundation-and-behavior`
> 起始日：2026-04-21
> 發起人：jerry（iiiplay001@gmail.com）

---

## 為什麼要做這個重構

Dashboard 模式封裝功能完成後發現：**設計師在 Designer 拖出來的控件（如 PlcLabel），無法表達「值變化時要做什麼反應」**。想加反應也不知道從哪下手。

追溯原因發現底層有四個結構性問題：

1. **沒有共用控件基類**（文件寫有 `CyberControlBase` / `PlcControlBase`，但實際檔案不存在）
2. **沒有統一的「控件值變化」事件出口**（每個控件各自訂閱 PLC、重複實作）
3. **Shell 沒有策略化**（Dashboard / Standard / 未來 Kiosk 要靠 if-else 分流在 DesignPlayer 啟動點）
4. **文件與實作漂移**（controls-reference.md 寫 26 個但數量/位置不正確、architecture.md 少列 `PlcEventContext`）

底層這四塊不整好，上面做再多 Behavior Engine / Shell 模式都是接不上的空架子。

---

## 最終主旨（重構完要能做到這句話）

> **讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 21 CFR Part 11 稽核要求的監控介面。**

具體白話驗收：設計師在 MachinePageDesigner 拖出一個 PlcLabel，在屬性面板「事件」頁籤配「值 > 100 時設成紅色並寫 PLC M0=1」，存檔、封裝、部署到量產機器後，PLC 值超過 100 時 Label 變紅並觸發寫入——全程不寫一行 C#。

---

## 重構範圍（九階段）

| 階段 | 內容 | 產物 |
|---|---|---|
| **B0** | 底層現況校正 | 控件盤點表、文件 vs 現況差異表、4 份文件修正 |
| **B1** | 抽共用基類 | `CyberControlBase` / `PlcControlBase`（含 `ValueChanged`） |
| **B2** | 事件能力收斂 | `PlcEventContext` 升級為統一匯流排（bit + value + control-prop） |
| **B3** | Templates/Shell 策略化 | `IShellStrategy` + Dashboard/Standard Shell |
| **B4** | Behavior JSON Schema | DesignItem.events[]（on / when / do） |
| **B5** | Behavior Engine | `IBehaviorAction` + 6 內建 actions + 派發 |
| **B6** | Designer UI 事件頁籤 | PropertyPanel 新增事件編輯 |
| **B7** | DesignPlayer Standard 模式收尾 | layoutMode 分流 + BuildNavFromPages |
| **B8** | docs + index.html 全面對齊 | 所有 kb/ + devlog + memory |

---

## 如何使用這個資料夾

- **第一次接手**：先讀 [`HANDOFF.md`](HANDOFF.md)
- **想看整體路線**：讀 [`PLAN.md`](PLAN.md)
- **想看現在做到哪**：讀 [`PROGRESS.md`](PROGRESS.md)
- **B0 產出**：
  - [`B0-control-inventory.md`](B0-control-inventory.md) — 所有控件 DP + 繼承關係
  - [`B0-findings.md`](B0-findings.md) — 文件 vs 實際差異
