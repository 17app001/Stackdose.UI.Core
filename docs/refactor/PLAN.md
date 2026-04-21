# 重構完整計畫（B0 → B8）

> 每階段：獨立 commit、獨立可驗收、可單獨回滾。
> 不看這份不要動手。看完這份還有疑問 → 先問用戶。

---

## 核心原則

1. **舊控件可以移除**：用戶明確同意不為 UbiDemo / 測試專案的編譯讓步。
2. **底層優先，上層後做**：B0→B3 做完才碰 Behavior Engine。
3. **每階段 stop-and-report**：除非用戶明確授權連跑，否則每階段完成後回報停手。
4. **文件與程式碼同步**：最後一階段 B8 全面對齊前，每階段產出文件都放 `docs/refactor/`，不要直接改 `docs/kb/`（避免中途漂移）——B8 才統一回灌到 `kb/`。

例外：**B0** 本身就是在修 `kb/`，因為 B0 的目的就是把 `kb/` 和實作對齊。

---

## B0 — 底層現況校正（本階段進行中）

### 目標
讓所有文件準確反映**現在的程式碼狀態**。不新增、不抽基類，只校正。

### 子任務
- **B0.1** Core Controls DP 盤點 → `B0-control-inventory.md`
- **B0.2** Templates Controls / Pages 盤點（附錄寫進同一份）
- **B0.3** 文件 vs 實際差異表 → `B0-findings.md`
- **B0.4** 修正 `docs/kb/controls-reference.md`
- **B0.5** 修正 `docs/kb/architecture.md` + `docs/PROJECT_MAP.md`
- **B0.6** 更新 `index.html`、`CURRENT_FOCUS.md`、`docs/devlog/2026-04.md`
- **B0.7** Commit、回報、停手等 B1 許可

### 驗收
- [ ] `controls-reference.md` 所有控件對應實體檔案且屬性正確
- [ ] `architecture.md` 沒有「檔案不存在的基類體系」
- [ ] `PROJECT_MAP.md` 控件歸屬正確
- [ ] `index.html` 對外描述不誇大
- [ ] `B0-control-inventory.md` 涵蓋 24 個 Core + 7 個 Templates 控件
- [ ] `B0-findings.md` 每條差異都有「誰該改 / 改成什麼」

---

## B1 — 抽共用基類

### 目標
建立 `CyberControlBase`（主題 + WeakRef）與 `PlcControlBase`（PlcContext + ScanUpdated + **ValueChanged 事件**），逐一遷移 Plc 系列控件。

### 關鍵設計
`PlcControlBase` 必須暴露：
```csharp
public event EventHandler<PlcValueChangedEventArgs> ValueChanged;
public string Address { get; set; }       // 共用 DP
public object? CurrentValue { get; }      // 最新值（thread-safe）
```

### 子任務
- **B1.1** 在 `Stackdose.UI.Core/Controls/` 新增 `CyberControlBase.cs`
- **B1.2** 新增 `PlcControlBase.cs`（含 ValueChanged 事件）
- **B1.3** PlcLabel → PlcControlBase（優先，因最常用）
- **B1.4** PlcText / PlcStatusIndicator / PlcStatus → PlcControlBase
- **B1.5** PlcEventTrigger 移植邏輯至 PlcControlBase（保留向後相容介面）
- **B1.6** 其餘控件（PlcDeviceEditor、ProcessStatusIndicator）評估是否遷移
- **B1.7** UbiDemo 可以不編譯（依用戶授權），但保留 `*.UbiDemo.broken.md` 註記

### 驗收
- [ ] MachinePageDesigner Preview 正常（DesignRuntime 開 UbiOven.machinedesign.json 能跑）
- [ ] DesignPlayer 封裝輸出的 Dashboard App 正常
- [ ] `PlcControlBase.ValueChanged` 可透過 code-behind 訂閱並收到值變化

---

## B2 — 事件能力收斂

### 目標
把 `PlcEventContext`（目前只在 PlcEventTrigger 內部用）升級為**統一的控件事件匯流排**，供未來 Behavior Engine 訂閱。

### 關鍵設計
```
PlcEventContext（升級後）
├── 來源 1：PLC bit 邊緣（沿用 PlcEventTrigger）
├── 來源 2：PLC value 條件（新增，支援 >/</==/range）
└── 來源 3：控件屬性變化（新增，來自 PlcControlBase.ValueChanged）

統一 API：
  PlcEventContext.Subscribe(filter, handler)
  PlcEventContext.Publish(source, payload)
```

### 驗收
- [ ] 單元測試：模擬 PLC bit / value / property change，Subscribe 都能收到
- [ ] PlcEventTrigger 向後相容（舊設計稿可用）

---

## B3 — Templates/Shell 策略化

### 目標
把「Shell 長相」從 DesignPlayer 啟動點抽出，變成策略模式。

### 子任務
- **B3.1** `Stackdose.App.ShellShared` 新增 `IShellStrategy` 介面
- **B3.2** `DashboardShellStrategy`（對應現有 Dashboard 模式）
- **B3.3** `StandardShellStrategy`（完整 Header + LeftNav + BottomBar）
- **B3.4** DesignPlayer `App.xaml.cs` 改為選 strategy，不再 if-else

### 驗收
- [ ] 現有 Dashboard 封裝行為**完全不變**
- [ ] 新增一個 `layoutMode: "Standard"` 設計稿可封裝並顯示完整 Shell

---

## B4 — Behavior JSON Schema

### 目標
DesignItem 新增 `events[]` 欄位。純資料層，不執行。

### Schema 草案
```json
{
  "itemType": "PlcLabel",
  "props": { "address": "D100" },
  "events": [
    {
      "on": "valueChanged",
      "when": { "op": ">", "value": 100 },
      "do": [
        { "action": "setProp", "target": "Self", "prop": "Foreground", "value": "Status.Error" },
        { "action": "writePlc", "address": "M0", "value": 1 }
      ]
    }
  ]
}
```

### 驗收
- [ ] JSON round-trip 無損
- [ ] v2.0 設計稿無 events 仍能讀（向後相容）
- [ ] v2.1 schema 文件（`docs/refactor/B4-behavior-schema.md`）

---

## B5 — Behavior Engine

### 目標
`IBehaviorAction` 介面 + 6 個內建 action：
1. `SetProp`（修改控件屬性）
2. `WritePlc`（寫 PLC 地址）
3. `LogAudit`（寫稽核日誌）
4. `ShowDialog`（CyberMessageBox）
5. `Navigate`（切換頁面）
6. `SetStatus`（語意狀態切換，內部改 theme token）

### 執行流程
```
PlcControlBase.ValueChanged
  → PlcEventContext.Publish
  → BehaviorEngine.Dispatch(event)
  → 比對 events[].when
  → 逐一執行 do[]
  → 每個 action 自動 LogAudit 寫稽核（FDA 合規）
```

### 驗收
- [ ] 單元測試：餵 JSON + 模擬事件，action 正確觸發
- [ ] FDA 稽核日誌：每次 action 觸發都有紀錄

---

## B6 — Designer UI 事件頁籤

### 目標
MachinePageDesigner 的 PropertyPanel 新增「事件」分頁：
- DataGrid：on / when / do 三欄
- when：op 下拉（>, <, ==, range）+ value 輸入
- do：action 下拉 + 參數欄位（根據 action 動態）

### 驗收
- [ ] 拖一個 PlcLabel + 配「值 > 100 變紅」→ 存檔 → JSON 驗證
- [ ] DesignRuntime 預覽該設計稿，注入假 PLC 值，反應觸發

---

## B7 — DesignPlayer Standard 模式收尾

### 目標
把原本 Phase 1 的 Standard 模式做完（layoutMode 分流、BuildNavFromPages）。
此時 Shell 已在 B3 備好，這裡只接線。

### 驗收
- [ ] Standard 模式設計稿封裝 + 部署 + LeftNav 自動對應 pages[]
- [ ] Dashboard 模式不受影響

---

## B8 — docs + index.html 全面對齊

### 目標
把 `docs/refactor/` 的內容回灌到 `docs/kb/`、CLAUDE.md、index.html、memory、devlog。

### 子任務
- **B8.1** 新建 `docs/kb/behavior-system.md`（B4/B5 對外教學）
- **B8.2** 新建 `docs/kb/foundation-base-classes.md`（B1 基類使用指南）
- **B8.3** 更新 `controls-reference.md`（反映新基類）
- **B8.4** `architecture.md` 加上「Behavior Engine」章節
- **B8.5** `index.html` 首頁卡片加「一鍵反應設計」功能
- **B8.6** 更新 auto-memory 的 `project_architecture.md` / `project_components.md`
- **B8.7** `CURRENT_FOCUS.md` 標記重構完成，指向下一個焦點

### 驗收
- [ ] CLAUDE.md 讀完就能掌握新架構
- [ ] `index.html` 可展示
- [ ] memory 讀回來跟程式碼一致
