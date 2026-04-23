# 設計器系統知識庫

> 涵蓋 MachinePageDesigner、DesignViewer、DesignRuntime 三個工具的架構與使用方式。

---

## 1. 設計器系統概覽

三個專案構成完整的「設計 → 預覽 → 執行」工作流程：

```
MachinePageDesigner        DesignViewer              DesignRuntime
（拖曳設計畫布）    →→→   （JSON即時預覽）   →→→   （真實PLC連線執行）
輸出 .machinedesign.json   拖入JSON即時渲染         載入JSON + 連線PLC
```

---

## 2. MachinePageDesigner

**專案：** `Stackdose.Tools.MachinePageDesigner`
**狀態：** 主力開發（自由畫布模式已完成）

### 2.1 設計哲學演進

| 版本 | 模式 | 說明 |
|---|---|---|
| v1.0（2026-04-01） | Zone 制（Grid排列） | 固定功能區塊，UniformGrid 自動排列 |
| v2.0（2026-04-08+） | **自由畫布（FreeCanvas）** | 拖曳任意位置，完全自由定位 |

目前為 v2.0 自由畫布模式。

### 2.2 自由畫布（FreeCanvas）功能清單

| 功能 | 狀態 | 說明 |
|---|---|---|
| 拖曳任意放置 | ✅ | 控制項可拖到畫布任意位置 |
| Snap 對齊 | ✅ | 拖曳時自動吸附格線 |
| Z-Order 控制 | ✅ | 前置/後置層級調整 |
| 框選（多選） | ✅ | 拖曳框選多個控制項 |
| 鎖定 | ✅ | 鎖定控制項防止誤移 |
| 複製貼上 | ✅ | Ctrl+C / Ctrl+V |
| 多選同步 | ✅ | 多選同步移動 |
| GroupBox | ✅ | 將多個控制項組合成群組 |
| 對齊分配 | ✅ | 左對齊、水平均等分配等 |
| 畫布尺寸設定 | ✅ | 可自訂畫布寬高 |
| Undo/Redo | ✅ | Ctrl+Z / Ctrl+Y |
| 儲存/載入 | ✅ | .machinedesign.json |

### 2.3 輸出格式（.machinedesign.json）

```json
{
  "version": "2.0",
  "shellMode": "FreeCanvas",
  "meta": {
    "title": "頁面標題",
    "machineId": "M1"
  },
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "canvasItems": [
    {
      "id": "a1b2c3d4",
      "type": "PlcLabel",
      "x": 100, "y": 200,
      "width": 120, "height": 80,
      "order": 0,
      "locked": false,
      "props": {
        "label": "溫度",
        "address": "D100",
        "colorTheme": "NeonBlue",
        "shape": "Circle"
      },
      "events": [
        {
          "on": "valueChanged",
          "when": { "op": ">", "value": 100 },
          "do": [
            { "action": "SetProp", "target": "self", "prop": "background", "value": "Red" },
            { "action": "LogAudit", "message": "溫度超標: {value}" },
            { "action": "ShowDialog", "title": "警告", "message": "溫度超過 100°C！" }
          ]
        }
      ]
    }
  ]
}
```

#### shellMode 可選值
| 值 | 說明 |
|---|---|
| `FreeCanvas`（預設） | 裸畫布，無外殼包裝 |
| `SinglePage` | 包裝進 SinglePageContainer（Header only） |
| `Standard` | 包裝進 MainContainer（Header + LeftNav + BottomBar，B7 接線） |

#### events 欄位（B4 新增）

每個控制項可定義行為事件清單，實現「無 XAML 反應式 UI」：

| 欄位 | 型別 | 說明 |
|---|---|---|
| `on` | string | 觸發來源：`valueChanged` / `click` / `connected` / `disconnected` |
| `when` | 物件（可省略） | 比較條件；省略時無條件觸發 |
| `when.op` | string | 運算子：`>` / `>=` / `<` / `<=` / `==` / `!=` |
| `when.value` | number | 比較基準值 |
| `do` | 陣列 | 依序執行的動作清單 |

#### 支援的動作（B5 Engine 實作）

| `action` | 必填欄位 | 說明 |
|---|---|---|
| `SetProp` | `target`, `prop`, `value` | 修改控制項 props（`"self"` = 觸發控制項） |
| `WritePlc` | `target`(位址), `value` | 寫入 PLC 暫存器 |
| `LogAudit` | `message` | 寫入 FDA 稽核日誌 |
| `ShowDialog` | `title`, `message` | 顯示警告對話框 |
| `Navigate` | `page` | 切換頁面（Standard Shell） |
| `SetStatus` | `value` | 設定機器狀態 |

`message` / `value` 支援 `{value}` 佔位符（運行時替換為實際 PLC 值）。

### 2.4 支援控制項

| 控制項 | 說明 |
|---|---|
| `PlcLabel` | PLC數值顯示，支援色彩主題、外框形狀、除數換算 |
| `PlcText` | PLC文字顯示 |
| `PlcStatusIndicator` | 狀態指示燈 |
| `SecuredButton` | 需權限驗證的操作按鈕 |
| `Spacer` | 空白佔位元素 |

### 2.7 PropertyPanel — 事件（⚡）Tab（B6 新增）

`MachinePageDesigner/Views/PropertyPanel.xaml` 現在是一個 TabControl，有兩個 Tab：

| Tab | 內容 |
|---|---|
| **屬性** | 原有的控件屬性設定（Label、Address、Shape…） |
| **事件 ⚡** | `EventsPanel`：事件行為編輯 UI |

#### EventsPanel 結構

3 層 Master-Detail：

```
事件清單（ListBox）
  ├── 每筆：On 觸發類型 + 觸發說明 Summary
  ├── [新增] / [移除] 按鈕
  └── 選取某事件後展開「事件詳情」
        ├── On：下拉（valueChanged / click / connected / disconnected）
        ├── When：勾選框（有無條件）→ 運算子 + 基準值
        └── Do（動作清單 ListBox）
              ├── 每筆：動作 Summary
              ├── [新增動作] / [移除動作] 按鈕
              └── 選取某動作後展開「動作詳情」
                    ├── ActionType 下拉（SetProp / WritePlc / LogAudit…）
                    └── 依 ActionType 動態顯示欄位（Target/Prop/Value/Message/Title/Page）
```

#### 相關 ViewModel

| 類別 | 說明 |
|---|---|
| `BehaviorEventViewModel` | 包裝 `BehaviorEvent` POCO；`Actions` = ObservableCollection；靜態 `OnTypes`/`WhenOps` 供 ComboBox |
| `BehaviorActionViewModel` | 包裝 `BehaviorAction` POCO；`ShowTarget/Prop/Value/Message/Title/Page` 可見性；`Summary` 顯示字串 |

`DesignerItemViewModel.Events`（ObservableCollection）由 `BuildEventsCollection()` 初始化並 CollectionChanged 同步回 `_definition.Events`（POCO 清單）。

#### 注意：_suppressHandlers 機制

`EventsPanel.xaml.cs` 中 `ShowEventDetail()` / `ShowActionDetail()` 使用 `_suppressHandlers = true/false` 包裹程式碼寫 UI 的段落，防止 ComboBox.SelectionChanged 等事件在程式更新 UI 時觸發回寫邏輯，造成資料損毀。



| 快捷鍵 | 功能 |
|---|---|
| `Ctrl+N` | 新建 |
| `Ctrl+O` | 開啟 |
| `Ctrl+S` | 儲存 |
| `Ctrl+Shift+S` | 另存新檔 |
| `Ctrl+Z` | 復原 |
| `Ctrl+Y` | 重做 |
| `Ctrl+C` / `Ctrl+V` | 複製/貼上 |
| `Delete` | 刪除選取 |
| `Shift+Click` | 多選 |

### 2.6 專案依賴
```
Stackdose.Tools.MachinePageDesigner
├── → Stackdose.UI.Core（PlcLabel 等真實控制項，WYSIWYG）
└── → Stackdose.App.DeviceFramework（DeviceLabelViewModel、PlcDataGridPanel）
```

---

## 3. DesignViewer

**專案：** `Stackdose.Tools.DesignViewer`
**狀態：** 開發中

### 3.1 用途
純預覽工具，不連 PLC，不需安裝完整執行環境。使用者拖入 `.machinedesign.json` 即可即時渲染頁面外觀。

### 3.2 使用方式
1. 執行 `Stackdose.Tools.DesignViewer.exe`
2. 將 `.machinedesign.json` 拖入視窗
3. 畫布即時渲染（控制項顯示預設值/模擬值）

### 3.3 專案依賴
```
Stackdose.Tools.DesignViewer
├── → Stackdose.UI.Core
└── → Stackdose.Tools.MachinePageDesigner（共用渲染邏輯）
```

---

## 4. DesignRuntime

**專案：** `Stackdose.App.DesignRuntime`
**狀態：** 開發中（有未提交變更）

### 4.1 用途
真實執行環境：連線 PLC，載入 `.machinedesign.json`，控制項顯示實際 PLC 數值。

### 4.2 與其他工具的差異
| | MachinePageDesigner | DesignViewer | DesignRuntime |
|---|---|---|---|
| PLC連線 | ❌ | ❌ | ✅ |
| 即時數值 | ❌（預設值） | ❌（模擬值） | ✅ |
| 編輯功能 | ✅ | ❌ | ❌ |
| 用途 | 設計 | 預覽 | 執行 |

### 4.3 專案依賴
```
Stackdose.App.DesignRuntime
├── → Stackdose.UI.Core
├── → Stackdose.App.ShellShared（B3：Shell 策略）
├── → Stackdose.App.DeviceFramework
└── → Stackdose.Tools.MachinePageDesigner（載入 JSON 渲染）
```

---

## 5. 常見問題

**Q: 設計時控制項不顯示數值？**
A: 正常，MachinePageDesigner 和 DesignViewer 不連 PLC，顯示 DefaultValue。

**Q: 儲存後在 DesignRuntime 看不到更新？**
A: 確認 DesignRuntime 的 Config 路徑指向正確的 .machinedesign.json。

**Q: 拖曳時控制項跳到奇怪位置？**
A: 確認 Snap 設定，Snap 啟用時會吸附格線（可在工具列關閉）。
