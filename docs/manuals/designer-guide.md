# 設計師手冊 — MachinePageDesigner 使用指南

> **適用對象：** UI 設計師（負責設計機台監控介面）  
> **版本：** 2026-04  
> **相關工具：** MachinePageDesigner、DesignViewer

---

## 1. 工具概覽

| 工具 | 用途 | PLC 連線 |
|---|---|---|
| **MachinePageDesigner** | 拖曳設計畫布，輸出 `.machinedesign.json` | ❌ |
| **DesignViewer** | 拖入 JSON 即時靜態預覽 | ❌ |
| **DesignPlayer** | 量產部署，即時顯示 PLC 數值 | ✅ |

**設計流程：**
```
MachinePageDesigner  →  DesignViewer    →  (工程師) DesignPlayer
     拖曳設計              靜態預覽              連線驗證 + 量產
```

---

## 2. MachinePageDesigner 快速入門

### 2.1 啟動與介面

```
┌─ Toolbar ─────────────────────────────────────────────────────┐
│  [New] [Open] [Save] [SaveAs]  |  Snap ✓  |  📌 Tags  |  縮放 │
└────────────────────────────────────────────────────────────────┘
┌─ 工具箱 ──┐  ┌─ 自由畫布（FreeCanvas）───────────────────────┐
│ 控制項    │  │                                               │
│ 模板庫    │  │   ← 拖曳控制項到這裡 →                        │
│           │  │                                               │
│           │  └───────────────────────────────────────────────┘
└───────────┘  ┌─ 屬性面板（PropertyPanel）────────────────────┐
               │  選取控制項後，在這裡設定屬性                   │
               └───────────────────────────────────────────────┘
```

### 2.2 建立新設計稿

1. 啟動 MachinePageDesigner
2. `Ctrl+N` 或點選「New」
3. 輸入頁面標題與機台 ID

### 2.3 快捷鍵

| 快捷鍵 | 功能 |
|---|---|
| `Ctrl+N` | 新建 |
| `Ctrl+O` | 開啟 |
| `Ctrl+S` | 儲存 |
| `Ctrl+Shift+S` | 另存新檔 |
| `Ctrl+Z` | 復原 |
| `Ctrl+Y` | 重做 |
| `Ctrl+C` / `Ctrl+V` | 複製 / 貼上 |
| `Delete` | 刪除選取 |
| `Shift+Click` | 多選 |
| 右鍵 | ContextMenu（複製/貼上/刪除/鎖定/Z-Order） |

---

## 3. 控制項參考

### 3.1 PLC 數值顯示

#### PlcLabel — PLC 數值顯示

顯示 PLC 寄存器的即時數值（整數/浮點數）。

| 屬性 | 說明 | 範例 |
|---|---|---|
| `address` | PLC 地址 | `D100` |
| `label` | 顯示標籤 | `溫度` |
| `valueColorTheme` | 數值顏色主題 | `NeonBlue`、`NeonGreen`、`NeonRed` |
| `frameShape` | 外框形狀 | `Rectangle`、`Ellipse`、`None` |
| `divisor` | 除數換算（`value / divisor` 顯示） | `10`（1234 顯示為 123.4） |
| `defaultValue` | 設計時顯示值 | `--` |

#### PlcText — PLC 文字顯示

顯示 PLC 字串型數值。屬性與 PlcLabel 相同，但無顏色主題。

#### PlcStatusIndicator — 狀態指示燈

顯示 PLC 位元 ON/OFF 狀態（圓形燈號）。

| 屬性 | 說明 | 範例 |
|---|---|---|
| `displayAddress` | PLC 位元地址 | `M0`、`D200.0` |
| `trueColor` | ON 時顏色 | `#22c55e`（綠色） |
| `falseColor` | OFF 時顏色 | `#374151`（暗灰） |

### 3.2 操作按鈕

#### SecuredButton — 受權限管控的按鈕

| 屬性 | 說明 |
|---|---|
| `label` | 按鈕文字 |
| `commandAddress` | 寫入目標 PLC 地址 |
| `commandType` | `write` / `pulse` / `toggle` / `sequence` |
| `writeValue` | 寫入值（write/pulse 模式，預設 1） |
| `pulseMs` | pulse 持續時間（毫秒，預設 500） |

**commandType 說明：**

- **write**：點擊後寫入 `writeValue` 到 `commandAddress`
- **pulse**：寫入 `writeValue` → 等待 `pulseMs` → 寫 0
- **toggle**：讀取當前值後翻轉
- **sequence**：執行 JSON 定義的多步驟指令（詳見第 7 節）

### 3.3 版面控制項

#### StaticLabel — 靜態文字

純靜態文字，用於標題、說明、分組標籤。

| 屬性 | 說明 |
|---|---|
| `text` | 顯示文字 |
| `fontSize` | 字體大小（預設 14） |
| `fontWeight` | `Normal` / `Bold` |
| `textAlignment` | `Left` / `Center` / `Right` |
| `foreground` | 文字顏色 |

#### Spacer（GroupBox）— 群組框

視覺分組容器，在其他控制項後方畫一個框線區塊。

| 屬性 | 說明 |
|---|---|
| `label` | 框線標題（可空白） |
| `borderColor` | 框線顏色 |

### 3.4 Viewer 控制項

#### AlarmViewer — 警報列表

顯示 alarms.json 定義的警報狀態。詳見第 5 節設定方式。

#### SensorViewer — 感測器數值列表

顯示 sensors.json 定義的組合感測器狀態。詳見第 6 節設定方式。

#### LiveLog — 即時日誌

顯示系統即時日誌，無需設定，直接放置即可。

---

## 4. 多頁面設計

### 4.1 新增頁面

- 頁籤列右側點「＋」新增頁面
- 每頁可獨立設定畫布大小（寬 × 高）
- 每頁有獨立的 Undo/Redo 堆疊

### 4.2 重新命名頁面

雙擊頁籤名稱即可重新命名。

### 4.3 頁面順序

拖曳頁籤可調整頁面順序（左右移動）。

### 4.4 刪除頁面

右鍵頁籤 → 「刪除頁面」（至少保留 1 頁）。

---

## 5. AlarmViewer 設定

### 5.1 在設計器中設定警報

1. 從工具箱拖曳 **AlarmViewer** 到畫布
2. 點選控制項，在 PropertyPanel 找「警報項目」
3. 點「＋ 新增警報」，填入：
   - **群組**：分類名稱（如「馬達」）
   - **地址**：PLC 字組地址（如 `D200`）
   - **位元**：0~15
   - **說明**：警報描述（如「馬達過載」）
4. 儲存設計稿

**存檔時自動輸出** `alarms.json` 到同目錄。

### 5.2 alarms.json 格式（參考）

```json
{ "alarms": [
  { "group": "馬達", "device": "D200", "bit": 0, "operationDescription": "馬達過載" },
  { "group": "加熱器", "device": "D200", "bit": 1, "operationDescription": "加熱器過溫" }
] }
```

---

## 6. SensorViewer 設定

### 6.1 在設計器中設定感測器

1. 拖曳 **SensorViewer** 到畫布
2. PropertyPanel → 「感測器項目」→「＋ 新增感測器」
3. 填入：
   - **群組**：分類名稱
   - **地址**：PLC 字組地址
   - **位元**：多位元用逗號分隔（如 `2,3`）
   - **期望值**：對應的正常值（如 `0,0`）
   - **模式**：`AND`（全部符合）/ `OR`（任一符合）/ `COMPARE`（數值比較）
   - **說明**：狀態說明

**存檔時自動輸出** `sensors.json` 到同目錄。

---

## 7. Command Sequence DSL（進階操作按鈕）

當單一 PLC 寫入無法滿足需求時，可使用 Sequence 模式定義多步驟指令。

### 7.1 設定方式

1. 拖曳 **SecuredButton** 到畫布
2. PropertyPanel → `commandType` 選「sequence」
3. 點「編輯 Sequence」按鈕，開啟 JSON 編輯器
4. 填入指令 JSON（見下方格式）

### 7.2 Sequence JSON 格式

```json
{
  "steps": [
    { "type": "write",  "address": "D100", "value": 1 },
    { "type": "wait",   "ms": 500 },
    { "type": "read",   "address": "D101", "variable": "status" },
    {
      "type": "conditional",
      "variable": "status", "operator": "==", "value": 1,
      "then": [{ "type": "write", "address": "D102", "value": 1 }],
      "else": [{ "type": "write", "address": "D103", "value": 0 }]
    },
    { "type": "readWait", "address": "D104", "expected": 1, "timeoutMs": 5000, "pollIntervalMs": 200 }
  ],
  "rollback": [{ "type": "write", "address": "D100", "value": 0 }],
  "onError": "rollback"
}
```

**步驟類型：**

| type | 說明 | 必要屬性 |
|---|---|---|
| `write` | 寫入 PLC 地址 | `address`, `value` |
| `read` | 讀取 PLC 地址存入變數 | `address`, `variable` |
| `wait` | 等待指定毫秒 | `ms` |
| `conditional` | 條件分支 | `variable`, `operator`, `value`, `then`, `else` |
| `readWait` | 輪詢等待直到值符合或逾時 | `address`, `expected`, `timeoutMs` |

**條件運算子（`operator`）：** `==` `!=` `>` `<` `>=` `<=`

**`onError` 策略：** `rollback`（執行 rollback 步驟）/ `stop` / `continue`

---

## 8. PLC Tags — 地址名稱對照表

### 8.1 為何使用 Tags？

由工程師預先定義 PLC 地址清單，設計師從下拉選取，避免打錯地址。

### 8.2 工程師設定 Tags（一次）

點選 Toolbar「📌 Tags」按鈕 → 新增條目：

| 欄位 | 說明 | 範例 |
|---|---|---|
| Address | PLC 地址 | `D100` |
| Name | 顯示名稱 | `爐溫` |
| Unit | 單位 | `°C` |

### 8.3 設計師使用 Tags

在 PropertyPanel 的地址欄（address / displayAddress / commandAddress）：
- 直接輸入地址，**或**
- 點選下拉箭頭，從 Tags 清單選取（顯示「D100 — 爐溫（°C）」格式）

選取後只填入地址（`D100`），不影響 JSON 格式。

---

## 9. 控制項模板庫（Template Gallery）

### 9.1 使用內建模板

1. 工具箱點選「模板庫」頁籤
2. 瀏覽 9 個內建模板（溫度監控組、壓力監控組、計數器顯示、啟動/停止控制、緊急停止、閥門切換、設備總覽、日誌面板）
3. 拖曳模板到畫布，自動展開多個控制項組合

### 9.2 儲存自訂模板

1. 框選多個控制項（拖曳框選或 Shift+Click）
2. 右鍵 → 「儲存為自訂模板」
3. 輸入模板名稱與分類
4. 自訂模板儲存於 `%LOCALAPPDATA%\Stackdose\Templates\`

---

## 10. 多選與對齊操作

### 10.1 多選

- **框選**：在空白處拖曳畫出選取框
- **Shift+Click**：點選時加入/移出選取集合

### 10.2 對齊與分配

多選後，Toolbar 或右鍵選單提供：

| 功能 | 說明 |
|---|---|
| 左對齊 | 所有選取項目對齊最左者 |
| 右對齊 | 對齊最右者 |
| 頂端對齊 | 對齊最頂者 |
| 底部對齊 | 對齊最底者 |
| 水平均等分配 | 水平間距均等 |
| 垂直均等分配 | 垂直間距均等 |
| 水平置中 | 對齊水平中心線 |
| 垂直置中 | 對齊垂直中心線 |

### 10.3 Z-Order（層級）

右鍵 → 「移到最前」/ 「移到最後」/ 「上移一層」/ 「下移一層」

### 10.4 鎖定控制項

右鍵 → 「鎖定」可防止誤移。鎖定後顯示 🔒 圖示。再次右鍵 → 「解鎖」。

---

## 11. 儲存與輸出

### 11.1 儲存

`Ctrl+S` 儲存 `.machinedesign.json`。

**存檔時自動輸出（若有相應設定）：**
- `alarms.json`（若有 AlarmViewer 且設定了警報項目）
- `sensors.json`（若有 SensorViewer 且設定了感測器項目）
- `{檔名}.tags-report.txt`（若有定義 Tags）

### 11.2 Tags 使用報告

存檔後會在同目錄產生 `{檔名}.tags-report.txt`，列出：
- 所有定義的 Tags 及使用狀況（✓ 使用中 / — 未使用）
- 設計稿中使用但**未定義**在 Tags 清單的地址（⚠ 警告）

> **建議上線前確認** Tags 報告中無 ⚠ 警告項目。

---

## 12. 使用 DesignViewer 預覽

1. 執行 `DesignViewer.exe`
2. 將 `.machinedesign.json` **拖入**視窗
3. 畫布即時渲染（控制項顯示設計時預設值）
4. 若有多頁面，可點選頁籤切換

> DesignViewer 不連 PLC，僅顯示版面外觀。確認排版後交給工程師連線驗證。

---

## 13. 常見問題

**Q: 控制項不顯示數值（只顯示 `--`）？**
A: 正常。設計器與 DesignViewer 不連 PLC，顯示 `defaultValue`。真實數值只在 DesignPlayer / DesignRuntime 中可見。

**Q: 拖曳控制項時會吸附？**
A: Snap 功能預設開啟（工具列「Snap ✓」）。關閉 Snap 可自由定位，開啟時自動吸附格線與其他控制項邊緣。

**Q: AlarmViewer 在設計器看不到警報列表？**
A: 正常。警報資料只在執行時（DesignPlayer）讀取 alarms.json 後顯示。設計器中僅顯示「已設定 N 個警報」摘要。

**Q: Undo 只能復原當前頁的操作？**
A: 是的，Undo/Redo 是每頁獨立的。切換頁面後 Ctrl+Z 只復原該頁的操作。

**Q: 如何確認 PLC 地址正確？**
A: 請工程師在 Toolbar「📌 Tags」預先定義地址清單，設計師從下拉選取，可降低打錯地址的風險。存檔後也可檢查 Tags 使用報告。
